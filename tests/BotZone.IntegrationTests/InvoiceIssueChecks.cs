using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Seeding;
using BotZone.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

internal static class InvoiceIssueChecks
{
    public static async Task RunAsync(string connection,IConfiguration configuration,Func<string,Func<Task>,Task> test)
    {
        var config=new ConfigurationBuilder().AddConfiguration(configuration).AddInMemoryCollection(new Dictionary<string,string?>{{"DemoMode","true"}}).Build();
        var owner=new Actor(DemoSeeder.OwnerId,"Owner","owner@botzone.local","owner",true);var customer=owner with{Id=DemoSeeder.CustomerId,Role="customer"};var staff=owner with{Id="demo-nv01",Role="order_staff"};var warehouse=owner with{Id="demo-warehouse",Role="warehouse_staff"};
        var orders=new List<Guid>();var evidence=new List<object>();
        BotZoneDbContext Db()=>new(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection).Options);
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        async Task<JsonElement> Command(Actor actor,string name,object input,string? key=null)
        {
            await using var db=Db();var sql=new SqlStore(db);var commerce=new CommerceService(sql,config);var media=new MediaService(sql,config);var commands=new CommandService(sql,commerce,new InventoryService(sql),new LandingService(sql,config,media),new AfterSalesService(sql,commerce,config));
            return JsonSerializer.SerializeToElement(await commands.Execute(actor,new(name,key??Guid.NewGuid().ToString(),JsonSerializer.SerializeToElement(input))));
        }
        async Task<Dictionary<string,object?>> One(string query,params object?[] args){await using var db=Db();return await new SqlStore(db).One(query,args);}
        async Task<Guid> Place()
        {
            await using var db=Db();var commerce=new CommerceService(new SqlStore(db),config);var items=new[]{new{variantId=DemoSeeder.Id("sku:AP2"),quantity=1,care=false}};
            var quote=JsonSerializer.SerializeToElement(await commerce.Quote(customer,JsonSerializer.SerializeToElement(new{items,points=0,creditVnd="0",promotionCode=""})));
            var result=await Command(customer,"place_order",new{items,points=0,creditVnd="0",promotionCode="",quoteHash=quote.GetProperty("quoteHash").GetString(),method="cod",address=new{recipient="Fixture DB-T17 phát hành",phone="0901234567",province="Hồ Chí Minh",ward="Phường Sài Gòn",addressLine="123 Đường Nguyễn Huệ",country="VN"}});
            var id=result.GetProperty("orderId").GetGuid();orders.Add(id);await Command(staff,"confirm_order",new{orderId=id});return id;
        }
        async Task<Dictionary<string,object?>> State(Guid id)=>await One("SELECT o.order_status,(SELECT count(*)::int FROM invoices WHERE order_id=o.id) invoice_count,(SELECT lifecycle FROM invoices WHERE order_id=o.id) lifecycle,(SELECT invoice_no FROM invoices WHERE order_id=o.id) invoice_no,(SELECT count(*)::int FROM shipments WHERE order_id=o.id) shipment_count,(SELECT count(*)::int FROM inventory_allocations a JOIN order_items i ON i.id=a.order_item_id WHERE i.order_id=o.id AND a.state='shipped') shipped_count,(SELECT count(*)::int FROM stock_movements m WHERE m.source_event_key IN(SELECT 'ship:'||a.id::text FROM inventory_allocations a JOIN order_items i ON i.id=a.order_item_id WHERE i.order_id=o.id)) movement_count FROM orders o WHERE o.id=$1",id);
        try
        {
            foreach(var withDraft in new[]{false,true})
            {
                await test("DB-T17 issue "+(withDraft?"existing draft":"without draft")+" and retry preserves one invoice number and one stock movement",async()=>
                {
                    var orderId=await Place();Guid? draftId=null;
                    if(withDraft)
                    {
                        var draft=await Command(staff,"create_invoice_draft",new{orderId});draftId=draft.GetProperty("id").GetGuid();
                        var refreshed=await Command(staff,"create_invoice_draft",new{orderId});Check(refreshed.GetProperty("id").GetGuid()==draftId,"Repeated draft creation reuses the same invoice.");
                        Check((await State(orderId)).S("lifecycle")=="draft","Draft exists before physical handoff.");
                    }
                    else Check((await State(orderId)).I("invoice_count")==0,"No draft exists before handoff.");
                    var key=Guid.NewGuid().ToString();var issued=await Command(warehouse,"handoff",new{orderId},key);var replay=await Command(warehouse,"handoff",new{orderId},key);
                    Check(issued.GetProperty("invoiceId").GetGuid()==replay.GetProperty("invoiceId").GetGuid()&&issued.GetProperty("invoiceNo").GetString()==replay.GetProperty("invoiceNo").GetString(),"Retry preserves ID and issued number.");
                    if(draftId.HasValue)Check(issued.GetProperty("invoiceId").GetGuid()==draftId,"Issuance transitions the existing draft.");
                    var rejected=false;try{await Command(warehouse,"handoff",new{orderId});}catch(BusinessException e)when(e.Code=="STATE_CONFLICT"){rejected=true;}
                    Check(rejected,"A second unrelated handoff cannot ship again.");var state=await State(orderId);
                    Check(state.I("invoice_count")==1&&state.I("shipment_count")==1&&state.I("shipped_count")==1&&state.I("movement_count")==1&&state.S("lifecycle")=="issued","Exactly one issued invoice, shipment and physical stock movement.");
                    var actors=await One("SELECT responsible_staff_id,issued_by_user_id FROM invoices WHERE order_id=$1",orderId);Check(actors.S("responsible_staff_id")==staff.Id&&actors.S("issued_by_user_id")==warehouse.Id,"Responsible staff and issuing actor stay distinct.");
                    evidence.Add(new{orderId,withDraft,invoiceId=issued.GetProperty("invoiceId").GetGuid(),invoiceNo=issued.GetProperty("invoiceNo").GetString(),retryStable=true});
                });
            }
            await test("DB-T17 invoice validation failure after stock mutation rolls back handoff transaction",async()=>
            {
                var orderId=await Place();await Command(staff,"create_invoice_draft",new{orderId});
                var before=await One("SELECT sum(l.qty_on_hand) quantity,sum(l.qty_reserved) reserved FROM inventory_lots l WHERE l.variant_id=$1",DemoSeeder.Id("sku:AP2"));
                // Missing current responsibility fails inside Invoice(), after Handoff
                // has mutated stock. Only this new fixture's mutable handler is changed.
                await using(var db=Db())await new SqlStore(db).Exec("UPDATE orders SET responsible_staff_id=NULL WHERE id=$1",orderId);
                var failed=false;
                try{await Command(warehouse,"handoff",new{orderId});}
                catch(BusinessException e)when(e.Code=="STATE_CONFLICT"&&e.Message=="Đơn chưa có nhân viên phụ trách."){failed=true;}
                Check(failed,"Fault must occur at invoice validation after the stock loop.");var state=await State(orderId);
                var after=await One("SELECT sum(l.qty_on_hand) quantity,sum(l.qty_reserved) reserved FROM inventory_lots l WHERE l.variant_id=$1",DemoSeeder.Id("sku:AP2"));
                Check(before.L("quantity")==after.L("quantity")&&before.L("reserved")==after.L("reserved"),"Stock on hand and reserved balances rolled back.");
                Check(state.S("order_status")=="confirmed"&&state.S("lifecycle")=="draft"&&state.S("invoice_no")==""&&state.I("shipment_count")==0&&state.I("shipped_count")==0&&state.I("movement_count")==0,"No half-issued invoice, shipment, allocation or stock movement survives.");
                evidence.Add(new{orderId,fault="missing current staff at invoice stage",stockRestored=true,draftUnissued=true});
            });
        }
        finally
        {
            var errors=new List<Exception>();var cleanup=new List<object>();
            foreach(var id in orders)
            {
                try
                {
                    var state=await State(id);
                    if(state.S("order_status")=="handed_off")
                    {
                        await Command(staff,"shipment_event",new{orderId=id,status="failed"});await Command(staff,"shipment_event",new{orderId=id,status="returned"});
                        var shipment=(await One("SELECT id FROM shipments WHERE order_id=$1",id)).G("id");await Command(warehouse,"inspect_failed_shipment",new{shipmentId=shipment,condition="new",note="Fixture DB-T17 thu hồi nguyên niêm phong"});
                        var credit=await Command(owner,"finalize_failed_delivery",new{shipmentId=shipment,restock=true,reason="Kết thúc fixture DB-T17, duyệt nhập lại hàng mới"});
                        var refund=await Command(owner,"execute_refund",new{creditNoteId=credit.GetProperty("creditNoteId").GetGuid(),outcome="success"});Check(refund.GetProperty("totalVnd").GetString()=="0","Unpaid COD cleanup cannot invent a refund.");
                        cleanup.Add(new{orderId=id,closedBy="inspected returned shipment and issued credit note",newStockRestored=true,fakeRefund=false});
                    }
                    else
                    {
                        await Command(customer,"cancel_order",new{orderId=id});cleanup.Add(new{orderId=id,closedBy="cancel order command"});
                    }
                }
                catch(Exception error){errors.Add(error);cleanup.Add(new{orderId=id,error=error.Message});}
            }
            await File.WriteAllTextAsync(Path.Combine(configuration["BOTZONE_ROOT"]!,"tests/BotZone.IntegrationTests/invoice-issue-result.json"),JsonSerializer.Serialize(new{checkedAt=DateTimeOffset.UtcNow,database=new Npgsql.NpgsqlConnectionStringBuilder(connection).Database,cases=evidence,cleanup,noRowsDeletedByTest=true},new JsonSerializerOptions{WriteIndented=true}));
            if(errors.Count>0)throw new AggregateException("Invoice issue fixture cleanup needs attention.",errors);
        }
    }
}
