using BotZone.Infrastructure.Seeding;
using Npgsql;

internal static class DatabaseGuardChecks
{
    public static async Task RunAsync(string connectionString,Func<string,Func<Task>,Task> test)
    {
        await using var connection=new NpgsqlConnection(connectionString);await connection.OpenAsync();
        async Task Reject(string name,string sql,string code,params object[] values)
        {
            await test(name,async()=>
            {
                await using var tx=await connection.BeginTransactionAsync();var rejected=false;
                try
                {
                    await using var cmd=new NpgsqlCommand(sql,connection,tx);
                    for(var i=0;i<values.Length;i++)cmd.Parameters.AddWithValue("p"+i,values[i]);
                    await cmd.ExecuteNonQueryAsync();
                    await using var check=new NpgsqlCommand("SET CONSTRAINTS ALL IMMEDIATE",connection,tx);await check.ExecuteNonQueryAsync();
                }
                catch(PostgresException error)when(error.SqlState==code){rejected=true;}
                finally{await tx.RollbackAsync();}
                if(!rejected)throw new Exception("Expected PostgreSQL "+code+" for "+name);
            });
        }
        await Reject("DB guard issued cannot become draft","UPDATE invoices SET lifecycle='draft' WHERE id=@p0","23514",DemoSeeder.Id("invoice:G01"));
        await Reject("DB guard issued cannot delete","DELETE FROM invoices WHERE id=@p0","23514",DemoSeeder.Id("invoice:G01"));
        await Reject("DB guard issued line immutable","UPDATE invoice_items SET name_snapshot='tamper' WHERE id=@p0","23514",DemoSeeder.Id("invoice-item:G01:1"));
        await Reject("DB guard frozen unit immutable","UPDATE order_item_unit_amounts SET cash_planned_vnd=cash_planned_vnd WHERE id=@p0","23514",DemoSeeder.Id("unit:G01:1:1"));
        await Reject("DB guard completed refund cannot reopen","UPDATE refunds SET status='failed' WHERE id=@p0","23514",DemoSeeder.Id("refund:CN02"));
        await Reject("DB guard refund source allocations append only","DELETE FROM refund_allocations WHERE refund_id=@p0","23514",DemoSeeder.Id("refund:CN02"));
        await Reject("DB guard issued credit old parent cannot move","INSERT INTO credit_notes(id,credit_note_no,invoice_id,order_id,status,return_request_id,reason,total_vnd,approved_by_user_id,event_key) SELECT @p0,@p1,invoice_id,order_id,'draft',return_request_id,'test',total_vnd,approved_by_user_id,@p1 FROM credit_notes WHERE id=@p2; UPDATE credit_note_items SET credit_note_id=@p0 WHERE credit_note_id=@p2","23514",Guid.NewGuid(),"guard-probe-"+Guid.NewGuid(),DemoSeeder.Id("credit-note:CN01"));
        await Reject("DB guard used promotion target immutable","DELETE FROM promotion_targets WHERE id=@p0","23514",DemoSeeder.Id("promo-target"));
        await Reject("DB guard wrong allocation SKU rejected","UPDATE inventory_allocations SET lot_id=@p0 WHERE id=@p1","23514",DemoSeeder.Id("lot:AP2"),DemoSeeder.Id("allocation:"+DemoSeeder.Id("unit:G01:1:1")));
        await Reject("DB guard receipt cannot exceed approved PO","INSERT INTO goods_receipt_items(id,receipt_id,purchase_order_id,purchase_order_item_id,received_qty,unit_cost_vnd) SELECT @p0,receipt_id,purchase_order_id,purchase_order_item_id,1,unit_cost_vnd FROM goods_receipt_items WHERE id=@p1","23514",Guid.NewGuid(),DemoSeeder.Id("receipt-item:P13"));
        await Reject("DB guard tender cannot overfund unit","INSERT INTO payment_receipts(id,order_id,tender_kind,amount_vnd,source_event_key) VALUES(@p0,@p1,'cash_demo',1,@p2); INSERT INTO order_tender_allocations(order_item_unit_amount_id,receipt_id,amount_vnd) VALUES(@p3,@p0,1)","23514",Guid.NewGuid(),DemoSeeder.Id("order:G01"),"guard-probe-"+Guid.NewGuid(),DemoSeeder.Id("unit:G01:1:1"));
        await Reject("DB guard duplicate business SKU rejected","INSERT INTO product_variants(product_id,sku,option_signature,price_vnd,tracking_mode) SELECT product_id,sku,'guard-probe',price_vnd,tracking_mode FROM product_variants WHERE id=@p0","23505",DemoSeeder.Id("sku:P13"));
        await Reject("DB guard invalid FK rejected","INSERT INTO order_tender_allocations(order_item_unit_amount_id,receipt_id,amount_vnd) VALUES(@p0,@p1,1)","23503",Guid.NewGuid(),DemoSeeder.Id("payment:G01:cash_demo"));
        await Reject("DB guard unfinished order cannot commit","INSERT INTO orders(order_no,customer_id,order_status,payment_method,address_snapshot,total_vnd,expires_at) VALUES(@p0,@p1,'building','cod','{}'::jsonb,0,now())","23514","guard-probe-"+Guid.NewGuid(),DemoSeeder.CustomerId);
        await Reject("DB guard draft invoice invalid header cannot issue","UPDATE invoices SET lifecycle='issued',issued_at=now(),invoice_no=@p0,issued_by_user_id=@p1,grand_total_vnd=grand_total_vnd+1,subtotal_vnd=subtotal_vnd+1 WHERE id=@p2","23514","guard-probe-"+Guid.NewGuid(),DemoSeeder.OwnerId,DemoSeeder.Id("invoice:D01"));
    }
}
