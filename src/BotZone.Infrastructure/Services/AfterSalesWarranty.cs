using System.Text.Json;
using BotZone.Contracts;

namespace BotZone.Infrastructure.Services;

public sealed partial class AfterSalesService
{
    async Task<Dictionary<string,object?>> Entitlement(Actor actor,Guid id)
    {
        Access.Require(actor,"customer","owner","order_staff","warehouse_staff");
        return await sql.One("SELECT e.*,i.variant_id,i.order_id,o.customer_id,v.tracking_mode FROM warranty_entitlements e JOIN order_item_unit_amounts u ON u.id=e.order_item_unit_amount_id JOIN order_items i ON i.id=u.order_item_id JOIN orders o ON o.id=i.order_id JOIN product_variants v ON v.id=i.variant_id WHERE e.id=$1 AND ($2<>'customer' OR o.customer_id=$3) FOR UPDATE OF e",id,actor.Role,actor.Id);
    }
    async Task<object> RequestWarranty(Actor actor,JsonElement p)
    {
        Access.Require(actor,"customer");State(actor.Verified,"Cần xác minh email để yêu cầu Care.");var entitlement=await Entitlement(actor,p.Id("entitlementId"));var today=DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        State(entitlement.S("state")=="active"&&entitlement["starts_on"] is DateOnly start&&entitlement["ends_on"] is DateOnly end&&today>=start&&today<end,"Gói Care chưa đến hạn, đã hết hạn hoặc đã bị hủy.");
        State(await sql.Optional("SELECT id FROM warranty_claims WHERE entitlement_id=$1 AND state NOT IN('closed','rejected','refunded')",entitlement.G("id")) is null,"Thiết bị đã có hồ sơ Care đang xử lý.");
        string damage=p.Text("damageKind"),description=p.Text("description").Trim();Valid(damage is "hardware" or "accidental" or "liquid","Loại hư hỏng không hợp lệ.");Valid(description.Length is >=10 and <=2000,"Mô tả cần 10–2.000 ký tự.");
        var id=Guid.NewGuid();var state=damage=="hardware"?"requested":"rejected";await sql.Exec("INSERT INTO warranty_claims(id,entitlement_id,state,description,damage_kind,inspection_result) VALUES($1,$2,$3,$4,$5,$6)",id,entitlement.G("id"),state,description,damage,damage=="hardware"?null:"BotZone Care chỉ gia hạn lỗi phần cứng; không bảo vệ rơi vỡ/vào nước.");
        await Evidence(actor,p,"warranty_evidence","claim_id",id,"entitlement",entitlement.G("id"));await WarrantyEvent(actor,id,"request",description);
        if(state=="rejected")await Notify(actor.Id,"warranty","Hồ sơ ngoài phạm vi: gói không bảo vệ rơi vỡ/vào nước.",id);return new{claimId=id,state};
    }
    Task WarrantyEvent(Actor actor,Guid claim,string action,string note)=>sql.Exec("INSERT INTO warranty_events(claim_id,actor_id,event,note) VALUES($1,$2,$3,$4)",claim,actor.Id,action,note);
    async Task<object> ProcessWarranty(Actor actor,JsonElement p,string key)
    {
        Access.Require(actor,"owner","order_staff","warehouse_staff");var claim=await sql.One("SELECT * FROM warranty_claims WHERE id=$1 FOR UPDATE",p.Id("claimId"));var entitlement=await Entitlement(actor,claim.G("entitlement_id"));string action=p.Text("action"),note=Note(p);var state=claim.S("state");var id=claim.G("id");
        if(action=="receive")
        {
            Access.Require(actor,"order_staff");State(state=="requested","Hồ sơ không còn chờ tiếp nhận.");await SetCareCustody(entitlement,"service");await sql.Exec("UPDATE warranty_claims SET state='received' WHERE id=$1",id);state="received";
        }
        else if(action=="inspect")
        {
            Access.Require(actor,"warehouse_staff");State(state=="received","Thiết bị chưa nhận hoặc đã kiểm định.");string damage=p.Text("damageKind",claim.S("damage_kind"));Valid(damage is "hardware" or "accidental" or "liquid","Kết quả kiểm định không hợp lệ.");state=damage=="hardware"?"inspected":"rejected";
            await sql.Exec("UPDATE warranty_claims SET state=$2,damage_kind=$3,inspection_result=$4 WHERE id=$1",id,state,damage,note);await Evidence(actor,p,"warranty_evidence","claim_id",id,"entitlement",entitlement.G("id"));
        }
        else if(action=="repair")
        {
            Access.Require(actor,"warehouse_staff");State(state=="inspected"&&claim.S("damage_kind")=="hardware","Cần xác nhận lỗi phần cứng đủ điều kiện.");await sql.Exec("UPDATE warranty_claims SET state='repairing' WHERE id=$1;UPDATE warranty_entitlements SET used=true WHERE id=$2",id,entitlement.G("id"));state="repairing";
        }
        else if(action=="repaired")
        {
            Access.Require(actor,"warehouse_staff");State(state=="repairing","Chưa bắt đầu sửa chữa.");await sql.Exec("UPDATE warranty_claims SET state='repaired' WHERE id=$1",id);state="repaired";
        }
        else if(action=="unrepairable")
        {
            Access.Require(actor,"warehouse_staff");State(state is "inspected" or "repairing","Cần kiểm định/sửa chữa trước khi kết luận.");await sql.Exec("UPDATE warranty_claims SET state='replace',inspection_result=$2 WHERE id=$1",id,note);state="replace";
        }
        else if(action is "replace" or "approve_replace")
        {
            State(state=="replace","Cần kết luận không thể sửa trước khi đổi cùng SKU.");
            if(action=="approve_replace")Access.Require(actor,"owner");
            if(actor.Role=="owner")
            {
                await WarrantyEvent(actor,id,"replace_approved",Values.Json(new{replacementUnitId=p.MaybeId("replacementUnitId"),replacementLotId=p.MaybeId("replacementLotId"),note}));
                return new{claimId=id,state="replace",requiresWarehouseHandoff=true};
            }
            Access.Require(actor,"warehouse_staff");State(await sql.Optional("SELECT e.id FROM warranty_events e JOIN staff_profiles s ON s.user_id=e.actor_id WHERE e.claim_id=$1 AND e.event='replace_approved' AND s.role='owner'",id) is not null,"Cần owner duyệt phương án thay thế.");
            await ReplaceWarranty(actor,claim,entitlement,p,note);state="replaced";
        }
        else if(action=="refund")
        {
            Access.Require(actor,"owner");State(state=="replace","Chỉ hoàn sau kết luận không thể sửa/đổi.");
            var stock=await sql.One("SELECT available FROM variant_stock WHERE variant_id=$1",entitlement.G("variant_id"));State(stock.L("available")==0,"Còn cùng SKU để đổi; chưa đủ điều kiện hoàn thay sửa/đổi.");
            var result=await Credit(actor,entitlement.G("order_id"),[entitlement.G("order_item_unit_amount_id")],note,key,warrantyId:id);
            await sql.Exec("UPDATE warranty_claims SET state='refund' WHERE id=$1;UPDATE warranty_entitlements SET state='refunded',used=true WHERE id=$2",id,entitlement.G("id"));await WarrantyEvent(actor,id,"refund_approved",note);return result;
        }
        else if(action=="reject")
        {
            Access.Require(actor,"owner");State(state is "received" or "inspected" or "replace","Chỉ từ chối khi có kết quả kiểm tra.");await sql.Exec("UPDATE warranty_claims SET state='rejected',inspection_result=$2 WHERE id=$1",id,note);state="rejected";
        }
        else if(action=="close")
        {
            Access.Require(actor,"order_staff");State(state is "repaired" or "replaced" or "rejected" or "refunded","Cần kết quả xử lý trước khi trao trả/đóng.");
            if(state is "repaired" or "rejected")await SetCareCustody(entitlement,"shipped");await sql.Exec("UPDATE warranty_claims SET state='closed' WHERE id=$1",id);state="closed";
        }
        else throw new BusinessException("VALIDATION_ERROR","Bước xử lý Care không hợp lệ.");
        await WarrantyEvent(actor,id,action,note);await Notify(entitlement.S("customer_id"),"warranty",note,id);return new{claimId=id,state};
    }
    async Task SetCareCustody(Dictionary<string,object?> entitlement,string state)
    {
        if(entitlement["inventory_unit_id"] is Guid physical)await sql.Exec("UPDATE inventory_units SET custody_status=$2 WHERE id=$1",physical,state);
        else
        {
            var allocation=await sql.One("SELECT lot_id FROM inventory_allocations WHERE order_item_unit_amount_id=$1 AND state='shipped'",entitlement.G("order_item_unit_amount_id"));
            await sql.Exec("INSERT INTO inventory_quarantine(order_item_unit_amount_id,lot_id,quantity,state,reason) VALUES($1,$2,1,$3,'Care service custody') ON CONFLICT(order_item_unit_amount_id,lot_id) DO UPDATE SET state=excluded.state,reason=excluded.reason",entitlement.G("order_item_unit_amount_id"),allocation.G("lot_id"),state=="service"?"service":"released");
        }
    }
    async Task ReplaceWarranty(Actor actor,Dictionary<string,object?> claim,Dictionary<string,object?> entitlement,JsonElement p,string note)
    {
        var unit=entitlement.G("order_item_unit_amount_id");var old=await sql.One("SELECT a.*,i.variant_id FROM inventory_allocations a JOIN order_items i ON i.id=a.order_item_id WHERE a.order_item_unit_amount_id=$1 AND a.state='shipped' FOR UPDATE OF a",unit);Guid? replacementUnit=null;Dictionary<string,object?> replacement;
        if(entitlement.S("tracking_mode")=="serialized")
        {
            replacementUnit=p.Id("replacementUnitId");replacement=await sql.One("SELECT l.id lot_id,l.unit_cost_vnd,u.id FROM inventory_units u JOIN inventory_lots l ON l.id=u.lot_id WHERE u.id=$1 AND l.variant_id=$2 AND u.condition_status='new' AND u.custody_status='available' FOR UPDATE OF u,l",replacementUnit,entitlement.G("variant_id"));
            State(old["inventory_unit_id"] is Guid,"Thiết bị nguồn thiếu serial.");await sql.Exec("UPDATE inventory_units SET custody_status='quarantine',condition_status='faulty' WHERE id=$1;UPDATE inventory_units SET custody_status='shipped' WHERE id=$2",old["inventory_unit_id"],replacementUnit);
        }
        else
        {
            var requested=p.MaybeId("replacementLotId");replacement=await sql.One("SELECT id lot_id,unit_cost_vnd FROM inventory_lots WHERE variant_id=$1 AND qty_on_hand>qty_reserved AND ($2::uuid IS NULL OR id=$2) ORDER BY received_at,id LIMIT 1 FOR UPDATE",entitlement.G("variant_id"),requested);
            await sql.Exec("UPDATE inventory_lots SET qty_on_hand=qty_on_hand-1 WHERE id=$1",replacement.G("lot_id"));
            await sql.Exec("INSERT INTO inventory_quarantine(order_item_unit_amount_id,lot_id,quantity,state,reason) VALUES($1,$2,1,'quarantine',$3) ON CONFLICT(order_item_unit_amount_id,lot_id) DO UPDATE SET state='quarantine',reason=excluded.reason",unit,old.G("lot_id"),note);
        }
        await sql.Exec("UPDATE inventory_allocations SET state='replaced' WHERE id=$1",old.G("id"));
        await sql.Exec("INSERT INTO inventory_allocations(order_item_id,order_item_unit_amount_id,lot_id,inventory_unit_id,quantity,cost_vnd,state) VALUES($1,$2,$3,$4,1,$5,'shipped')",old.G("order_item_id"),unit,replacement.G("lot_id"),replacementUnit,replacement.L("unit_cost_vnd"));
        await sql.Exec("INSERT INTO warranty_replacements(claim_id,old_unit_id,new_unit_id,old_lot_id,new_lot_id,order_item_unit_amount_id) VALUES($1,$2,$3,$4,$5,$6)",claim.G("id"),old["inventory_unit_id"],replacementUnit,replacementUnit.HasValue?null:old.G("lot_id"),replacementUnit.HasValue?null:replacement.G("lot_id"),unit);
        await sql.Exec("INSERT INTO stock_movements(variant_id,lot_id,unit_id,quantity_delta,kind,source_event_key,actor_user_id,reason) VALUES($1,$2,$3,-1,'replacement',$4,$5,$6)",entitlement.G("variant_id"),replacement.G("lot_id"),replacementUnit,"care-replace:"+claim.G("id"),actor.Id,note);
        await sql.Exec("UPDATE warranty_entitlements SET inventory_unit_id=$2,used=true WHERE id=$1;UPDATE warranty_claims SET state='replaced' WHERE id=$3",entitlement.G("id"),replacementUnit,claim.G("id"));
    }
}
