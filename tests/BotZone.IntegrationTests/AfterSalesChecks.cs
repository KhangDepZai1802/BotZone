using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Seeding;
using BotZone.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

internal static class AfterSalesChecks
{
    public static async Task RunAsync(BotZoneDbContext db,IConfiguration configuration,Func<string,Func<Task>,Task> test)
    {
        var config=new ConfigurationBuilder().AddConfiguration(configuration).AddInMemoryCollection(new Dictionary<string,string?>{{"DemoMode","true"}}).Build();
        var sql=new SqlStore(db);var commerce=new CommerceService(sql,config);var service=new AfterSalesService(sql,commerce,config);
        var owner=new Actor(DemoSeeder.OwnerId,"Owner","owner@botzone.local","owner",true);var order=owner with{Id="demo-nv01",Role="order_staff"};var warehouse=owner with{Id="demo-warehouse",Role="warehouse_staff"};var customer=owner with{Id=DemoSeeder.CustomerId,Role="customer"};
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        async Task<JsonElement> Call(Actor actor,string command,object payload)
        {
            await sql.Exec("SELECT set_config('botzone.actor_role',$1,true),set_config('botzone.actor_id',$2,true)",actor.Role,actor.Id);
            var input=JsonSerializer.SerializeToElement(payload);var key=Guid.NewGuid().ToString();
            return JsonSerializer.SerializeToElement(await service.Handle(actor,command,input,key)??await commerce.Handle(actor,command,input,key)??throw new Exception("Unknown test command."));
        }
        async Task Rejected(Func<Task> action){try{await action();}catch(BusinessException){return;}throw new Exception("Expected business rejection.");}
        async Task Scenario(string name,Func<Task> run)=>await test(name,async()=>
        {
            await using var tx=await db.Database.BeginTransactionAsync();
            try{await sql.Exec("SELECT pg_advisory_xact_lock(12648321)");await run();await sql.Exec("SET CONSTRAINTS ALL IMMEDIATE");}
            finally{await tx.RollbackAsync();}
        });
        object Product(string sku,int quantity=1,string? identifier=null)=>new{productId=DemoSeeder.Id("product:"+sku),variantId=DemoSeeder.Id("sku:"+sku),quantity,identifier,condition=new{condition="used",notes="Thiết bị kiểm thử của khách"}};
        async Task<Guid> NewOrder(string sku,int quantity,bool care,int points,string credit,bool issue=true)
        {
            var items=new[]{new{variantId=DemoSeeder.Id("sku:"+sku),quantity,care}};var cart=new{items,promotionCode="",points,creditVnd=credit};
            var quote=JsonSerializer.SerializeToElement(await commerce.Quote(customer,JsonSerializer.SerializeToElement(cart)));
            var placed=await Call(customer,"place_order",new{items,promotionCode="",points,creditVnd=credit,quoteHash=quote.GetProperty("quoteHash").GetString(),method="banking",address=new{recipient="Nguyễn Minh Anh",phone="0901234567",province="Hồ Chí Minh",ward="Phường Sài Gòn",addressLine="123 Đường Nguyễn Huệ",country="VN"}});var oid=placed.GetProperty("orderId").GetGuid();
            var attempt=await Call(customer,"start_payment",new{orderId=oid});await Call(order,"record_payment",new{attemptId=attempt.GetProperty("id").GetGuid(),outcome="success",amountVnd=quote.GetProperty("cashDueVnd").GetString()});await Call(order,"confirm_order",new{orderId=oid});
            if(issue){await Call(warehouse,"handoff",new{orderId=oid});await Call(order,"shipment_event",new{orderId=oid,status="in_transit"});await Call(order,"shipment_event",new{orderId=oid,status="delivered"});}
            return oid;
        }
        await Scenario("after-sales trade-in approval / acceptance / credit exactly once",async()=>
        {
            var balance=(await sql.One("SELECT balance_vnd FROM store_credit_accounts WHERE user_id=$1",customer.Id)).L("balance_vnd");
            var request=await Call(customer,"request_trade_in",new{receptionMethod="store",items=new[]{Product("AP2",2)}});var rid=request.GetProperty("requestId").GetGuid();var item=(await sql.One("SELECT id FROM trade_in_items WHERE request_id=$1",rid)).G("id");
            await Call(warehouse,"inspect_trade_in",new{itemId=item,condition=new{condition="used",note="Kiểm đủ tai và hộp sạc"},evidenceIds=Array.Empty<Guid>()});
            var quote=await Call(order,"quote_trade_in",new{requestId=rid,items=new[]{new{itemId=item,offeredVnd="1234567"}},expiresAt=DateTimeOffset.UtcNow.AddDays(1).ToString("O")});var qid=quote.GetProperty("quoteId").GetGuid();Check(quote.GetProperty("requiresOwnerApproval").GetBoolean(),"Unpriced offer needs owner.");
            var qi=(await sql.One("SELECT id FROM trade_in_quote_items WHERE quote_id=$1",qid)).G("id");
            await Rejected(async()=>await Call(customer,"decide_trade_in",new{quoteId=qid,quoteItemId=qi,decision="accepted"}));
            await Call(owner,"approve_trade_in_quote",new{quoteId=qid,reason="Duyệt giá sau kiểm định thực tế"});await Call(customer,"decide_trade_in",new{quoteId=qid,quoteItemId=qi,decision="accepted"});
            await Call(order,"finalize_trade_in",new{itemId=item,quoteItemId=qi});await Call(order,"finalize_trade_in",new{itemId=item,quoteItemId=qi});
            Check((await sql.One("SELECT balance_vnd FROM store_credit_accounts WHERE user_id=$1",customer.Id)).L("balance_vnd")==balance+1_234_567,"Credit applied exactly once.");Check((await sql.One("SELECT count(*)::int n FROM store_credit_entries WHERE trade_in_item_id=$1",item)).I("n")==1,"One ledger entry per item.");
        });
        await Scenario("after-sales new quote invalidates old acceptance and expiry never accepts",async()=>
        {
            var request=await Call(customer,"request_trade_in",new{receptionMethod="store",items=new[]{Product("AP2")}});var rid=request.GetProperty("requestId").GetGuid();var item=(await sql.One("SELECT id FROM trade_in_items WHERE request_id=$1",rid)).G("id");await Call(warehouse,"inspect_trade_in",new{itemId=item,condition=new{condition="used",note="Phụ kiện kiểm đủ chức năng"}});
            async Task<(Guid Quote,Guid Item)> Quote(string amount,DateTimeOffset? expires=null){var q=await Call(owner,"quote_trade_in",new{requestId=rid,items=new[]{new{itemId=item,offeredVnd=amount}},expiresAt=(expires??DateTimeOffset.UtcNow.AddDays(1)).ToString("O")});var id=q.GetProperty("quoteId").GetGuid();return(id,(await sql.One("SELECT id FROM trade_in_quote_items WHERE quote_id=$1",id)).G("id"));}
            var first=await Quote("500000");await Call(customer,"decide_trade_in",new{quoteId=first.Quote,quoteItemId=first.Item,decision="accepted"});var second=await Quote("450000",DateTimeOffset.UtcNow.AddSeconds(1));await Rejected(async()=>await Call(order,"finalize_trade_in",new{itemId=item,quoteItemId=first.Item}));
            await Task.Delay(TimeSpan.FromMilliseconds(1100));var expired=await Call(customer,"decide_trade_in",new{quoteId=second.Quote,quoteItemId=second.Item,decision="accepted"});Check(expired.GetProperty("expired").GetBoolean(),"Expiry remains pending contact without editing historical expiry.");Check((await sql.One("SELECT count(*)::int n FROM store_credit_entries WHERE trade_in_item_id=$1",item)).I("n")==0,"Expired offer does not grant credit.");
        });
        await Scenario("after-sales partial return uses fixed ordinal / mixed tenders / no new stock",async()=>
        {
            var oid=DemoSeeder.Id("order:G08");await sql.Exec("UPDATE orders SET delivered_at=now()-interval '1 day' WHERE id=$1",oid);
            var unit=DemoSeeder.Id("unit:G08:2:2");var lot=DemoSeeder.Id("lot:AP2");var before=(await sql.One("SELECT qty_on_hand FROM inventory_lots WHERE id=$1",lot)).I("qty_on_hand");
            var request=await Call(customer,"request_return",new{orderId=oid,reason="defective",items=new[]{new{orderItemId=DemoSeeder.Id("item:G08:2"),unitIds=new[]{unit}}}});var rid=request.GetProperty("returnId").GetGuid();
            await Rejected(async()=>await Call(customer,"request_return",new{orderId=oid,reason="defective",items=new[]{new{orderItemId=DemoSeeder.Id("item:G08:2"),unitIds=new[]{unit}}}}));
            await Call(order,"approve_return_intake",new{returnId=rid,approve=true,reason="Đúng cửa sổ trả và lý do lỗi"});await Call(warehouse,"inspect_return",new{returnId=rid,condition="used",eligible=true,note="Đã kiểm tra lỗi phần cứng"});
            await Rejected(async()=>await Call(owner,"approve_return",new{returnId=rid,restock=true,reason="Không được tái nhập hàng đã dùng"}));
            var cn=await Call(owner,"approve_return",new{returnId=rid,restock=false,reason="Duyệt hoàn món lỗi đã kiểm tra"});Check(cn.GetProperty("totalVnd").GetString()=="1500000","One AP2 ordinal retains exact net.");
            var failed=await Call(owner,"execute_refund",new{creditNoteId=cn.GetProperty("creditNoteId").GetGuid(),outcome="failed"});Check(failed.GetProperty("status").GetString()=="failed","Failed refund recorded.");
            var refund=await Call(owner,"execute_refund",new{creditNoteId=cn.GetProperty("creditNoteId").GetGuid(),outcome="success"});Check(refund.GetProperty("creditVnd").GetString()=="450000"&&refund.GetProperty("cashVnd").GetString()=="1050000","Refund uses original 30/70 allocations.");
            var repeat=await Call(owner,"execute_refund",new{creditNoteId=cn.GetProperty("creditNoteId").GetGuid(),outcome="success"});Check(repeat.GetProperty("totalVnd").GetString()=="0","No double refund.");
            Check((await sql.One("SELECT qty_on_hand FROM inventory_lots WHERE id=$1",lot)).I("qty_on_hand")==before,"Used returns never become saleable new stock.");Check((await sql.One("SELECT state FROM inventory_quarantine WHERE order_item_unit_amount_id=$1",unit)).S("state")=="quarantine","Returned quantity has custody record.");
        });
        await Scenario("after-sales full eligible return refunds original shipping",async()=>
        {
            var oid=DemoSeeder.Id("order:G06");await sql.Exec("UPDATE orders SET delivered_at=now()-interval '1 day' WHERE id=$1",oid);
            var request=await Call(customer,"request_return",new{orderId=oid,reason="wrong_item",items=new[]{new{orderItemId=DemoSeeder.Id("item:G06:1"),unitIds=new[]{DemoSeeder.Id("unit:G06:1:1")},actualReceivedIdentifier="Hàng khác không trùng serial dự kiến"}}});var rid=request.GetProperty("returnId").GetGuid();await Call(order,"approve_return_intake",new{returnId=rid,approve=true,reason="Khách gửi bằng chứng giao sai"});await Call(warehouse,"inspect_return",new{returnId=rid,condition="opened",eligible=true,note="Kiểm nhận đúng món giao sai"});
            var cn=await Call(owner,"approve_return",new{returnId=rid,restock=false,reason="Hoàn toàn bộ do giao sai hàng"});Check(cn.GetProperty("totalVnd").GetString()=="1830000","Full eligible return includes 30,000 original shipping.");
        });
        await Scenario("after-sales Care excludes accidental / same SKU replacement preserves dates",async()=>
        {
            var eid=(await sql.One("SELECT id FROM warranty_entitlements WHERE order_item_unit_amount_id=$1",DemoSeeder.Id("unit:G07:1:1"))).G("id");await sql.Exec("UPDATE warranty_entitlements SET state='active',starts_on=current_date-1,ends_on=current_date+364 WHERE id=$1",eid);
            var outside=await Call(customer,"request_warranty",new{entitlementId=eid,description="Thiết bị rơi vỡ sau khi sử dụng",damageKind="accidental"});Check(outside.GetProperty("state").GetString()=="rejected","Accidental excluded explicitly.");
            var claim=await Call(customer,"request_warranty",new{entitlementId=eid,description="Thiết bị gặp lỗi phần cứng thực tế",damageKind="hardware"});var cid=claim.GetProperty("claimId").GetGuid();
            await Call(order,"process_warranty",new{claimId=cid,action="receive",note="Đã tiếp nhận thiết bị của khách"});await Call(warehouse,"process_warranty",new{claimId=cid,action="inspect",note="Xác nhận lỗi phần cứng đủ điều kiện"});await Call(warehouse,"process_warranty",new{claimId=cid,action="unrepairable",note="Không thể sửa phần cứng hỏng"});
            var newUnit=(await sql.One("SELECT u.id FROM inventory_units u JOIN inventory_lots l ON l.id=u.lot_id WHERE l.variant_id=$1 AND u.custody_status='available' ORDER BY u.id LIMIT 1",DemoSeeder.Id("sku:P14"))).G("id");
            await Rejected(async()=>await Call(warehouse,"process_warranty",new{claimId=cid,action="replace",replacementUnitId=newUnit,note="Chưa có chủ duyệt phương án"}));
            var dates=await sql.One("SELECT starts_on,ends_on FROM warranty_entitlements WHERE id=$1",eid);await Call(owner,"process_warranty",new{claimId=cid,action="approve_replace",replacementUnitId=newUnit,note="Duyệt đổi cùng SKU giữ hạn cũ"});await Call(warehouse,"process_warranty",new{claimId=cid,action="replace",replacementUnitId=newUnit,note="Xuất thiết bị thay thế cùng SKU"});
            var updated=await sql.One("SELECT starts_on,ends_on,inventory_unit_id FROM warranty_entitlements WHERE id=$1",eid);Check(updated.G("inventory_unit_id")==newUnit&&Equals(dates["starts_on"],updated["starts_on"])&&Equals(dates["ends_on"],updated["ends_on"]),"Replacement keeps exact warranty dates.");
            await Call(order,"process_warranty",new{claimId=cid,action="close",note="Đã trao thiết bị thay thế cho khách"});
        });
        await Scenario("after-sales role / ownership and return expiry",async()=>
        {
            await Rejected(async()=>await Call(owner with{Role="content_staff"},"execute_refund",new{creditNoteId=DemoSeeder.Id("credit-note:CN02")}));
            await Rejected(async()=>await Call(customer with{Id="demo-customer-other"},"request_return",new{orderId=DemoSeeder.Id("order:G08"),reason="defective",items=Array.Empty<object>()}));
            await Rejected(async()=>await Call(customer,"request_return",new{orderId=DemoSeeder.Id("order:G04"),reason="defective",items=Array.Empty<object>()}));
        });
        await Scenario("after-sales actual checkout partial odd VND and points keep frozen unit",async()=>
        {
            await sql.Exec("UPDATE product_variants SET price_vnd=20001 WHERE id=$1",DemoSeeder.Id("sku:AP2"));var pointsBefore=(await sql.One("SELECT balance_points FROM loyalty_accounts WHERE user_id=$1",customer.Id)).I("balance_points");
            var oid=await NewOrder("AP2",2,false,1,"10001");var units=await sql.Rows("SELECT u.*,i.id item_id FROM order_item_unit_amounts u JOIN order_items i ON i.id=u.order_item_id WHERE i.order_id=$1 AND i.line_type='product' ORDER BY u.unit_ordinal",oid);Check(units.Count==2&&units[0].I("redeemed_points")==1&&units[1].I("redeemed_points")==0,"Integer point distributed to one stable ordinal.");var first=units[0];
            var request=await Call(customer,"request_return",new{orderId=oid,reason="defective",items=new[]{new{orderItemId=first.G("item_id"),unitIds=new[]{first.G("id")}}}});var rid=request.GetProperty("returnId").GetGuid();await Call(order,"approve_return_intake",new{returnId=rid,approve=true,reason="Tiếp nhận trả một món bị lỗi"});await Call(warehouse,"inspect_return",new{returnId=rid,condition="faulty",eligible=true,note="Kho kiểm tra lỗi đơn vị số một"});var cn=await Call(owner,"approve_return",new{returnId=rid,restock=false,reason="Duyệt đúng tiền frozen của ordinal"});
            Check(cn.GetProperty("totalVnd").GetString()==first.S("net_amount_vnd"),"Credit uses stored unit net; points not refunded as money.");var refund=await Call(owner,"execute_refund",new{creditNoteId=cn.GetProperty("creditNoteId").GetGuid(),outcome="success"});Check(refund.GetProperty("creditVnd").GetString()==first.S("credit_planned_vnd")&&refund.GetProperty("cashVnd").GetString()==first.S("cash_planned_vnd"),"Odd VND refunds original funded components.");Check((await sql.One("SELECT balance_points FROM loyalty_accounts WHERE user_id=$1",customer.Id)).I("balance_points")==pointsBefore,"Redeemed point restored once.");
        });
        await Scenario("after-sales paid unissued cancellation refunds original sources once",async()=>
        {
            var before=await sql.One("SELECT sc.balance_vnd,la.balance_points FROM store_credit_accounts sc JOIN loyalty_accounts la ON la.user_id=sc.user_id WHERE sc.user_id=$1",customer.Id);var oid=await NewOrder("AP2",1,false,1,"10000",false);
            await Call(customer,"cancel_order",new{orderId=oid});var refund=await Call(owner,"execute_refund",new{orderId=oid,outcome="success"});Check(refund.GetProperty("creditVnd").GetString()=="10000","Consumed credit restored to credit.");Check((await sql.One("SELECT balance_vnd FROM store_credit_accounts WHERE user_id=$1",customer.Id)).L("balance_vnd")==before.L("balance_vnd"),"Balance exactly restored.");Check((await sql.One("SELECT balance_points FROM loyalty_accounts WHERE user_id=$1",customer.Id)).I("balance_points")==before.I("balance_points"),"Cancellation point restore is not duplicated by refund.");
            var repeated=await Call(owner,"execute_refund",new{orderId=oid,outcome="success"});Check(repeated.GetProperty("totalVnd").GetString()=="0","No double unissued refund.");Check(await sql.Optional("SELECT id FROM invoices WHERE order_id=$1 AND lifecycle='issued'",oid) is null,"No issued invoice invented.");
        });
        await Scenario("after-sales quantity Care replacement records lot custody without revenue",async()=>
        {
            await sql.Exec("UPDATE product_variants SET care_fee_vnd=100001 WHERE id=$1",DemoSeeder.Id("sku:AP2"));var oid=await NewOrder("AP2",1,true,0,"0");var entitlement=await sql.One("SELECT e.* FROM warranty_entitlements e JOIN order_item_unit_amounts u ON u.id=e.order_item_unit_amount_id JOIN order_items i ON i.id=u.order_item_id WHERE i.order_id=$1",oid);var eid=entitlement.G("id");await sql.Exec("UPDATE warranty_entitlements SET starts_on=current_date-1,ends_on=current_date+364 WHERE id=$1",eid);
            var claim=await Call(customer,"request_warranty",new{entitlementId=eid,damageKind="hardware",description="Thiết bị quantity lỗi phần cứng"});var cid=claim.GetProperty("claimId").GetGuid();await Call(order,"process_warranty",new{claimId=cid,action="receive",note="Đã nhận sản phẩm không serial"});await Call(warehouse,"process_warranty",new{claimId=cid,action="inspect",note="Kiểm đúng lỗi phần cứng được phủ"});await Call(warehouse,"process_warranty",new{claimId=cid,action="unrepairable",note="Không sửa được, cần thay cùng SKU"});await Call(owner,"process_warranty",new{claimId=cid,action="approve_replace",note="Duyệt thay cùng SKU và lô đã nhận"});await Call(warehouse,"process_warranty",new{claimId=cid,action="replace",replacementLotId=DemoSeeder.Id("lot:AP2"),note="Xuất một sản phẩm cùng SKU từ lô"});
            var replacement=await sql.One("SELECT * FROM warranty_replacements WHERE claim_id=$1",cid);Check(replacement["old_unit_id"] is null&&replacement["new_unit_id"] is null&&replacement.G("new_lot_id")==DemoSeeder.Id("lot:AP2"),"Quantity replacement uses exact lot and order ordinal.");Check((await sql.One("SELECT count(*)::int n FROM invoices WHERE order_id=$1",oid)).I("n")==1,"Replacement does not create another sale.");
        });
        await Scenario("after-sales failed COD delivery closes invoice with no fake refund",async()=>
        {
            var oid=DemoSeeder.Id("order:G02");await Call(order,"shipment_event",new{orderId=oid,status="failed"});await Call(order,"shipment_event",new{orderId=oid,status="returned"});var shipment=(await sql.One("SELECT id FROM shipments WHERE order_id=$1",oid)).G("id");
            await Rejected(async()=>await Call(owner,"finalize_failed_delivery",new{shipmentId=shipment,reason="Chưa có kho xác nhận thu hồi"}));await Call(warehouse,"inspect_failed_shipment",new{shipmentId=shipment,condition="opened",note="Đã thu hồi đủ kiện COD chưa thu tiền"});var cn=await Call(owner,"finalize_failed_delivery",new{shipmentId=shipment,reason="Kết thúc giao thất bại đã thu hồi"});Check(cn.GetProperty("totalVnd").GetString()=="16000000","Full failed delivery credited.");var refund=await Call(owner,"execute_refund",new{creditNoteId=cn.GetProperty("creditNoteId").GetGuid(),outcome="success"});Check(refund.GetProperty("totalVnd").GetString()=="0","Unpaid COD has no refundable receipt.");
        });
        await Scenario("after-sales unopened failed-delivery stock requires owner approval",async()=>
        {
            var oid=DemoSeeder.Id("order:G02");await Call(order,"shipment_event",new{orderId=oid,status="failed"});await Call(order,"shipment_event",new{orderId=oid,status="returned"});var shipment=(await sql.One("SELECT id FROM shipments WHERE order_id=$1",oid)).G("id");var original=(await sql.One("SELECT inventory_unit_id FROM inventory_allocations a JOIN order_items i ON i.id=a.order_item_id WHERE i.order_id=$1 AND a.inventory_unit_id IS NOT NULL",oid)).G("inventory_unit_id");
            await Call(warehouse,"inspect_failed_shipment",new{shipmentId=shipment,condition="new",note="Kiện còn niêm phong và đủ chuẩn mới"});Check((await sql.One("SELECT custody_status FROM inventory_units WHERE id=$1",original)).S("custody_status")=="quarantine","Inspection alone cannot add sellable stock.");await Call(owner,"finalize_failed_delivery",new{shipmentId=shipment,restock=true,reason="Duyệt tồn mới theo kiểm tra niêm phong"});Check((await sql.One("SELECT custody_status FROM inventory_units WHERE id=$1",original)).S("custody_status")=="available","Owner approved inspected new return enters available stock.");
        });
        await Scenario("after-sales returned product cancels and refunds unused Care",async()=>
        {
            var oid=DemoSeeder.Id("order:G08");await sql.Exec("UPDATE orders SET delivered_at=now()-interval '1 day' WHERE id=$1",oid);var product=DemoSeeder.Id("unit:G08:1:1");var request=await Call(customer,"request_return",new{orderId=oid,reason="defective",items=new[]{new{orderItemId=DemoSeeder.Id("item:G08:1"),unitIds=new[]{product}}}});var rid=request.GetProperty("returnId").GetGuid();await Call(order,"approve_return_intake",new{returnId=rid,approve=true,reason="Đúng điều kiện trả máy lỗi"});await Call(warehouse,"inspect_return",new{returnId=rid,condition="faulty",eligible=true,note="Thiết bị lỗi, Care chưa sử dụng"});var cn=await Call(owner,"approve_return",new{returnId=rid,restock=false,reason="Hoàn máy lỗi và Care chưa dùng"});Check(cn.GetProperty("totalVnd").GetString()=="7500000","Refund net7m product plus500k unusedCare.");Check((await sql.One("SELECT state FROM warranty_entitlements WHERE order_item_unit_amount_id=$1",product)).S("state")=="refunded","Care cancelled with original product.");
        });
        await Scenario("after-sales remote rejected trade-in records both freight directions without credit",async()=>
        {
            var request=await Call(customer,"request_trade_in",new{receptionMethod="remote",items=new[]{Product("AP2")}});var rid=request.GetProperty("requestId").GetGuid();var item=(await sql.One("SELECT id FROM trade_in_items WHERE request_id=$1",rid)).G("id");await Rejected(async()=>await Call(warehouse,"inspect_trade_in",new{itemId=item,condition=new{condition="used",note="Chưa nhận kiện từ xa"}}));
            await Call(warehouse,"receive_trade_in",new{requestId=rid,note="Đã nhận kiện gửi từ xa của khách"});
            // A validated private image fixture; physical PNG bytes are kept with test artifacts.
            var bytes=TinyPng();MediaService.ValidateImage(bytes);var mediaId=Guid.NewGuid();var storageKey="evidence-private/"+mediaId+".png";var root=configuration["BOTZONE_ROOT"]!;var file=Path.Combine(root,configuration["UPLOAD_DIR"]!,storageKey);Directory.CreateDirectory(Path.GetDirectoryName(file)!);await File.WriteAllBytesAsync(file,bytes);
            await sql.Exec("INSERT INTO media_assets(id,storage_key,visibility,mime,size_bytes,checksum,validation_status,source_url,license_note,owner_user_id,metadata) VALUES($1,$2,'private','image/png',$3,$4,'valid','original:integration-fixture','Test fixture', $5,$6::jsonb)",mediaId,storageKey,(long)bytes.Length,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)),warehouse.Id,Values.Json(new{referenceType="trade_in",referenceId=rid}));
            await Call(warehouse,"inspect_trade_in",new{itemId=item,condition=new{condition="used",note="Kiểm định kiện và đủ thành phần"},evidenceIds=new[]{mediaId}});var quote=await Call(owner,"quote_trade_in",new{requestId=rid,items=new[]{new{itemId=item,offeredVnd="250000"}},expiresAt=DateTimeOffset.UtcNow.AddDays(1).ToString("O")});var qid=quote.GetProperty("quoteId").GetGuid();var qi=(await sql.One("SELECT id FROM trade_in_quote_items WHERE quote_id=$1",qid)).G("id");await Call(customer,"decide_trade_in",new{quoteId=qid,quoteItemId=qi,decision="rejected"});var outbound=(await sql.One("SELECT id FROM trade_in_shipments WHERE request_id=$1 AND direction='outbound'",rid)).G("id");await Call(order,"trade_in_shipment",new{shipmentId=outbound,status="delivered",note="Đã gửi trả và khách nhận đủ hàng"});Check((await sql.One("SELECT count(*)::int n,sum(fee_vnd) fee FROM trade_in_shipments WHERE request_id=$1",rid)).L("fee")==60000,"Both freight costs remain operational, not deducted from offer.");Check((await sql.One("SELECT count(*)::int n FROM store_credit_entries WHERE trade_in_item_id=$1",item)).I("n")==0,"Decline gives no credit.");Check((await sql.One("SELECT state FROM trade_in_items WHERE id=$1",item)).S("state")=="returned","Rejected remote item physically returned.");
        });
        await Scenario("policy point value 2000 applies only to new frozen units",async()=>
        {
            var old=await sql.One("SELECT point_value_vnd,points_discount_vnd FROM order_item_unit_amounts WHERE id=$1",DemoSeeder.Id("unit:G08:1:1"));await sql.Exec("UPDATE app_settings SET value=jsonb_set(value,'{pointValueVnd}','\"2000\"'::jsonb) WHERE key='commerce'");
            var oid=await NewOrder("AP2",1,false,1,"0");var units=await sql.Rows("SELECT u.* FROM order_item_unit_amounts u JOIN order_items i ON i.id=u.order_item_id WHERE i.order_id=$1 AND i.line_type='product'",oid);Check(units.Sum(u=>u.I("redeemed_points"))==1&&units.Sum(u=>u.L("points_discount_vnd"))==2000&&units.All(u=>u.L("point_value_vnd")==2000),"New order freezes2000 perpoint.");var unchanged=await sql.One("SELECT point_value_vnd,points_discount_vnd FROM order_item_unit_amounts WHERE id=$1",DemoSeeder.Id("unit:G08:1:1"));Check(old.S("point_value_vnd")==unchanged.S("point_value_vnd")&&old.S("points_discount_vnd")==unchanged.S("points_discount_vnd"),"Existing order unit snapshot remains unchanged.");
        });
    }
    private static byte[] TinyPng()
    {
        using var output=new MemoryStream();output.Write([137,80,78,71,13,10,26,10]);
        void Chunk(string type,byte[] data)
        {
            var name=System.Text.Encoding.ASCII.GetBytes(type);Span<byte> integer=stackalloc byte[4];System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(integer,data.Length);output.Write(integer);output.Write(name);output.Write(data);
            uint crc=uint.MaxValue;foreach(var b in name.Concat(data)){crc^=b;for(var n=0;n<8;n++)crc=(crc&1)!=0?0xedb88320^(crc>>1):crc>>1;}System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(integer,~crc);output.Write(integer);
        }
        var header=new byte[13];header[3]=1;header[7]=1;header[8]=8;header[9]=6;Chunk("IHDR",header);
        using var compressed=new MemoryStream();using(var zlib=new System.IO.Compression.ZLibStream(compressed,System.IO.Compression.CompressionLevel.SmallestSize,true))zlib.Write([0,255,255,255,255]);Chunk("IDAT",compressed.ToArray());Chunk("IEND",[]);return output.ToArray();
    }
}
