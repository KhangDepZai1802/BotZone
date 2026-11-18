using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Infrastructure.Services;

public sealed partial class AfterSalesService
{
    Task<Dictionary<string,object?>> TradeRequest(Actor actor,Guid id)
    {
        Access.Require(actor,"customer","owner","order_staff","warehouse_staff");
        return sql.One("SELECT * FROM trade_in_requests WHERE id=$1 AND ($2<>'customer' OR customer_id=$3) FOR UPDATE",id,actor.Role,actor.Id);
    }
    static JsonElement Condition(JsonElement p)
    {
        var condition=p.Field("condition");Valid(condition.ValueKind==JsonValueKind.Object&&condition.GetRawText().Length<=4000,"Checklist tình trạng không hợp lệ.");
        var allowed=new[]{"condition","battery","screen","components","notes","note","powersOn","activationLock","identifierMatches","hardware","grade"};
        foreach(var property in condition.EnumerateObject()){Valid(allowed.Contains(property.Name),"Checklist có trường không hỗ trợ; không cung cấp mật khẩu.");Valid(property.Value.ValueKind is JsonValueKind.String or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Number,"Giá trị checklist không hợp lệ.");Valid(property.Value.ToString().Length<=1000,"Giá trị checklist quá dài.");}
        return condition;
    }
    async Task<object> RequestTradeIn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"customer");State(actor.Verified,"Cần xác minh email để gửi yêu cầu thu cũ.");var method=p.Text("receptionMethod");Valid(method is "store" or "remote","Cách tiếp nhận không hợp lệ.");var items=p.Array("items");Valid(items.Length is >=1 and <=20,"Yêu cầu cần 1–20 món.");var id=Guid.NewGuid();
        await sql.Exec("INSERT INTO trade_in_requests(id,customer_id,reception_method,state) VALUES($1,$2,$3,'requested')",id,actor.Id,method);
        foreach(var item in items)
        {
            var product=await sql.One("SELECT id FROM products WHERE id=$1",item.Id("productId"));var variant=item.MaybeId("variantId");var condition=Condition(item);var quantity=item.Int("quantity",1);Valid(quantity is >=1 and <=100,"Số lượng thu cũ không hợp lệ.");
            string? identifier=string.IsNullOrWhiteSpace(item.Text("identifier",""))?null:Identifier(item.Text("identifier"));Guid? historicUnit=null;
            if(variant.HasValue)
            {
                var sku=await sql.One("SELECT tracking_mode FROM product_variants WHERE id=$1 AND product_id=$2",variant,product.G("id"));
                if(sku.S("tracking_mode")=="serialized"){Valid(quantity==1&&identifier is not null,"Thiết bị theo serial cần một mã cho từng món.");}
            }
            if(identifier is not null)
            {
                Valid(quantity==1,"Mỗi mã định danh chỉ có một thiết bị.");State(await sql.Optional("SELECT id FROM trade_in_items WHERE identifier=$1 AND state NOT IN('completed','returned')",identifier) is null,"Thiết bị đang có một yêu cầu thu cũ khác.");
                var physical=await sql.Optional("SELECT u.id,u.custody_status FROM inventory_identifiers ii JOIN inventory_units u ON u.id=ii.inventory_unit_id WHERE ii.normalized_value=$1 LIMIT 1",identifier);
                if(physical is not null){State(physical.S("custody_status")=="shipped","Thiết bị đang trong kho hoặc hồ sơ khác.");historicUnit=physical.G("id");}
            }
            await sql.Exec("INSERT INTO trade_in_items(request_id,product_id,variant_id,qty,identifier,internal_code,condition_snapshot,state,inventory_unit_id) VALUES($1,$2,$3,$4,$5,$6,$7::jsonb,'requested',$8)",id,product.G("id"),variant,quantity,identifier,Code("TI"),condition.GetRawText(),historicUnit);
        }
        if(method=="remote")await sql.Exec("INSERT INTO trade_in_shipments(request_id,direction,tracking_no,fee_vnd,state) VALUES($1,'inbound',$2,30000,'created')",id,Code("TI-IN"));
        await Notify(actor.Id,"trade_in","Đã tiếp nhận yêu cầu. Giá cuối cần kiểm định và khách chấp nhận; BotZone chịu phí gửi/nhận.",id);return new{requestId=id,state="requested"};
    }
    async Task<object> ReceiveTradeIn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"warehouse_staff");var itemId=p.MaybeId("itemId");var requestId=p.MaybeId("requestId")??(await sql.One("SELECT request_id FROM trade_in_items WHERE id=$1",itemId)).G("request_id");var request=await TradeRequest(actor,requestId);
        State(request.S("state") is "requested" or "received","Yêu cầu không còn chờ nhận.");var note=Note(p);await sql.Exec("UPDATE trade_in_items SET state='received' WHERE request_id=$1 AND state='requested' AND ($2::uuid IS NULL OR id=$2)",requestId,itemId);
        if(request.S("reception_method")=="remote")await sql.Exec("UPDATE trade_in_shipments SET state='delivered' WHERE request_id=$1 AND direction='inbound' AND state IN('created','in_transit')",requestId);
        await RefreshTradeState(requestId);await Log(actor,"receive_trade_in","trade_in",requestId,new{itemId,note},note);return new{requestId,state="received"};
    }
    async Task<object> InspectTradeIn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"warehouse_staff");var item=await sql.One("SELECT * FROM trade_in_items WHERE id=$1 FOR UPDATE",p.Id("itemId"));var request=await TradeRequest(actor,item.G("request_id"));State(item.S("state") is "requested" or "received" or "inspected","Món không còn ở bước kiểm định.");var condition=Condition(p);
        if(request.S("reception_method")=="remote")
        {
            State(await sql.Optional("SELECT id FROM trade_in_shipments WHERE request_id=$1 AND direction='inbound' AND state='delivered'",request.G("id")) is not null,"Cần ghi nhận kiện đã đến trước khi kiểm định.");
            Valid(p.TryGetProperty("evidenceIds",out var evidence)&&evidence.ValueKind==JsonValueKind.Array&&evidence.GetArrayLength()>0,"Kiện gửi từ xa cần ảnh tình trạng khi nhận.");
        }
        var inspection=Guid.NewGuid();await sql.Exec("INSERT INTO trade_in_inspections(id,item_id,inspector_staff_id,checklist_version,assessed_condition) VALUES($1,$2,$3,1,$4::jsonb)",inspection,item.G("id"),actor.Id,condition.GetRawText());
        await Evidence(actor,p,"trade_in_evidence","inspection_id",inspection,"trade_in",request.G("id"));
        await sql.Exec("UPDATE trade_in_items SET state='inspected' WHERE id=$1",item.G("id"));
        if(item["inventory_unit_id"] is Guid unit)await sql.Exec("UPDATE inventory_units SET custody_status='quarantine',condition_status='used' WHERE id=$1",unit);
        await RefreshTradeState(request.G("id"));return new{itemId=item.G("id"),inspectionId=inspection,state="inspected"};
    }
    async Task<object> QuoteTradeIn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"owner","order_staff");var request=await TradeRequest(actor,p.Id("requestId"));State(request.S("state") is not ("completed" or "returned" or "exception"),"Yêu cầu đã đóng hoặc đang ngoại lệ.");var items=p.Array("items");Valid(items.Length is >=1 and <=20&&items.Select(i=>i.Id("itemId")).Distinct().Count()==items.Length,"Báo giá cần các món khác nhau.");
        var eligible=await sql.Rows("SELECT i.* FROM trade_in_items i WHERE i.request_id=$1 AND i.state NOT IN('completed','returned','exception') ORDER BY i.id FOR UPDATE",request.G("id"));State(eligible.Count==items.Length&&eligible.All(i=>items.Any(q=>q.Id("itemId")==i.G("id"))),"Phiên bản mới cần báo giá đủ các món chưa hoàn tất.");
        Valid(DateTimeOffset.TryParse(p.Text("expiresAt"),out var expires)&&expires.UtcDateTime>DateTime.UtcNow&&expires.UtcDateTime<=DateTime.UtcNow.AddDays(30),"Hạn báo giá phải trong 30 ngày tới.");
        bool needsApproval=false;var priceSetting=await sql.Optional("SELECT value FROM app_settings WHERE key='tradeIn'");var policy=priceSetting?.J("value")??JsonSerializer.SerializeToElement(new{});
        foreach(var item in eligible)
        {
            State(await sql.Optional("SELECT id FROM trade_in_inspections WHERE item_id=$1",item.G("id")) is not null,"Mỗi món phải được kho kiểm định.");
            var amount=items.Single(q=>q.Id("itemId")==item.G("id")).Money("offeredVnd");Valid(amount>0,"Giá đề nghị phải lớn hơn 0.");long? cap=null;
            if(policy.TryGetProperty("prices",out var prices)&&prices.ValueKind==JsonValueKind.Object)
                foreach(var code in new[]{item.S("variant_id"),item.S("product_id")})if(prices.TryGetProperty(code,out var v)&&long.TryParse(v.ToString(),out var value)&&value>0){cap=checked(value*item.I("qty"));break;}
            if(cap is null||amount>cap)needsApproval=true;
        }
        var quote=await sql.One("INSERT INTO trade_in_quotes(request_id,version_no,expires_at,quoted_by,approved_by) SELECT $1,COALESCE(max(version_no),0)+1,$2,$3,$4 FROM trade_in_quotes WHERE request_id=$1 RETURNING id,version_no",request.G("id"),expires.UtcDateTime,actor.Id,actor.Role=="owner"||!needsApproval?actor.Id:null);
        foreach(var offer in items)await sql.Exec("INSERT INTO trade_in_quote_items(quote_id,request_id,item_id,offered_vnd,decision) VALUES($1,$2,$3,$4,'pending')",quote.G("id"),request.G("id"),offer.Id("itemId"),offer.Money("offeredVnd"));
        await sql.Exec("UPDATE trade_in_items SET state='quoted' WHERE request_id=$1 AND state NOT IN('completed','returned','exception');UPDATE trade_in_requests SET state='quoted' WHERE id=$1",request.G("id"));
        await Notify(request.S("customer_id"),"trade_in","Có báo giá phiên bản mới; cần chấp nhận lại từng món sau khi được duyệt.",request.G("id"));return new{requestId=request.G("id"),quoteId=quote.G("id"),version=quote.I("version_no"),requiresOwnerApproval=needsApproval&&actor.Role!="owner"};
    }
    async Task<object> ApproveTradeInQuote(Actor actor,JsonElement p)
    {
        Access.Require(actor,"owner");var quote=await sql.One("SELECT * FROM trade_in_quotes WHERE id=$1 FOR UPDATE",p.Id("quoteId"));await TradeRequest(actor,quote.G("request_id"));State((DateTime)quote["expires_at"]!>DateTime.UtcNow,"Báo giá đã hết hạn.");
        State(await sql.Optional("SELECT id FROM trade_in_quotes WHERE request_id=$1 AND version_no>$2",quote.G("request_id"),quote.I("version_no")) is null,"Chỉ duyệt phiên bản mới nhất.");var note=Note(p,"reason");await sql.Exec("UPDATE trade_in_quotes SET approved_by=$2 WHERE id=$1",quote.G("id"),actor.Id);await Log(actor,"approve_trade_in_quote","trade_in",quote.G("request_id"),new{quoteId=quote.G("id")},note);return new{quoteId=quote.G("id"),approved=true};
    }
    async Task<object> DecideTradeIn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"customer");string decision=p.Text("decision");Valid(decision is "accepted" or "rejected","Chọn đồng ý hoặc từ chối.");var item=await sql.One("SELECT qi.*,q.version_no,q.expires_at,q.approved_by FROM trade_in_quote_items qi JOIN trade_in_quotes q ON q.id=qi.quote_id WHERE qi.id=$1 AND q.id=$2 FOR UPDATE OF qi,q",p.Id("quoteItemId"),p.Id("quoteId"));var request=await TradeRequest(actor,item.G("request_id"));
        State(await sql.Optional("SELECT id FROM trade_in_quotes WHERE request_id=$1 AND version_no>$2",request.G("id"),item.I("version_no")) is null,"Báo giá đã có phiên bản mới; vui lòng tải lại.");State(item.S("decision")=="pending","Món đã có lựa chọn; cần báo giá mới để đổi lựa chọn.");State(item["approved_by"] is not null,"Báo giá đang chờ owner duyệt.");
        if((DateTime)item["expires_at"]!<=DateTime.UtcNow){await sql.Exec("UPDATE trade_in_requests SET state='contact_pending' WHERE id=$1",request.G("id"));return new{quoteItemId=item.G("id"),state="contact_pending",expired=true};}
        await sql.Exec("UPDATE trade_in_quote_items SET decision=$2,decided_at=now() WHERE id=$1;UPDATE trade_in_items SET state=$2 WHERE id=$3",item.G("id"),decision,item.G("item_id"));
        if(decision=="rejected"&&request.S("reception_method")=="remote"&&await sql.Optional("SELECT id FROM trade_in_shipments WHERE request_id=$1 AND direction='outbound' AND state IN('created','in_transit')",request.G("id")) is null)
            await sql.Exec("INSERT INTO trade_in_shipments(request_id,direction,tracking_no,fee_vnd,state) VALUES($1,'outbound',$2,30000,'created')",request.G("id"),Code("TI-OUT"));
        await RefreshTradeState(request.G("id"));return new{quoteItemId=item.G("id"),decision};
    }
    async Task<object> FinalizeTradeIn(Actor actor,JsonElement p,string key)
    {
        Access.Require(actor,"owner","order_staff");var item=await sql.One("SELECT * FROM trade_in_items WHERE id=$1 FOR UPDATE",p.Id("itemId"));var request=await TradeRequest(actor,item.G("request_id"));var existing=await sql.Optional("SELECT delta_vnd FROM store_credit_entries WHERE trade_in_item_id=$1 AND kind='trade_in'",item.G("id"));
        if(existing is not null)return new{itemId=item.G("id"),state="completed",creditVnd=existing.S("delta_vnd"),alreadyFinalized=true};
        State(item.S("state")=="accepted"&&request.S("state")!="exception","Cần khách đồng ý sau kiểm định; ngoại lệ chưa giải quyết.");
        var quote=await sql.One("SELECT qi.*,q.version_no,q.expires_at,q.approved_by FROM trade_in_quote_items qi JOIN trade_in_quotes q ON q.id=qi.quote_id WHERE qi.id=$1 AND qi.item_id=$2 AND qi.request_id=$3 FOR UPDATE OF qi,q",p.Id("quoteItemId"),item.G("id"),request.G("id"));
        State(quote.S("decision")=="accepted"&&quote["approved_by"] is not null&&quote["decided_at"] is DateTime decided&&decided<=(DateTime)quote["expires_at"]!,"Báo giá chưa được chấp nhận hợp lệ.");
        State(await sql.Optional("SELECT id FROM trade_in_quotes WHERE request_id=$1 AND version_no>$2",request.G("id"),quote.I("version_no")) is null,"Báo giá cũ không còn hiệu lực.");State(await sql.Optional("SELECT id FROM trade_in_inspections WHERE item_id=$1",item.G("id")) is not null,"Chưa có kiểm định.");
        await sql.Exec("INSERT INTO store_credit_accounts(user_id) VALUES($1) ON CONFLICT DO NOTHING",request.S("customer_id"));await sql.One("SELECT user_id FROM store_credit_accounts WHERE user_id=$1 FOR UPDATE",request.S("customer_id"));
        await sql.Exec("INSERT INTO store_credit_entries(user_id,delta_vnd,kind,trade_in_item_id,quote_item_id,event_key) VALUES($1,$2,'trade_in',$3,$4,$5)",request.S("customer_id"),quote.L("offered_vnd"),item.G("id"),quote.G("id"),key);
        await sql.Exec("UPDATE store_credit_accounts SET balance_vnd=balance_vnd+$2 WHERE user_id=$1;UPDATE trade_in_items SET state='completed' WHERE id=$3",request.S("customer_id"),quote.L("offered_vnd"),item.G("id"));
        await RefreshTradeState(request.G("id"));await Notify(request.S("customer_id"),"trade_in","Giá trị thu cũ đã vào số dư mua hàng; không quy đổi tiền mặt.",request.G("id"));return new{itemId=item.G("id"),state="completed",creditVnd=quote.S("offered_vnd")};
    }
    async Task<object> TradeInShipment(Actor actor,JsonElement p)
    {
        Access.Require(actor,"order_staff","warehouse_staff");var shipment=await sql.One("SELECT * FROM trade_in_shipments WHERE id=$1 FOR UPDATE",p.Id("shipmentId"));var request=await TradeRequest(actor,shipment.G("request_id"));string state=p.Text("status");var note=Note(p);
        var allowed=shipment.S("state") switch{"created"=>new[]{"in_transit","delivered","exception"},"in_transit"=>["delivered","exception"],"exception"=>["in_transit"],_=>[]};State(allowed.Contains(state),"Chuyển trạng thái vận đơn không hợp lệ.");if(state=="delivered"&&shipment.S("direction")=="inbound")Access.Require(actor,"warehouse_staff");
        await sql.Exec("UPDATE trade_in_shipments SET state=$2 WHERE id=$1",shipment.G("id"),state);
        if(state=="exception")await sql.Exec("UPDATE trade_in_requests SET state='exception' WHERE id=$1",request.G("id"));
        else if(state=="delivered")
        {
            if(shipment.S("direction")=="inbound")await sql.Exec("UPDATE trade_in_items SET state='received' WHERE request_id=$1 AND state='requested'",request.G("id"));
            else
            {
                await sql.Exec("UPDATE inventory_units SET custody_status='shipped' WHERE id IN(SELECT inventory_unit_id FROM trade_in_items WHERE request_id=$1 AND state='rejected');UPDATE trade_in_items SET state='returned' WHERE request_id=$1 AND state='rejected'",request.G("id"));
            }
            await RefreshTradeState(request.G("id"));
        }
        await Log(actor,"trade_in_shipment","trade_in",request.G("id"),new{shipmentId=shipment.G("id"),state},note);return new{shipmentId=shipment.G("id"),state};
    }
    async Task RefreshTradeState(Guid id)
    {
        var items=await sql.Rows("SELECT state FROM trade_in_items WHERE request_id=$1",id);var states=items.Select(i=>i.S("state")).ToArray();
        var state=states.Any(s=>s=="exception")?"exception":states.All(s=>s is "completed" or "returned")?(states.Any(s=>s=="completed")?"completed":"returned"):states.Any(s=>s is "quoted" or "accepted" or "rejected")?"quoted":states.All(s=>s=="inspected")?"inspected":states.Any(s=>s is "received" or "inspected")?"received":"requested";
        await sql.Exec("UPDATE trade_in_requests SET state=$2 WHERE id=$1",id,state);
    }
    async Task<object> ReturnTradeIn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"warehouse_staff");var item=await sql.One("SELECT * FROM trade_in_items WHERE id=$1 FOR UPDATE",p.Id("itemId"));var request=await TradeRequest(actor,item.G("request_id"));State(item.S("state")=="rejected","Chỉ trao trả món khách đã từ chối.");State(request.S("reception_method")=="store","Món gửi từ xa cần hoàn tất vận đơn gửi trả.");var note=Note(p);
        await sql.Exec("UPDATE trade_in_items SET state='returned' WHERE id=$1",item.G("id"));if(item["inventory_unit_id"] is Guid physical)await sql.Exec("UPDATE inventory_units SET custody_status='shipped' WHERE id=$1",physical);
        await RefreshTradeState(request.G("id"));await Log(actor,"return_trade_in","trade_in",request.G("id"),new{itemId=item.G("id")},note);return new{itemId=item.G("id"),state="returned"};
    }
    async Task<object> ResolveTradeInException(Actor actor,JsonElement p)
    {
        Access.Require(actor,"owner");var request=await TradeRequest(actor,p.Id("requestId"));State(request.S("state")=="exception","Yêu cầu không ở trạng thái ngoại lệ.");var note=Note(p);State(await sql.Optional("SELECT id FROM trade_in_shipments WHERE request_id=$1 AND state='exception'",request.G("id")) is null,"Cần khắc phục vận đơn ngoại lệ trước.");await RefreshTradeState(request.G("id"));await Log(actor,"resolve_trade_in_exception","trade_in",request.G("id"),new{resolved=true},note);return new{requestId=request.G("id"),resolved=true};
    }
    public Task ExpireTradeInQuotes()=>sql.Exec("UPDATE trade_in_requests r SET state='contact_pending' WHERE r.state='quoted' AND EXISTS(SELECT 1 FROM trade_in_quotes q JOIN trade_in_quote_items qi ON qi.quote_id=q.id WHERE q.request_id=r.id AND qi.decision='pending' AND q.expires_at<now() AND q.version_no=(SELECT max(version_no) FROM trade_in_quotes WHERE request_id=r.id))");
}
