using System.Globalization;
using System.Text.Json;
using BotZone.Contracts;
using Microsoft.Extensions.Configuration;

namespace BotZone.Infrastructure.Services;

public sealed partial class AfterSalesService
{
    async Task<object> Credit(Actor actor,Guid orderId,Guid[] unitIds,string reason,string key,Guid? returnId=null,Guid? warrantyId=null,Guid? shipmentId=null)
    {
        Access.Require(actor,"owner");Valid(unitIds.Length>0&&unitIds.Distinct().Count()==unitIds.Length,"Chứng từ cần các đơn vị khác nhau.");await sql.One("SELECT id FROM orders WHERE id=$1 FOR UPDATE",orderId);
        var invoice=await sql.One("SELECT * FROM invoices WHERE order_id=$1 AND lifecycle='issued' FOR UPDATE",orderId);
        var units=await sql.Rows("SELECT u.*,ii.id invoice_item_id,ii.line_type FROM order_item_unit_amounts u JOIN order_items i ON i.id=u.order_item_id JOIN invoice_items ii ON ii.order_item_id=i.id WHERE i.order_id=$1 AND u.id=ANY($2) ORDER BY i.line_no,u.unit_ordinal FOR UPDATE OF u",orderId,unitIds);
        State(units.Count==unitIds.Length,"Đơn vị không thuộc hóa đơn này.");State(await sql.Optional("SELECT order_item_unit_amount_id FROM credit_note_item_units WHERE order_item_unit_amount_id=ANY($1) LIMIT 1",unitIds) is null,"Một đơn vị đã có chứng từ giảm; không thể ghi trùng.");
        var id=Guid.NewGuid();var number=Code("CN");var total=units.Sum(u=>u.L("net_amount_vnd"));
        await sql.Exec("INSERT INTO credit_notes(id,credit_note_no,invoice_id,order_id,status,return_request_id,warranty_claim_id,shipment_id,reason,total_vnd,approved_by_user_id,event_key) VALUES($1,$2,$3,$4,'draft',$5,$6,$7,$8,$9,$10,$11)",id,number,invoice.G("id"),orderId,returnId,warrantyId,shipmentId,reason,total,actor.Id,key);
        foreach(var group in units.GroupBy(u=>u.G("invoice_item_id")))
        {
            var item=Guid.NewGuid();await sql.Exec("INSERT INTO credit_note_items(id,credit_note_id,invoice_item_id,quantity,amount_vnd) VALUES($1,$2,$3,$4,$5)",item,id,group.Key,group.Count(),group.Sum(u=>u.L("net_amount_vnd")));
            foreach(var unit in group)await sql.Exec("INSERT INTO credit_note_item_units(credit_note_item_id,order_item_unit_amount_id,amount_vnd) VALUES($1,$2,$3)",item,unit.G("id"),unit.L("net_amount_vnd"));
        }
        await sql.Exec("UPDATE credit_notes SET status='issued',issued_at=now() WHERE id=$1",id);await RestoreCreditedPoints(orderId,units);
        return new{creditNoteId=id,creditNoteNo=number,totalVnd=total.ToString(CultureInfo.InvariantCulture),unitIds};
    }
    async Task RestoreCreditedPoints(Guid orderId,List<Dictionary<string,object?>> units)
    {
        var order=await sql.One("SELECT customer_id,policy_snapshot FROM orders WHERE id=$1",orderId);await sql.One("SELECT user_id FROM loyalty_accounts WHERE user_id=$1 FOR UPDATE",order.S("customer_id"));
        var redeemed=await sql.Optional("SELECT id FROM loyalty_entries WHERE order_id=$1 AND kind='redeem' LIMIT 1",orderId);
        if(redeemed is not null)foreach(var unit in units.Where(u=>u.I("redeemed_points")>0))
        {
            var added=await sql.Exec("INSERT INTO loyalty_entries(user_id,delta,kind,order_id,unit_id,event_key) VALUES($1,$2,'restore',$3,$4,$5) ON CONFLICT(event_key) DO NOTHING",order.S("customer_id"),unit.I("redeemed_points"),orderId,unit.G("id"),"points-restore:"+unit.G("id"));
            if(added>0)await sql.Exec("UPDATE loyalty_accounts SET balance_points=balance_points+$2 WHERE user_id=$1",order.S("customer_id"),unit.I("redeemed_points"));
        }
        var earned=await sql.One("SELECT COALESCE(sum(delta),0)::int points FROM loyalty_entries WHERE order_id=$1 AND kind='earn'",orderId);
        if(earned.I("points")==0)return;
        var totals=await sql.One("SELECT (SELECT sum(net_amount_vnd) FROM order_items WHERE order_id=$1 AND line_type='product') gross,(SELECT COALESCE(sum(cu.amount_vnd),0) FROM credit_note_item_units cu JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id JOIN invoice_items ii ON ii.id=ci.invoice_item_id JOIN credit_notes cn ON cn.id=ci.credit_note_id WHERE cn.order_id=$1 AND cn.status='issued' AND ii.line_type='product') reduced,(SELECT -COALESCE(sum(delta),0)::int FROM loyalty_entries WHERE order_id=$1 AND kind='reverse') reversed",orderId);
        var policy=order.J("policy_snapshot");var multiplier=policy.TryGetProperty("loyaltyMultiplier",out var rate)?decimal.Parse(rate.ToString(),CultureInfo.InvariantCulture):1m;
        var pointsPerVnd=policy.TryGetProperty("pointsPerVnd",out var divisor)?long.Parse(divisor.ToString(),CultureInfo.InvariantCulture):100_000L;
        State(pointsPerVnd>0,"Snapshot chính sách điểm không hợp lệ.");
        var retained=checked((int)decimal.Floor((totals.L("gross")-totals.L("reduced"))*multiplier/pointsPerVnd));var desired=Math.Max(0,earned.I("points")-retained);var reverse=desired-totals.I("reversed");
        if(reverse>0)
        {
            await sql.Exec("INSERT INTO loyalty_entries(user_id,delta,kind,order_id,event_key) VALUES($1,$2,'reverse',$3,$4)",order.S("customer_id"),-reverse,orderId,$"points-reverse:{orderId}:{desired}");
            await sql.Exec("UPDATE loyalty_accounts SET balance_points=balance_points-$2 WHERE user_id=$1",order.S("customer_id"),reverse);
        }
        var returned=units.Where(u=>u.S("line_type")=="product").Sum(u=>u.L("net_amount_vnd"));
        if(returned>0)await sql.Exec("UPDATE loyalty_accounts SET net_spend_vnd=GREATEST(0,net_spend_vnd-$2) WHERE user_id=$1",order.S("customer_id"),returned);
    }
    async Task<object> Refund(Actor actor,JsonElement p,string key)
    {
        Access.Require(actor,"owner");Access.Ensure(config.GetValue<bool>("DemoMode"),"FORBIDDEN","Công cụ hoàn mô phỏng đang tắt.",403);var outcome=p.Text("outcome","success");Valid(outcome is "success" or "failed","Kết quả hoàn không hợp lệ.");
        var cnId=p.MaybeId("creditNoteId");Dictionary<string,object?>? cn=null;Guid orderId;
        if(cnId.HasValue){cn=await sql.One("SELECT * FROM credit_notes WHERE id=$1 AND status='issued'",cnId);orderId=cn.G("order_id");}
        else orderId=p.Id("orderId");
        var order=await sql.One("SELECT * FROM orders WHERE id=$1 FOR UPDATE",orderId);
        if(cn is null)
        {
            State(order.S("order_status") is "cancelled" or "expired" or "reconciliation","Chỉ hoàn trước phát hành cho đơn đã hủy/đối soát.");
            State(await sql.Optional("SELECT id FROM invoices WHERE order_id=$1 AND lifecycle='issued'",orderId) is null,"Hóa đơn đã phát hành cần chứng từ giảm.");
            await commerce.Release(orderId);await commerce.RestorePoints(orderId);
        }
        var funds=await sql.Rows("SELECT a.receipt_id,a.order_item_unit_amount_id,r.tender_kind,(a.amount_vnd-COALESCE((SELECT sum(ra.amount_vnd) FROM refund_allocations ra JOIN refunds rf ON rf.id=ra.refund_id WHERE ra.payment_receipt_id=a.receipt_id AND ra.order_item_unit_amount_id=a.order_item_unit_amount_id AND rf.status<>'failed'),0)) remaining FROM order_tender_allocations a JOIN payment_receipts r ON r.id=a.receipt_id WHERE r.order_id=$1 AND ($2::uuid IS NULL OR EXISTS(SELECT 1 FROM credit_note_item_units cu JOIN credit_note_items ci ON ci.id=cu.credit_note_item_id WHERE ci.credit_note_id=$2 AND cu.order_item_unit_amount_id=a.order_item_unit_amount_id)) ORDER BY a.order_item_unit_amount_id,a.receipt_id",orderId,cnId);
        funds=funds.Where(f=>f.L("remaining")>0).ToList();var total=funds.Sum(f=>f.L("remaining"));
        if(cn is not null)
        {
            var already=await sql.One("SELECT COALESCE(sum(amount_vnd),0) amount FROM refunds WHERE credit_note_id=$1 AND status<>'failed'",cnId);
            State(total+already.L("amount")<=cn.L("total_vnd"),"Số hoàn vượt chứng từ được duyệt.");
        }
        if(total==0)return new{refundId=(Guid?)null,status="no_payment_to_refund",totalVnd="0",creditVnd="0",cashVnd="0"};
        var id=Guid.NewGuid();var status=outcome=="success"?"completed":"failed";
        await sql.Exec("INSERT INTO refunds(id,order_id,credit_note_id,status,amount_vnd,approved_by,completed_at,idempotency_key) VALUES($1,$2,$3,$4,$5,$6,$7,$8)",id,orderId,cnId,status,total,actor.Id,outcome=="success"?DateTime.UtcNow:null,key);
        long cash=0,credit=0;
        foreach(var fund in funds)
        {
            await sql.Exec("INSERT INTO refund_allocations(refund_id,payment_receipt_id,order_item_unit_amount_id,amount_vnd) VALUES($1,$2,$3,$4)",id,fund.G("receipt_id"),fund.G("order_item_unit_amount_id"),fund.L("remaining"));
            if(fund.S("tender_kind")=="store_credit")credit+=fund.L("remaining");else cash+=fund.L("remaining");
        }
        if(outcome=="success")
        {
            if(credit>0)
            {
                await sql.One("SELECT user_id FROM store_credit_accounts WHERE user_id=$1 FOR UPDATE",order.S("customer_id"));
                await sql.Exec("INSERT INTO store_credit_entries(user_id,delta_vnd,kind,order_id,refund_id,event_key) VALUES($1,$2,'refund',$3,$4,$5)",order.S("customer_id"),credit,orderId,id,"refund-credit:"+id);
                await sql.Exec("UPDATE store_credit_accounts SET balance_vnd=balance_vnd+$2 WHERE user_id=$1",order.S("customer_id"),credit);
            }
            if(cn?["return_request_id"] is Guid rid)await sql.Exec("UPDATE return_requests SET state='closed' WHERE id=$1",rid);
            if(cn?["warranty_claim_id"] is Guid wid)await sql.Exec("UPDATE warranty_claims SET state='refunded' WHERE id=$1",wid);
            await Notify(order.S("customer_id"),"refund","Đã hoàn đúng nguồn thanh toán mô phỏng; điểm không được đổi thành tiền.",orderId);
        }
        return new{refundId=id,status,totalVnd=total.ToString(CultureInfo.InvariantCulture),cashVnd=cash.ToString(CultureInfo.InvariantCulture),creditVnd=credit.ToString(CultureInfo.InvariantCulture)};
    }
    async Task<object> CloseFailedShipment(Actor actor,JsonElement p,string key)
    {
        Access.Require(actor,"owner");var shipment=await sql.One("SELECT s.* FROM shipments s JOIN orders o ON o.id=s.order_id WHERE s.id=$1 AND o.delivered_at IS NULL FOR UPDATE OF s,o",p.Id("shipmentId"));
        State(shipment.S("status")=="returned","Hàng giao thất bại phải đã thu hồi trước khi kết thúc.");
        var inspection=await sql.Optional("SELECT after_data FROM audit_events WHERE action='inspect_failed_shipment' AND entity_id=$1 ORDER BY occurred_at DESC LIMIT 1",shipment.G("id").ToString());
        State(inspection is not null,"Kho cần xác nhận kiểm tra kiện thu hồi.");
        if(p.Bool("restock"))
        {
            State(inspection!.J("after_data").Text("condition","")=="new","Chỉ kiện đủ chuẩn hàng mới mới được nhập lại tồn bán.");
            foreach(var allocation in await sql.Rows("SELECT a.*,i.variant_id FROM inventory_allocations a JOIN order_items i ON i.id=a.order_item_id WHERE i.order_id=$1 AND a.state='returned' FOR UPDATE OF a",shipment.G("order_id")))
            {
                if(allocation["inventory_unit_id"] is Guid physical)await sql.Exec("UPDATE inventory_units SET custody_status='available' WHERE id=$1 AND condition_status='new'",physical);
                else await sql.Exec("UPDATE inventory_lots SET qty_on_hand=qty_on_hand+1 WHERE id=$1;UPDATE inventory_quarantine SET state='released' WHERE order_item_unit_amount_id=$2 AND lot_id=$1",allocation.G("lot_id"),allocation.G("order_item_unit_amount_id"));
                await sql.Exec("INSERT INTO stock_movements(variant_id,lot_id,unit_id,quantity_delta,kind,source_event_key,actor_user_id,reason) VALUES($1,$2,$3,1,'return',$4,$5,$6)",allocation.G("variant_id"),allocation.G("lot_id"),allocation["inventory_unit_id"],"failed-return:"+allocation.G("id"),actor.Id,Note(p,"reason"));
            }
        }
        var orderId=shipment.G("order_id");var units=(await sql.Rows("SELECT u.id FROM order_item_unit_amounts u JOIN order_items i ON i.id=u.order_item_id WHERE i.order_id=$1 AND NOT EXISTS(SELECT 1 FROM credit_note_item_units cu WHERE cu.order_item_unit_amount_id=u.id)",orderId)).Select(u=>u.G("id")).ToArray();
        var result=await Credit(actor,orderId,units,Note(p,"reason"),key,shipmentId:shipment.G("id"));await sql.Exec("UPDATE warranty_entitlements SET state='cancelled' WHERE order_item_unit_amount_id=ANY($1);UPDATE shipments SET status='closed' WHERE id=$2",units,shipment.G("id"));return result;
    }
    async Task<object> InspectFailedShipment(Actor actor,JsonElement p)
    {
        Access.Require(actor,"warehouse_staff");var shipment=await sql.One("SELECT s.* FROM shipments s JOIN orders o ON o.id=s.order_id WHERE s.id=$1 AND o.delivered_at IS NULL FOR UPDATE OF s,o",p.Id("shipmentId"));State(shipment.S("status")=="returned","Vận đơn phải ghi thu hồi trước khi kiểm tra.");var note=Note(p);var condition=p.Text("condition");Valid(condition is "new" or "opened" or "used" or "faulty","Tình trạng không hợp lệ.");
        var allocations=await sql.Rows("SELECT a.*,i.variant_id FROM inventory_allocations a JOIN order_items i ON i.id=a.order_item_id WHERE i.order_id=$1 AND a.state='shipped' FOR UPDATE OF a",shipment.G("order_id"));
        foreach(var allocation in allocations)
        {
            if(allocation["inventory_unit_id"] is Guid physical)await sql.Exec("UPDATE inventory_units SET custody_status='quarantine',condition_status=$2 WHERE id=$1",physical,condition);
            else await sql.Exec("INSERT INTO inventory_quarantine(order_item_unit_amount_id,lot_id,quantity,state,reason) VALUES($1,$2,1,'quarantine',$3) ON CONFLICT(order_item_unit_amount_id,lot_id) DO NOTHING",allocation.G("order_item_unit_amount_id"),allocation.G("lot_id"),note);
            await sql.Exec("UPDATE inventory_allocations SET state='returned' WHERE id=$1",allocation.G("id"));
        }
        await Log(actor,"inspect_failed_shipment","shipment",shipment.G("id"),new{condition,units=allocations.Count},note);return new{shipmentId=shipment.G("id"),inspected=true};
    }
}
