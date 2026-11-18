using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Seeding;
using BotZone.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

internal static class PaymentDispatchChecks
{
    public static async Task RunAsync(string connection,IConfiguration configuration,Func<string,Func<Task>,Task> test)
    {
        var config=new ConfigurationBuilder().AddConfiguration(configuration).AddInMemoryCollection(new Dictionary<string,string?>{{"DemoMode","true"}}).Build();
        var owner=new Actor(DemoSeeder.OwnerId,"Owner","owner@botzone.local","owner",true);
        var staff=owner with{Id="demo-nv01",Role="order_staff"};
        var customer=owner with{Id=DemoSeeder.CustomerId,Role="customer"};
        var other=customer with{Id="demo-customer-other"};
        var orders=new List<(Guid Id,string Method,string Purpose)>();
        var cleanups=new List<object>();
        BotZoneDbContext Db()=>new(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection).Options);
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        async Task<JsonElement> Command(Actor actor,string name,object input,string? key=null)
        {
            await using var db=Db();var sql=new SqlStore(db);var commerce=new CommerceService(sql,config);var media=new MediaService(sql,config);
            var commands=new CommandService(sql,commerce,new InventoryService(sql),new LandingService(sql,config,media),new AfterSalesService(sql,commerce,config));
            return JsonSerializer.SerializeToElement(await commands.Execute(actor,new(name,key??Guid.NewGuid().ToString(),JsonSerializer.SerializeToElement(input))));
        }
        async Task<Dictionary<string,object?>> One(string text,params object?[] args){await using var db=Db();return await new SqlStore(db).One(text,args);}
        async Task Reject(string code,int status,Func<Task> action)
        {
            try{await action();}catch(BusinessException error)when(error.Code==code&&error.Status==status){return;}
            throw new Exception($"Expected {code}/{status} from actual dispatcher.");
        }
        async Task<Guid> Place(string method,string purpose,bool verifyReplay=false)
        {
            await using var db=Db();var commerce=new CommerceService(new SqlStore(db),config);
            var items=new[]{new{variantId=DemoSeeder.Id("sku:AP2"),quantity=1,care=false}};
            var quote=JsonSerializer.SerializeToElement(await commerce.Quote(customer,JsonSerializer.SerializeToElement(new{items,promotionCode="",points=0,creditVnd="0"})));
            var input=new{items,promotionCode="",points=0,creditVnd="0",quoteHash=quote.GetProperty("quoteHash").GetString(),method,address=new{recipient="Fixture kiểm thử payment dispatcher",phone="0901234567",province="Hồ Chí Minh",ward="Phường Sài Gòn",addressLine="123 Đường Nguyễn Huệ",country="VN"}};
            var key=Guid.NewGuid().ToString();var placed=await Command(customer,"place_order",input,key);var id=placed.GetProperty("orderId").GetGuid();orders.Add((id,method,purpose));
            if(verifyReplay)
            {
                var replay=await Command(customer,"place_order",input,key);Check(replay.GetProperty("orderId").GetGuid()==id,"Double place with the same key returns the same order.");
                Check((await One("SELECT count(*)::int n FROM idempotency_records WHERE scope=$1 AND key=$2",customer.Id+":place_order",key)).I("n")==1,"One idempotent place record.");
            }
            return id;
        }
        async Task<JsonElement> Start(Guid orderId,Actor? actor=null)=>await Command(actor??customer,"start_payment",new{orderId});
        async Task<JsonElement> Outcome(JsonElement attempt,string outcome,string? key=null,Actor? actor=null)=>await Command(actor??customer,"record_payment",new{attemptId=attempt.GetProperty("id").GetGuid(),outcome,amountVnd=attempt.GetProperty("requested_amount_vnd").GetString()},key);
        async Task Cleanup((Guid Id,string Method,string Purpose) fixture)
        {
            await Command(customer,"cancel_order",new{orderId=fixture.Id});
            var refund=await Command(owner,"execute_refund",new{orderId=fixture.Id,outcome="success"});
            var sums=await One("SELECT (SELECT COALESCE(sum(amount_vnd),0) FROM payment_receipts WHERE order_id=$1) paid,(SELECT COALESCE(sum(amount_vnd),0) FROM refunds WHERE order_id=$1 AND status='completed') refunded,(SELECT count(*)::int FROM inventory_reservations r JOIN order_items i ON i.id=r.order_item_id WHERE i.order_id=$1 AND r.status='active') active",fixture.Id);
            Check(sums.L("paid")==sums.L("refunded")&&sums.I("active")==0,"Fixture cleanup refunds exactly paid money and releases reservation.");
            cleanups.Add(new{orderId=fixture.Id,fixture.Method,fixture.Purpose,paidVnd=sums.S("paid"),refundedVnd=sums.S("refunded"),cancelled=true,activeReservations=0,refundResult=refund});
        }
        try
        {
            foreach(var method in new[]{"momo","visa"})
            {
                await test("dispatcher "+method+" customer scenarios failed pending expired cancelled success and idempotent retries",async()=>
                {
                    var orderId=await Place(method,"actual-dispatcher-outcomes-and-retries",true);
                    await Reject("NOT_FOUND",404,async()=>await Start(orderId,other));
                    await Reject("NOT_FOUND",404,async()=>await Command(other,"cancel_order",new{orderId}));
                    var failed=await Start(orderId);var failedKey=Guid.NewGuid().ToString();
                    await Reject("NOT_FOUND",404,async()=>await Outcome(failed,"success",actor:other));
                    Check((await Outcome(failed,"failed",failedKey)).GetProperty("status").GetString()=="failed","Customer may choose failed demo scenario.");
                    Check((await Outcome(failed,"failed",failedKey)).GetProperty("status").GetString()=="failed","Failed callback replay is idempotent.");
                    var pending=await Start(orderId);var pendingKey=Guid.NewGuid().ToString();await Outcome(pending,"pending",pendingKey);await Outcome(pending,"pending",pendingKey);
                    Check((await One("SELECT status FROM payment_attempts WHERE id=$1",pending.GetProperty("id").GetGuid())).S("status")=="pending","Pending remains pending.");
                    Check((await Outcome(pending,"expired")).GetProperty("status").GetString()=="expired","Customer may expire a pending attempt.");
                    var cancelled=await Start(orderId);await Outcome(cancelled,"cancelled");
                    Check((await One("SELECT count(*)::int n FROM payment_receipts WHERE order_id=$1",orderId)).I("n")==0,"Failed/pending/expired/cancelled create no receipts.");
                    var success=await Start(orderId);var successKey=Guid.NewGuid().ToString();await Outcome(success,"success",successKey);await Outcome(success,"success",successKey);
                    await Reject("IDEMPOTENCY_CONFLICT",409,async()=>await Outcome(success,"failed",successKey));
                    Check((await One("SELECT count(*)::int n FROM payment_receipts WHERE order_id=$1",orderId)).I("n")==1,"Only one receipt on success and duplicate callback.");
                    Check((await One("SELECT count(*)::int n FROM payment_events e JOIN payment_attempts a ON a.id=e.payment_attempt_id WHERE a.order_id=$1",orderId)).I("n")==5,"Replayed outcomes do not duplicate payment events.");
                });
            }
            await test("dispatcher denies customer banking COD and foreign payment commands before cached result",async()=>
            {
                foreach(var method in new[]{"banking","cod"})
                {
                    var orderId=await Place(method,"customer-cannot-confirm-staff-only-tender");
                    if(method=="cod")await Reject("FORBIDDEN",403,async()=>await Start(orderId));
                    var attempt=await Start(orderId,method=="cod"?staff:customer);
                    await Reject("FORBIDDEN",403,async()=>await Outcome(attempt,"success"));
                    await Reject("NOT_FOUND",404,async()=>await Outcome(attempt,"success",actor:other));
                    if(method=="banking")
                    {
                        var key=Guid.NewGuid().ToString();await Outcome(attempt,"success",key,staff);
                        // Even the same key cannot lend a staff-only outcome to a customer.
                        await Reject("FORBIDDEN",403,async()=>await Outcome(attempt,"success",key));
                    }
                }
            });
            await test("dispatcher late demo success enters reconciliation and releases expired reservation",async()=>
            {
                var orderId=await Place("visa","late-success-after-explicit-test-fixture-timeout");var attempt=await Start(orderId);
                // Age only this new fixture deadline. We do not wait 30 minutes or alter a financial snapshot.
                await using(var db=Db())await new SqlStore(db).Exec("UPDATE orders SET expires_at=now()-interval '1 second' WHERE id=$1 AND order_status='pending'",orderId);
                var outcome=await Outcome(attempt,"success");Check(outcome.GetProperty("status").GetString()=="reconciliation","Late success requires reconciliation rather than fulfilment.");
                var state=await One("SELECT order_status,(SELECT count(*)::int FROM payment_receipts WHERE order_id=$1) receipts,(SELECT count(*)::int FROM inventory_reservations r JOIN order_items i ON i.id=r.order_item_id WHERE i.order_id=$1 AND r.status='active') active FROM orders WHERE id=$1",orderId);
                Check(state.S("order_status")=="reconciliation"&&state.I("receipts")==1&&state.I("active")==0,"Real late funds retained, stock released, no automatic handoff.");
            });
        }
        finally
        {
            var failures=new List<Exception>();
            foreach(var fixture in orders)try{await Cleanup(fixture);}catch(Exception error){failures.Add(error);cleanups.Add(new{orderId=fixture.Id,fixture.Method,fixture.Purpose,cancelled=false,error=error.Message});}
            await File.WriteAllTextAsync(Path.Combine(configuration["BOTZONE_ROOT"]!,"tests/BotZone.IntegrationTests/payment-dispatch-result.json"),JsonSerializer.Serialize(new{checkedAt=DateTimeOffset.UtcNow,database=new Npgsql.NpgsqlConnectionStringBuilder(connection).Database,fixtures=cleanups,noRowsDeleted=true},new JsonSerializerOptions{WriteIndented=true}));
            if(failures.Count>0)throw new AggregateException("Payment fixture cleanup needs attention.",failures);
        }
    }
}
