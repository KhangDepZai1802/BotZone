using System.Globalization;
using System.Numerics;
using System.Text.Json;
using BotZone.Contracts;
using Microsoft.Extensions.Configuration;

namespace BotZone.Infrastructure.Services;

/// <summary>Runs inside CommandService's transaction/idempotency/audit boundary.</summary>
public sealed partial class AfterSalesService(SqlStore sql,CommerceService commerce,IConfiguration config)
{
    public async Task<object?> Handle(Actor actor,string command,JsonElement input,string key)=>command switch
    {
        "request_return"=>await RequestReturn(actor,input),
        "approve_return_intake"=>await ApproveIntake(actor,input),
        "receive_return"=>await ReceiveReturn(actor,input),
        "inspect_return"=>await InspectReturn(actor,input),
        "approve_return"=>await ApproveReturn(actor,input,key),
        "resolve_return"=>await ResolveReturn(actor,input),
        "execute_refund"=>await Refund(actor,input,key),
        "request_trade_in"=>await RequestTradeIn(actor,input),
        "receive_trade_in"=>await ReceiveTradeIn(actor,input),
        "inspect_trade_in"=>await InspectTradeIn(actor,input),
        "quote_trade_in"=>await QuoteTradeIn(actor,input),
        "approve_trade_in_quote"=>await ApproveTradeInQuote(actor,input),
        "decide_trade_in"=>await DecideTradeIn(actor,input),
        "finalize_trade_in"=>await FinalizeTradeIn(actor,input,key),
        "trade_in_shipment"=>await TradeInShipment(actor,input),
        "return_trade_in"=>await ReturnTradeIn(actor,input),
        "resolve_trade_in_exception"=>await ResolveTradeInException(actor,input),
        "request_warranty"=>await RequestWarranty(actor,input),
        "process_warranty"=>await ProcessWarranty(actor,input,key),
        "close_failed_shipment"=>await CloseFailedShipment(actor,input,key),
        "finalize_failed_delivery"=>await CloseFailedShipment(actor,input,key),
        "inspect_failed_shipment"=>await InspectFailedShipment(actor,input),
        _=>null
    };
    static void Valid(bool condition,string message)=>Access.Ensure(condition,"VALIDATION_ERROR",message,400);
    static void State(bool condition,string message)=>Access.Ensure(condition,"STATE_CONFLICT",message);
    static string Note(JsonElement p,string field="note"){var note=p.Text(field).Trim();Valid(note.Length is >=5 and <=2000,"Ghi chú cần 5–2.000 ký tự.");return note;}
    static string Code(string prefix)=>$"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
    static string Identifier(string value){var normalized=new string(value.Trim().ToUpperInvariant().Where(c=>!char.IsWhiteSpace(c)&&c!='-').ToArray());Valid(normalized.Length is >=3 and <=100 && normalized.All(char.IsAsciiLetterOrDigit),"Mã thiết bị chỉ gồm chữ/số, tối đa 100 ký tự.");return normalized;}
    static Guid[] Ids(JsonElement value){Valid(value.ValueKind==JsonValueKind.Array,"Cần danh sách UUID.");var ids=value.EnumerateArray().Select(v=>v.ValueKind==JsonValueKind.String&&Guid.TryParse(v.GetString(),out var id)?id:throw new BusinessException("VALIDATION_ERROR","Mã đơn vị không hợp lệ.")).ToArray();Valid(ids.Length is >=1 and <=100 && ids.Distinct().Count()==ids.Length,"Chọn 1–100 đơn vị khác nhau.");return ids;}
    Task Log(Actor actor,string action,string entity,Guid id,object data,string note="")=>sql.Exec("INSERT INTO audit_events(actor_user_id,action,entity_type,entity_id,after_data,reason) VALUES($1,$2,$3,$4,$5::jsonb,$6)",actor.Id,action,entity,id.ToString(),Values.Json(data),note);
    Task Notify(string customer,string type,string message,Guid entity)=>sql.Exec("INSERT INTO notifications(recipient_user_id,type,message,entity_id) VALUES($1,$2,$3,$4)",customer,type,message,entity);
    async Task Evidence(Actor actor,JsonElement p,string table,string column,Guid id,string referenceType,Guid referenceId)
    {
        if(!p.TryGetProperty("evidenceIds",out var data))return;
        Valid(data.ValueKind==JsonValueKind.Array,"Ảnh bằng chứng cần danh sách.");var ids=data.EnumerateArray().Select(v=>v.ValueKind==JsonValueKind.String&&Guid.TryParse(v.GetString(),out var id)?id:throw new BusinessException("VALIDATION_ERROR","Mã ảnh không hợp lệ.")).Distinct().ToArray();Valid(ids.Length<=6,"Tối đa sáu ảnh bằng chứng.");
        foreach(var assetId in ids)
        {
            var asset=await sql.One("SELECT * FROM media_assets WHERE id=$1 AND visibility='private' AND validation_status='valid'",assetId);
            var meta=asset.J("metadata");State(meta.Text("referenceType","")==referenceType&&meta.Text("referenceId","")==referenceId.ToString(),"Ảnh bằng chứng không thuộc hồ sơ nguồn.");
            if(actor.Role=="customer")Access.Ensure(asset.S("owner_user_id")==actor.Id,"FORBIDDEN","Không được dùng ảnh riêng của khách khác.",403);
            // Table/column are constants at each call site.
            await sql.Exec($"INSERT INTO {table}({column},media_id) VALUES($1,$2) ON CONFLICT DO NOTHING",id,assetId);
        }
    }
    Task<Dictionary<string,object?>> Return(Actor actor,Guid id)
    {
        Access.Require(actor,"customer","owner","order_staff","warehouse_staff");
        return sql.One("SELECT r.* FROM return_requests r WHERE r.id=$1 AND ($2<>'customer' OR r.customer_id=$3) FOR UPDATE",id,actor.Role,actor.Id);
    }
    async Task<object> RequestReturn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"customer");State(actor.Verified,"Hãy xác minh email trước khi yêu cầu trả hàng.");var order=await commerce.Order(actor,p.Id("orderId"));
        State(order["delivered_at"] is DateTime,"Chỉ yêu cầu trả hàng đã giao thành công.");var delivered=(DateTime)order["delivered_at"]!;
        var days=order.J("policy_snapshot").Int("returnDays",14);State(DateTime.UtcNow<=delivered.AddDays(Math.Clamp(days,1,60)),"Đã hết thời hạn yêu cầu trả hàng theo đơn.");
        string reason=p.Text("reason");Valid(reason is "defective" or "wrong_item","Chỉ trả hàng do lỗi hoặc giao sai.");var items=p.Array("items");Valid(items.Length is >=1 and <=30,"Chọn 1–30 dòng hàng.");
        Valid(items.Select(i=>i.Id("orderItemId")).Distinct().Count()==items.Length,"Dòng hàng không được lặp.");var rid=Guid.NewGuid();
        await sql.Exec("INSERT INTO return_requests(id,order_id,customer_id,state,reason,fee_vnd) VALUES($1,$2,$3,'requested',$4,0)",rid,order.G("id"),actor.Id,reason);
        foreach(var item in items)
        {
            var orderItem=await sql.One("SELECT * FROM order_items WHERE id=$1 AND order_id=$2 AND line_type='product'",item.Id("orderItemId"),order.G("id"));
            Guid[] ids;
            if(item.TryGetProperty("unitIds",out var selected))ids=Ids(selected);
            else
            {
                var quantity=item.Int("quantity");Valid(quantity is >=1 and <=100,"Số lượng trả không hợp lệ.");
                var available=await sql.Rows("SELECT u.id FROM order_item_unit_amounts u WHERE u.order_item_id=$1 AND NOT EXISTS(SELECT 1 FROM return_item_units ru JOIN return_items ri ON ri.id=ru.return_item_id JOIN return_requests r ON r.id=ri.return_request_id WHERE ru.order_item_unit_amount_id=u.id AND r.state<>'rejected') AND NOT EXISTS(SELECT 1 FROM credit_note_item_units c WHERE c.order_item_unit_amount_id=u.id) ORDER BY u.unit_ordinal LIMIT $2 FOR UPDATE",orderItem.G("id"),quantity);
                State(available.Count==quantity,"Số lượng đã hoặc đang được trả.");ids=available.Select(r=>r.G("id")).ToArray();
            }
            var units=await sql.Rows("SELECT u.id,a.inventory_unit_id FROM order_item_unit_amounts u JOIN inventory_allocations a ON a.order_item_unit_amount_id=u.id AND a.state='shipped' WHERE u.order_item_id=$1 AND u.id=ANY($2) ORDER BY u.unit_ordinal FOR UPDATE OF u,a",orderItem.G("id"),ids);
            State(units.Count==ids.Length,"Đơn vị hàng chưa giao hoặc không thuộc dòng đã chọn.");
            State(await sql.Optional("SELECT ru.order_item_unit_amount_id FROM return_item_units ru JOIN return_items ri ON ri.id=ru.return_item_id JOIN return_requests r ON r.id=ri.return_request_id WHERE ru.order_item_unit_amount_id=ANY($1) AND r.state<>'rejected' UNION ALL SELECT order_item_unit_amount_id FROM credit_note_item_units WHERE order_item_unit_amount_id=ANY($1) LIMIT 1",ids) is null,"Một đơn vị đã có yêu cầu trả hoặc điều chỉnh.");
            var actual=item.Text("actualReceivedIdentifier","").Trim();Valid(actual.Length<=160,"Mã món thực nhận quá dài.");if(reason=="wrong_item")Valid(actual.Length>0,"Giao sai cần mô tả mã/món thực nhận.");
            var ri=Guid.NewGuid();await sql.Exec("INSERT INTO return_items(id,return_request_id,order_id,order_item_id,qty,actual_received_identifier) VALUES($1,$2,$3,$4,$5,$6)",ri,rid,order.G("id"),orderItem.G("id"),units.Count,actual.Length>0?actual:null);
            foreach(var unit in units)await sql.Exec("INSERT INTO return_item_units(return_item_id,order_item_id,order_item_unit_amount_id,inventory_unit_id) VALUES($1,$2,$3,$4)",ri,orderItem.G("id"),unit.G("id"),reason=="wrong_item"?null:unit["inventory_unit_id"]);
        }
        await Evidence(actor,p,"return_evidence","return_request_id",rid,"order",order.G("id"));
        await Notify(actor.Id,"return","Đã nhận yêu cầu trả hàng; chờ hướng dẫn gửi trả.",rid);return new{returnId=rid,state="requested"};
    }
    async Task<object> ApproveIntake(Actor actor,JsonElement p)
    {
        Access.Require(actor,"order_staff");var r=await Return(actor,p.Id("returnId"));State(r.S("state")=="requested","Yêu cầu không còn chờ tiếp nhận.");var note=Note(p,"reason");var state=p.Bool("approve")?"approved":"rejected";
        await sql.Exec("UPDATE return_requests SET state=$2,inspection_result=$3 WHERE id=$1",r.G("id"),state,Values.Json(new{intakeNote=note,approved=p.Bool("approve")}));await Notify(r.S("customer_id"),"return",note,r.G("id"));return new{returnId=r.G("id"),state};
    }
    async Task<object> ReceiveReturn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"warehouse_staff");var r=await Return(actor,p.Id("returnId"));State(r.S("state")=="approved","Cần duyệt gửi trả trước khi nhận.");var note=Note(p);
        await QuarantineReturn(r);await sql.Exec("UPDATE return_requests SET state='received',inspection_result=$2 WHERE id=$1",r.G("id"),Values.Json(new{receivedNote=note}));await Log(actor,"receive_return","return",r.G("id"),new{state="received"},note);return new{returnId=r.G("id"),state="received"};
    }
    async Task QuarantineReturn(Dictionary<string,object?> r)
    {
        await sql.Exec("UPDATE inventory_units SET custody_status='quarantine',condition_status=CASE WHEN condition_status='new' THEN 'opened' ELSE condition_status END WHERE id IN(SELECT ru.inventory_unit_id FROM return_item_units ru JOIN return_items ri ON ri.id=ru.return_item_id WHERE ri.return_request_id=$1)",r.G("id"));
        if(r.S("reason")!="wrong_item")await sql.Exec("INSERT INTO inventory_quarantine(order_item_unit_amount_id,lot_id,quantity,state,reason) SELECT ru.order_item_unit_amount_id,a.lot_id,1,'quarantine','Return received pending inspection' FROM return_item_units ru JOIN return_items ri ON ri.id=ru.return_item_id JOIN inventory_allocations a ON a.order_item_unit_amount_id=ru.order_item_unit_amount_id AND a.state='shipped' WHERE ri.return_request_id=$1 AND a.inventory_unit_id IS NULL ON CONFLICT(order_item_unit_amount_id,lot_id) DO UPDATE SET state='quarantine'",r.G("id"));
    }
    async Task<object> InspectReturn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"warehouse_staff");var r=await Return(actor,p.Id("returnId"));State(r.S("state") is "approved" or "received","Hàng chưa được duyệt nhận hoặc đã kiểm tra.");var condition=p.Text("condition");Valid(condition is "new" or "opened" or "used" or "faulty","Tình trạng không hợp lệ.");var note=Note(p);
        await QuarantineReturn(r);await sql.Exec("UPDATE inventory_units SET condition_status=$2 WHERE id IN(SELECT ru.inventory_unit_id FROM return_item_units ru JOIN return_items ri ON ri.id=ru.return_item_id WHERE ri.return_request_id=$1)",r.G("id"),condition);
        await sql.Exec("UPDATE return_requests SET state='inspected',inspection_result=$2,inspected_by=$3,inspected_at=now() WHERE id=$1",r.G("id"),Values.Json(new{condition,eligible=p.Bool("eligible"),note}),actor.Id);
        await Evidence(actor,p,"return_evidence","return_request_id",r.G("id"),"order",r.G("order_id"));return new{returnId=r.G("id"),state="inspected"};
    }
    async Task<object> ApproveReturn(Actor actor,JsonElement p,string key)
    {
        Access.Require(actor,"owner");var r=await Return(actor,p.Id("returnId"));State(r.S("state")=="inspected","Cần kết quả kiểm tra trước khi duyệt tài chính.");var inspection=JsonDocument.Parse(r.S("inspection_result")).RootElement;var note=Note(p,"reason");
        if(!inspection.Bool("eligible")){await sql.Exec("UPDATE return_requests SET state='rejected' WHERE id=$1",r.G("id"));await Notify(r.S("customer_id"),"return","Không đủ điều kiện hoàn; nhân viên sẽ liên hệ trả lại hàng. "+note,r.G("id"));return new{returnId=r.G("id"),state="rejected",requiresReturnDisposition=true};}
        bool restock=p.Bool("restock");State(!restock||inspection.Text("condition")=="new","Hàng đã dùng/mở/lỗi không được về tồn mới.");State(!restock||r.S("reason")!="wrong_item","Món giao sai cần xử lý ngoại lệ; không nhập lại SKU trên đơn gốc.");
        var rows=await sql.Rows("SELECT ru.order_item_unit_amount_id,ru.inventory_unit_id,oi.variant_id,a.id allocation_id,a.lot_id FROM return_item_units ru JOIN return_items ri ON ri.id=ru.return_item_id JOIN order_items oi ON oi.id=ri.order_item_id JOIN inventory_allocations a ON a.order_item_unit_amount_id=ru.order_item_unit_amount_id AND a.state='shipped' WHERE ri.return_request_id=$1 ORDER BY ru.order_item_unit_amount_id FOR UPDATE OF a",r.G("id"));
        State(rows.Count>0,"Không có đơn vị hợp lệ để trả.");var units=rows.Select(x=>x.G("order_item_unit_amount_id")).ToList();
        foreach(var row in rows)
        {
            await sql.Exec("UPDATE inventory_allocations SET state='returned' WHERE id=$1",row.G("allocation_id"));
            if(row["inventory_unit_id"] is Guid physical)await sql.Exec("UPDATE inventory_units SET custody_status=$2 WHERE id=$1",physical,restock?"available":"quarantine");
            else if(restock)await sql.Exec("UPDATE inventory_lots SET qty_on_hand=qty_on_hand+1 WHERE id=$1",row.G("lot_id"));
            if(restock)await sql.Exec("UPDATE inventory_quarantine SET state='released' WHERE order_item_unit_amount_id=$1",row.G("order_item_unit_amount_id"));
            await sql.Exec("INSERT INTO stock_movements(variant_id,lot_id,unit_id,quantity_delta,kind,source_event_key,actor_user_id,reason) VALUES($1,$2,$3,$4,'return',$5,$6,$7)",row.G("variant_id"),row.G("lot_id"),row["inventory_unit_id"],restock?1:0,"return:"+row.G("allocation_id"),actor.Id,note);
        }
        await IncludeUnusedCare(units);
        var unreturned=await sql.One("SELECT count(*)::int n FROM order_item_unit_amounts u JOIN order_items i ON i.id=u.order_item_id WHERE i.order_id=$1 AND i.line_type='product' AND NOT(u.id=ANY($2)) AND NOT EXISTS(SELECT 1 FROM credit_note_item_units cu JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id JOIN credit_notes c ON c.id=ci.credit_note_id WHERE cu.order_item_unit_amount_id=u.id AND c.status='issued')",r.G("order_id"),units.ToArray());
        if(unreturned.I("n")==0)units.AddRange((await sql.Rows("SELECT u.id FROM order_item_unit_amounts u JOIN order_items i ON i.id=u.order_item_id WHERE i.order_id=$1 AND i.line_type='shipping' AND NOT EXISTS(SELECT 1 FROM credit_note_item_units c WHERE c.order_item_unit_amount_id=u.id)",r.G("order_id"))).Select(u=>u.G("id")));
        var result=await Credit(actor,r.G("order_id"),units.ToArray(),note,key,returnId:r.G("id"));await sql.Exec("UPDATE return_requests SET state='credited' WHERE id=$1",r.G("id"));return result;
    }
    async Task IncludeUnusedCare(List<Guid> units)
    {
        foreach(var entitlement in await sql.Rows("SELECT * FROM warranty_entitlements WHERE order_item_unit_amount_id=ANY($1) FOR UPDATE",units.ToArray()))
        {
            await sql.Exec("UPDATE warranty_entitlements SET state=$2 WHERE id=$1",entitlement.G("id"),entitlement.B("used")?"cancelled":"refunded");
            if(!entitlement.B("used"))units.AddRange((await sql.Rows("SELECT id FROM order_item_unit_amounts WHERE parent_unit_amount_id=$1 AND NOT EXISTS(SELECT 1 FROM credit_note_item_units c WHERE c.order_item_unit_amount_id=order_item_unit_amounts.id)",entitlement.G("order_item_unit_amount_id"))).Select(u=>u.G("id")));
        }
    }
    async Task<object> ResolveReturn(Actor actor,JsonElement p)
    {
        Access.Require(actor,"order_staff");var r=await Return(actor,p.Id("returnId"));State(r.S("state")=="rejected","Chỉ ghi trả lại hàng bị từ chối.");var note=Note(p);var tracking=p.Text("trackingNo");Valid(tracking.Length is >=3 and <=100,"Cần mã vận đơn trả lại.");await Log(actor,"return_rejected_goods","return",r.G("id"),new{trackingNo=tracking,note},note);await Notify(r.S("customer_id"),"return",$"Hàng được gửi trả: {tracking}. {note}",r.G("id"));return new{returnId=r.G("id"),state="rejected",trackingNo=tracking};
    }
}
