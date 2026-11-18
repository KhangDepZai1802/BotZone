using System.Text.Json;
using System.Text.RegularExpressions;
using BotZone.Contracts;

namespace BotZone.Infrastructure.Services;

/// <summary>Caller owns the EF transaction, actor settings, command idempotency and audit.</summary>
public sealed class InventoryService(SqlStore sql)
{
    public Task<object?> Handle(Actor actor,string command,JsonElement input,string key)=>command switch
    {
        "create_purchase"=>CreatePurchase(actor,input),
        "approve_purchase"=>ApprovePurchase(actor,input),
        "receive_goods"=>ReceiveGoods(actor,input,key),
        "request_stock_adjustment"=>RequestAdjustment(actor,input),
        "approve_stock_adjustment"=>ApproveAdjustment(actor,input),
        _=>Task.FromResult<object?>(null)
    };
    private static int Quantity(JsonElement input,string field="quantity",bool signed=false)
    {
        Access.Ensure(input.TryGetProperty(field,out var value)&&value.ValueKind==JsonValueKind.Number&&value.TryGetInt32(out var n)&&n!=0&&Math.Abs((long)n)<=10000&&(signed||n>0),"VALIDATION_ERROR",$"{field} phải là số nguyên {(signed?"khác 0":"dương")} không quá 10.000.",400);
        return value.GetInt32();
    }
    private static string Reason(JsonElement input)
    {
        var reason=input.Text("reason").Trim();Access.Ensure(reason.Length is >=5 and <=2000,"VALIDATION_ERROR","Lý do cần từ 5 đến 2.000 ký tự.",400);return reason;
    }
    private static JsonElement[] Lines(JsonElement input)
    {
        var lines=input.Array("items");Access.Ensure(lines.Length is >=1 and <=100,"VALIDATION_ERROR","Phiếu cần 1–100 dòng hàng.",400);Access.Ensure(lines.All(x=>x.ValueKind==JsonValueKind.Object),"VALIDATION_ERROR","Dòng hàng phải là đối tượng.",400);return lines;
    }
    private async Task<object?> CreatePurchase(Actor actor,JsonElement input)
    {
        Access.Require(actor,"owner","warehouse_staff");var supplier=await sql.One("SELECT id,active FROM suppliers WHERE id=$1 FOR SHARE",input.Id("supplierId"));Access.Ensure(supplier.B("active"),"SUPPLIER_INACTIVE","Nhà cung cấp đang ngừng hoạt động.");
        var lines=Lines(input);var distinct=new HashSet<Guid>();var prepared=new List<(Guid Variant,int Quantity,long Cost)>();
        foreach(var line in lines)
        {
            var variant=line.Id("variantId");Access.Ensure(distinct.Add(variant),"DUPLICATE_SKU","Gộp các dòng cùng SKU trước khi tạo phiếu.",400);var quantity=Quantity(line);line.Field("unitCostVnd");var cost=line.Money("unitCostVnd");
            var sku=await sql.One("SELECT id,active FROM product_variants WHERE id=$1 FOR SHARE",variant);Access.Ensure(sku.B("active"),"SKU_INACTIVE","SKU đã ngừng kinh doanh.");prepared.Add((variant,quantity,cost));
        }
        var id=Guid.NewGuid();var code=$"PO-{DateTime.UtcNow:yyMMddHHmmss}-{id.ToString("N")[..8].ToUpperInvariant()}";
        await sql.Exec("INSERT INTO purchase_orders(id,supplier_id,code,status,created_by) VALUES($1,$2,$3,'draft',$4)",id,supplier.G("id"),code,actor.Id);
        foreach(var line in prepared)await sql.Exec("INSERT INTO purchase_order_items(purchase_order_id,variant_id,quantity,unit_cost_vnd) VALUES($1,$2,$3,$4)",id,line.Variant,line.Quantity,line.Cost);
        return new{purchaseId=id,code,status="draft",lineCount=prepared.Count};
    }
    private async Task<object?> ApprovePurchase(Actor actor,JsonElement input)
    {
        Access.Require(actor,"owner");var id=input.Id("purchaseId");var purchase=await sql.One("SELECT p.*,s.active supplier_active FROM purchase_orders p JOIN suppliers s ON s.id=p.supplier_id WHERE p.id=$1 FOR UPDATE OF p",id);
        Access.Ensure(purchase.S("status")=="draft","PURCHASE_STATE","Chỉ phiếu nháp mới được phê duyệt.");Access.Ensure(purchase.B("supplier_active"),"SUPPLIER_INACTIVE","Nhà cung cấp đã ngừng hoạt động.");
        var count=await sql.One("SELECT count(*)::int n FROM purchase_order_items WHERE purchase_order_id=$1",id);Access.Ensure(count.I("n")>0,"EMPTY_PURCHASE","Phiếu nhập chưa có dòng hàng.");
        await sql.Exec("UPDATE purchase_orders SET status='approved',approved_by=$2 WHERE id=$1",id,actor.Id);return new{purchaseId=id,status="approved"};
    }
    private async Task<object?> ReceiveGoods(Actor actor,JsonElement input,string key)
    {
        Access.Require(actor,"warehouse_staff");var purchaseId=input.Id("purchaseId");var purchase=await sql.One("SELECT * FROM purchase_orders WHERE id=$1 FOR UPDATE",purchaseId);
        Access.Ensure(purchase.S("status") is "approved" or "part_received"&&purchase.S("approved_by").Length>0,"PURCHASE_STATE","Chỉ nhận hàng theo phiếu đã được owner phê duyệt và chưa nhận đủ.");
        var lines=Lines(input);var seen=new HashSet<Guid>();var identifiers=new HashSet<string>(StringComparer.Ordinal);var prepared=new List<ReceiptLine>();
        foreach(var line in lines)
        {
            var lineId=line.Id("purchaseOrderItemId");Access.Ensure(seen.Add(lineId),"DUPLICATE_RECEIPT_LINE","Mỗi dòng phiếu nhập chỉ xuất hiện một lần trong lần nhận.",400);var quantity=Quantity(line);
            var source=await sql.One("SELECT i.*,v.tracking_mode,COALESCE((SELECT sum(g.received_qty) FROM goods_receipt_items g WHERE g.purchase_order_item_id=i.id),0)::int received_qty FROM purchase_order_items i JOIN product_variants v ON v.id=i.variant_id WHERE i.id=$1 AND i.purchase_order_id=$2 FOR UPDATE OF i",lineId,purchaseId);
            Access.Ensure(quantity<=source.I("quantity")-source.I("received_qty"),"RECEIPT_EXCEEDS_PURCHASE","Số nhận vượt phần còn lại trên phiếu đã duyệt.");
            var units=new List<(string Kind,string Identifier)>();var serials=line.TryGetProperty("serials",out var serialArray)?serialArray:JsonSerializer.SerializeToElement(Array.Empty<string>());
            Access.Ensure(serials.ValueKind==JsonValueKind.Array,"VALIDATION_ERROR","Serial phải là danh sách.",400);
            foreach(var value in serials.EnumerateArray())
            {
                Access.Ensure(value.ValueKind==JsonValueKind.String,"VALIDATION_ERROR","Serial/IMEI phải là chuỗi.",400);var identifier=Normalize(value.GetString()!);Access.Ensure(identifiers.Add(identifier.Kind+":"+identifier.Value),"DUPLICATE_IDENTIFIER","Serial/IMEI bị trùng trong phiếu nhận.",400);
                var existing=await sql.Optional("SELECT id FROM inventory_identifiers WHERE kind=$1 AND normalized_value=$2",identifier.Kind,identifier.Value);Access.Ensure(existing is null,"DUPLICATE_IDENTIFIER","Serial/IMEI đã tồn tại; kiểm tra nguồn hàng trước khi nhận.");units.Add((identifier.Kind,identifier.Value));
            }
            Access.Ensure(source.S("tracking_mode")=="serialized"?units.Count==quantity:units.Count==0,"SERIAL_QUANTITY_MISMATCH",source.S("tracking_mode")=="serialized"?"Mỗi thiết bị cần đúng một serial hoặc IMEI; số mã phải bằng số nhận.":"Phụ kiện theo số lượng không nhận danh sách serial.",400);
            prepared.Add(new(lineId,source.G("variant_id"),quantity,source.L("unit_cost_vnd"),units));
        }
        var warehouse=await sql.One("SELECT id FROM warehouses WHERE code='MAIN'");var receiptId=Guid.NewGuid();var number=$"GR-{DateTime.UtcNow:yyMMddHHmmss}-{receiptId.ToString("N")[..8].ToUpperInvariant()}";
        await sql.Exec("INSERT INTO goods_receipts(id,purchase_order_id,receipt_no,received_by,event_key) VALUES($1,$2,$3,$4,$5)",receiptId,purchaseId,number,actor.Id,"receive:"+purchaseId+":"+key);
        var lots=new List<Guid>();
        foreach(var line in prepared)
        {
            var receiptItem=Guid.NewGuid();var lot=Guid.NewGuid();lots.Add(lot);
            await sql.Exec("INSERT INTO goods_receipt_items(id,receipt_id,purchase_order_id,purchase_order_item_id,received_qty,unit_cost_vnd) VALUES($1,$2,$3,$4,$5,$6)",receiptItem,receiptId,purchaseId,line.PurchaseItemId,line.Quantity,line.Cost);
            await sql.Exec("INSERT INTO inventory_lots(id,warehouse_id,variant_id,receipt_item_id,unit_cost_vnd,qty_on_hand) VALUES($1,$2,$3,$4,$5,$6)",lot,warehouse.G("id"),line.VariantId,receiptItem,line.Cost,line.Quantity);
            if(line.Units.Count==0)await Movement(actor,line.VariantId,lot,null,line.Quantity,"receive","receive:"+receiptItem,"Nhận hàng theo "+number);
            foreach(var identifier in line.Units)
            {
                var unit=Guid.NewGuid();await sql.Exec("INSERT INTO inventory_units(id,lot_id) VALUES($1,$2)",unit,lot);await sql.Exec("INSERT INTO inventory_identifiers(inventory_unit_id,kind,normalized_value) VALUES($1,$2,$3)",unit,identifier.Kind,identifier.Identifier);
                await Movement(actor,line.VariantId,lot,unit,1,"receive","receive:"+receiptItem+":"+unit,"Nhận thiết bị theo "+number);
            }
        }
        var remaining=await sql.One("SELECT count(*)::int n FROM purchase_order_items i WHERE i.purchase_order_id=$1 AND i.quantity>COALESCE((SELECT sum(received_qty) FROM goods_receipt_items r WHERE r.purchase_order_item_id=i.id),0)",purchaseId);var state=remaining.I("n")==0?"received":"part_received";
        await sql.Exec("UPDATE purchase_orders SET status=$2 WHERE id=$1",purchaseId,state);return new{receiptId,receiptNo=number,purchaseId,status=state,lotIds=lots};
    }
    private async Task<object?> RequestAdjustment(Actor actor,JsonElement input)
    {
        Access.Require(actor,"warehouse_staff");var lot=input.Id("lotId");var delta=Quantity(input,"quantityDelta",true);var reason=Reason(input);await sql.One("SELECT id FROM inventory_lots WHERE id=$1 FOR UPDATE",lot);
        var id=Guid.NewGuid();await sql.Exec("INSERT INTO stock_adjustments(id,lot_id,quantity_delta,reason,requested_by,status) VALUES($1,$2,$3,$4,$5,'requested')",id,lot,delta,reason,actor.Id);return new{adjustmentId=id,status="requested"};
    }
    private async Task<object?> ApproveAdjustment(Actor actor,JsonElement input)
    {
        Access.Require(actor,"owner");var id=input.Id("adjustmentId");var reason=Reason(input);var row=await sql.One("SELECT a.*,l.variant_id,l.qty_on_hand,l.qty_reserved,v.tracking_mode FROM stock_adjustments a JOIN inventory_lots l ON l.id=a.lot_id JOIN product_variants v ON v.id=l.variant_id WHERE a.id=$1 FOR UPDATE OF a,l",id);
        Access.Ensure(row.S("status")=="requested","ADJUSTMENT_STATE","Đề nghị đã có quyết định.");Access.Ensure(row.S("requested_by")!=actor.Id,"SEPARATION_OF_DUTIES","Người đề nghị không tự duyệt đề nghị của mình.",403);
        Access.Ensure(input.TryGetProperty("approve",out var decision)&&decision.ValueKind is JsonValueKind.True or JsonValueKind.False,"VALIDATION_ERROR","Chọn duyệt hoặc từ chối.",400);
        if(!decision.GetBoolean()){await sql.Exec("UPDATE stock_adjustments SET status='rejected',approved_by=$2 WHERE id=$1",id,actor.Id);return new{adjustmentId=id,status="rejected"};}
        var delta=row.I("quantity_delta");var lot=row.G("lot_id");var variant=row.G("variant_id");Access.Ensure((long)row.I("qty_on_hand")+delta>=row.I("qty_reserved"),"STOCK_RESERVED","Điều chỉnh không được làm thiếu phần đang giữ cho đơn.");
        Access.Ensure((long)row.I("qty_on_hand")+delta<=int.MaxValue,"STOCK_RANGE","Số lượng sau điều chỉnh vượt miền dữ liệu.",400);
        if(row.S("tracking_mode")=="serialized")
        {
            var ids=input.Array("inventoryUnitIds").Select(value=>value.ValueKind==JsonValueKind.String&&Guid.TryParse(value.GetString(),out var unit)?unit:throw new BusinessException("VALIDATION_ERROR","Mã thiết bị không hợp lệ.",400)).ToArray();
            Access.Ensure(ids.Length==Math.Abs(delta)&&ids.Distinct().Count()==ids.Length,"SERIAL_QUANTITY_MISMATCH","Chọn đúng số thiết bị khác nhau bằng trị tuyệt đối của chênh lệch.",400);
            foreach(var unitId in ids)
            {
                var unit=await sql.One("SELECT * FROM inventory_units WHERE id=$1 AND lot_id=$2 FOR UPDATE",unitId,lot);Access.Ensure(unit.S("condition_status")=="new","UNIT_CONDITION","Điều chỉnh không chuyển thiết bị cũ/lỗi thành hàng mới.");
                var active=await sql.Optional("SELECT id FROM inventory_allocations WHERE inventory_unit_id=$1 AND state IN ('reserved','shipped') LIMIT 1",unitId);Access.Ensure(active is null,"UNIT_ALLOCATED","Thiết bị đang gắn với đơn; xử lý qua luồng giao/trả tương ứng.");
                if(delta<0)Access.Ensure(unit.S("custody_status")=="available","UNIT_STATE","Chỉ tách khỏi tồn thiết bị mới đang khả dụng.");
                else
                {
                    Access.Ensure(unit.S("custody_status")=="quarantine","UNIT_STATE","Chỉ khôi phục thiết bị đã cách ly do kiểm kê.");var last=await sql.Optional("SELECT kind,quantity_delta FROM stock_movements WHERE unit_id=$1 AND quantity_delta<>0 ORDER BY created_at DESC,id DESC LIMIT 1",unitId);
                    Access.Ensure(last is not null&&last.S("kind")=="adjust"&&last.I("quantity_delta")==-1,"UNIT_PROVENANCE","Chỉ khôi phục serial đã trừ bởi kiểm kê; hàng mới phải có phiếu nhận.");
                }
                await sql.Exec("UPDATE inventory_units SET custody_status=$2 WHERE id=$1",unitId,delta<0?"quarantine":"available");await Movement(actor,variant,lot,unitId,delta<0?-1:1,"adjust","adjust:"+id+":"+unitId,row.S("reason")+" | Duyệt: "+reason);
            }
        }
        else await Movement(actor,variant,lot,null,delta,"adjust","adjust:"+id,row.S("reason")+" | Duyệt: "+reason);
        await sql.Exec("UPDATE inventory_lots SET qty_on_hand=qty_on_hand+$2 WHERE id=$1",lot,delta);await sql.Exec("UPDATE stock_adjustments SET status='approved',approved_by=$2 WHERE id=$1",id,actor.Id);return new{adjustmentId=id,status="approved",quantityDelta=delta};
    }
    private Task<int> Movement(Actor actor,Guid variant,Guid lot,Guid? unit,int delta,string kind,string key,string reason)=>sql.Exec("INSERT INTO stock_movements(variant_id,lot_id,unit_id,quantity_delta,kind,source_event_key,actor_user_id,reason) VALUES($1,$2,$3,$4,$5,$6,$7,$8)",variant,lot,unit,delta,kind,key,actor.Id,reason);
    private static (string Kind,string Value) Normalize(string raw)
    {
        var value=raw.Trim().ToUpperInvariant();var imei=value.StartsWith("IMEI:",StringComparison.Ordinal);if(imei)value=value[5..].Trim();else if(value.StartsWith("SN:",StringComparison.Ordinal))value=value[3..].Trim();else if(Regex.IsMatch(value,"^[0-9]{15}$"))imei=true;
        Access.Ensure(imei?Regex.IsMatch(value,"^[0-9]{15}$"):Regex.IsMatch(value,"^[A-Z0-9][A-Z0-9_-]{4,79}$"),"INVALID_IDENTIFIER",imei?"IMEI cần đúng 15 chữ số.":"Serial cần 5–80 ký tự chữ/số, gạch ngang hoặc gạch dưới.",400);return(imei?"imei":"serial",value);
    }
    private sealed record ReceiptLine(Guid PurchaseItemId,Guid VariantId,int Quantity,long Cost,List<(string Kind,string Identifier)> Units);
}
