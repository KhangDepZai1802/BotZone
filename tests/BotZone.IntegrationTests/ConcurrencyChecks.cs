using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Seeding;
using BotZone.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

internal static class ConcurrencyChecks
{
    public static async Task RunAsync(string connection,IConfiguration configuration,Func<string,Func<Task>,Task> test)
    {
        var config=new ConfigurationBuilder().AddConfiguration(configuration).AddInMemoryCollection(new Dictionary<string,string?>{{"DemoMode","true"}}).Build();
        var owner=new Actor(DemoSeeder.OwnerId,"Owner","owner@botzone.local","owner",true);var warehouse=owner with{Id="demo-warehouse",Role="warehouse_staff"};var staff=owner with{Id="demo-nv01",Role="order_staff"};var customer=owner with{Id=DemoSeeder.CustomerId,Role="customer"};var other=customer with{Id="demo-customer-other"};
        BotZoneDbContext Db()=>new(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection).Options);
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        async Task<JsonElement> Command(Actor actor,string name,object payload,string? key=null)
        {
            await using var db=Db();var sql=new SqlStore(db);var commerce=new CommerceService(sql,config);var media=new MediaService(sql,config);var commands=new CommandService(sql,commerce,new InventoryService(sql),new LandingService(sql,config,media),new AfterSalesService(sql,commerce,config));
            return JsonSerializer.SerializeToElement(await commands.Execute(actor,new(name,key??Guid.NewGuid().ToString(),JsonSerializer.SerializeToElement(payload))));
        }
        async Task<Dictionary<string,object?>> One(string query,params object?[] args){await using var db=Db();return await new SqlStore(db).One(query,args);}
        var run=Guid.NewGuid().ToString("N")[..12];Guid variant=Guid.Empty;var openOrders=new List<(Actor Customer,Guid Order)>();
        async Task Receive(int count)
        {
            var po=await Command(warehouse,"create_purchase",new{supplierId=DemoSeeder.Id("supplier"),items=new[]{new{variantId=variant,quantity=count,unitCostVnd="10000"}}});var pid=po.GetProperty("purchaseId").GetGuid();await Command(owner,"approve_purchase",new{purchaseId=pid});var line=(await One("SELECT id FROM purchase_order_items WHERE purchase_order_id=$1",pid)).G("id");await Command(warehouse,"receive_goods",new{purchaseId=pid,items=new[]{new{purchaseOrderItemId=line,quantity=count,serials=Array.Empty<string>()}}});
        }
        async Task<object> PlaceInput(Actor actor,string method="cod",string promotion="")
        {
            await using var db=Db();var commerce=new CommerceService(new SqlStore(db),config);var items=new[]{new{variantId=variant,quantity=1,care=false}};var quote=JsonSerializer.SerializeToElement(await commerce.Quote(actor,JsonSerializer.SerializeToElement(new{items,promotionCode=promotion,points=0,creditVnd="0"})));
            return new{items,promotionCode=promotion,points=0,creditVnd="0",quoteHash=quote.GetProperty("quoteHash").GetString(),method,address=new{recipient="Khách kiểm thử đồng thời",phone="0901234567",province="Hồ Chí Minh",ward="Phường Sài Gòn",addressLine="123 Đường Nguyễn Huệ",country="VN"}};
        }
        async Task<(JsonElement? Result,string? Error)> Try(Func<Task<JsonElement>> action){try{return(await action(),null);}catch(BusinessException error){return(null,error.Code);}}
        async Task<(JsonElement? Result,string? Error)[]> Race(Func<Task<JsonElement>> first,Func<Task<JsonElement>> second)
        {
            var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<(JsonElement? Result,string? Error)> Start(Func<Task<JsonElement>> action){await gate.Task;return await Try(action);}
            var one=Start(first);var two=Start(second);gate.SetResult();return await Task.WhenAll(one,two);
        }
        async Task Cancel(Actor actor,Guid orderId){await Command(actor,"cancel_order",new{orderId});openOrders.RemoveAll(o=>o.Order==orderId);}
        try
        {
            var sku=await Command(owner,"create_variant",new{productId=DemoSeeder.Id("product:AP2"),sku="CONC-"+run.ToUpperInvariant(),options=new{fixture="concurrency-"+run},priceVnd="20001",trackingMode="quantity",standardWarrantyMonths=12,careFeeVnd=(string?)null,active=true});variant=sku.GetProperty("id").GetGuid();await Receive(1);
            await test("concurrency separate connections reserve last stock only once",async()=>
            {
                var p1=await PlaceInput(customer);var p2=await PlaceInput(other);var results=await Race(()=>Command(customer,"place_order",p1),()=>Command(other,"place_order",p2));Check(results.Count(r=>r.Result.HasValue)==1&&results.Count(r=>r.Error=="STOCK_CONFLICT")==1,"Exactly one reservation wins and the other sees stock conflict.");
                var index=Array.FindIndex(results,r=>r.Result.HasValue);var actor=index==0?customer:other;var oid=results[index].Result!.Value.GetProperty("orderId").GetGuid();openOrders.Add((actor,oid));Check((await One("SELECT available FROM variant_stock WHERE variant_id=$1",variant)).L("available")==0,"No remaining stock while reserved.");await Cancel(actor,oid);Check((await One("SELECT available FROM variant_stock WHERE variant_id=$1",variant)).L("available")==1,"Cancellation releases exactly the held unit.");
            });
            await Receive(2);
            await test("concurrency separate customers contest final voucher quota",async()=>
            {
                var code="CONC"+run.ToUpperInvariant();await Command(owner,"create_promotion",new{code,discountType="fixed",value="1000",minPurchaseVnd="0",startsAt=DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"),endsAt=DateTimeOffset.UtcNow.AddDays(1).ToString("O"),maxUses=1,maxUsesPerCustomer=1,allProducts=false,targets=new[]{new{variantId=variant}},active=true});
                var p1=await PlaceInput(customer,promotion:code);var p2=await PlaceInput(other,promotion:code);var results=await Race(()=>Command(customer,"place_order",p1),()=>Command(other,"place_order",p2));Check(results.Count(r=>r.Result.HasValue)==1&&results.Count(r=>r.Error=="PROMO_LIMIT")==1,"Only one reservation may consume the final voucher quota.");
                var index=Array.FindIndex(results,r=>r.Result.HasValue);var actor=index==0?customer:other;var oid=results[index].Result!.Value.GetProperty("orderId").GetGuid();openOrders.Add((actor,oid));Check((await One("SELECT count(*)::int n FROM promotion_redemptions r JOIN promotions p ON p.id=r.promotion_id WHERE p.code_normalized=$1 AND r.status='reserved'",code)).I("n")==1,"Exactly one reserved redemption.");await Cancel(actor,oid);
            });
            Guid paidOrder=Guid.Empty;
            await test("concurrency same payment idempotency key returns one receipt",async()=>
            {
                var input=await PlaceInput(customer,"banking");var placed=await Command(customer,"place_order",input);paidOrder=placed.GetProperty("orderId").GetGuid();openOrders.Add((customer,paidOrder));var attempt=await Command(customer,"start_payment",new{orderId=paidOrder});var payload=new{attemptId=attempt.GetProperty("id").GetGuid(),amountVnd=attempt.GetProperty("requested_amount_vnd").GetString(),outcome="success"};var key=Guid.NewGuid().ToString();var results=await Race(()=>Command(staff,"record_payment",payload,key),()=>Command(staff,"record_payment",payload,key));Check(results.All(r=>r.Result.HasValue),"Both callers receive the recorded result.");Check((await One("SELECT count(*)::int n FROM payment_receipts WHERE order_id=$1",paidOrder)).I("n")==1,"One actual receipt despite concurrent duplicate command.");await Cancel(customer,paidOrder);
            });
            await test("concurrency different refund requests cannot exceed paid source",async()=>
            {
                Check(paidOrder!=Guid.Empty,"Paid fixture exists.");var results=await Race(()=>Command(owner,"execute_refund",new{orderId=paidOrder,outcome="success"}),()=>Command(owner,"execute_refund",new{orderId=paidOrder,outcome="success"}));Check(results.All(r=>r.Result.HasValue),"Both refund commands finish deterministically.");Check(results.Count(r=>r.Result!.Value.GetProperty("totalVnd").GetString()=="0")==1,"Second transaction sees exhausted refundable source.");var counts=await One("SELECT count(*)::int n,sum(amount_vnd) total FROM refunds WHERE order_id=$1 AND status='completed'",paidOrder);var paid=await One("SELECT sum(amount_vnd) total FROM payment_receipts WHERE order_id=$1",paidOrder);Check(counts.I("n")==1&&counts.L("total")==paid.L("total"),"Exactly original paid amount refunded once.");
            });
        }
        finally
        {
            foreach(var item in openOrders.ToArray())try{await Cancel(item.Customer,item.Order);}catch(Exception error){Console.Error.WriteLine("Fixture cancellation failed: "+item.Order+" "+error.Message);}
            if(variant!=Guid.Empty)await Command(owner,"update_variant",new{variantId=variant,priceVnd="20001",active=false,careFeeVnd=(string?)null,standardWarrantyMonths=12});
        }
    }
}
