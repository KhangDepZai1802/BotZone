using System.Data;
using System.Globalization;
using BotZone.Contracts;
using Microsoft.EntityFrameworkCore;
namespace BotZone.Infrastructure.Services;
public sealed class OperationalReportsService(SqlStore sql)
{
 static DateTime Bound(string value){if(!DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day))throw new BusinessException("VALIDATION_ERROR","Khoảng ngày cần yyyy-MM-dd.",400);return DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue).AddHours(-7),DateTimeKind.Utc);}
 public async Task<object> Read(Actor actor,string kind,string from,string to,string q){Access.Require(actor,kind=="inventory"?["owner","warehouse_staff"]:kind=="promotions"?["owner","order_staff"]:["owner"]);DateTime start=Bound(from),end=Bound(to);Access.Ensure(end>start&&(end-start).TotalDays<=366*30,"VALIDATION_ERROR","Khoảng ngày không hợp lệ hoặc quá 30 năm.",400);string definition,query;
  switch(kind){
   case "inventory":definition="Tồn hiện tại: khả dụng, đang giữ và cách ly theo serial/lô; giá trị tính theo giá vốn nhập. Khoảng ngày không thay đổi ảnh chụp tồn hiện tại.";query="""
    SELECT l.id,v.sku,p.model_code,l.received_at,l.unit_cost_vnd,
    CASE WHEN v.tracking_mode='serialized' THEN (SELECT count(*) FROM inventory_units u WHERE u.lot_id=l.id AND u.custody_status='available' AND u.condition_status='new') ELSE l.qty_on_hand-l.qty_reserved END::int available,
    CASE WHEN v.tracking_mode='serialized' THEN (SELECT count(*) FROM inventory_units u WHERE u.lot_id=l.id AND u.custody_status='reserved') ELSE l.qty_reserved END::int reserved,
    CASE WHEN v.tracking_mode='serialized' THEN (SELECT count(*) FROM inventory_units u WHERE u.lot_id=l.id AND u.custody_status IN('quarantine','service')) ELSE (SELECT COALESCE(sum(quantity),0) FROM inventory_quarantine iq WHERE iq.lot_id=l.id AND iq.state IN('quarantine','service')) END::int quarantine,
    (CASE WHEN v.tracking_mode='serialized' THEN (SELECT count(*) FROM inventory_units u WHERE u.lot_id=l.id AND u.custody_status IN('available','reserved') AND u.condition_status='new') ELSE l.qty_on_hand END*l.unit_cost_vnd)::text stock_value_vnd
    FROM inventory_lots l JOIN product_variants v ON v.id=l.variant_id JOIN products p ON p.id=v.product_id
    """;break;
   case "margin":case "top_products":definition="Doanh thu hàng = phát hành − chứng từ giảm theo ngày phát sinh. Giá vốn = xuất bán − hàng trả đã duyệt về tồn mới; chi phí đổi Care trình bày riêng. Không phải lợi nhuận kế toán sau mọi chi phí.";query="""
    WITH events AS (
      SELECT ii.product_id,ii.quantity::bigint quantity,ii.net_amount_vnd revenue,0::bigint cost,0::bigint care_cost FROM invoice_items ii JOIN invoices i ON i.id=ii.invoice_id WHERE i.lifecycle='issued' AND ii.line_type='product' AND i.issued_at>=$1 AND i.issued_at<$2
      UNION ALL SELECT ii.product_id,-COALESCE(ci.quantity,0)::bigint,-ci.amount_vnd,0,0 FROM credit_note_items ci JOIN credit_notes cn ON cn.id=ci.credit_note_id JOIN invoice_items ii ON ii.id=ci.invoice_item_id WHERE cn.status='issued' AND ii.line_type='product' AND cn.issued_at>=$1 AND cn.issued_at<$2
      UNION ALL SELECT v.product_id,0,0,CASE WHEN sm.kind='ship' THEN -sm.quantity_delta*l.unit_cost_vnd WHEN sm.kind='return' THEN -GREATEST(sm.quantity_delta,0)*l.unit_cost_vnd ELSE 0 END,CASE WHEN sm.kind='replacement' THEN -sm.quantity_delta*l.unit_cost_vnd ELSE 0 END FROM stock_movements sm JOIN inventory_lots l ON l.id=sm.lot_id JOIN product_variants v ON v.id=sm.variant_id WHERE sm.kind IN('ship','return','replacement') AND sm.created_at>=$1 AND sm.created_at<$2
    ) SELECT p.id,p.model_code,t.name,sum(e.quantity)::int net_quantity,sum(e.revenue)::text product_net_vnd,sum(e.cost)::text cost_vnd,(sum(e.revenue)-sum(e.cost))::text gross_margin_vnd,sum(e.care_cost)::text care_replacement_cost_vnd FROM events e JOIN products p ON p.id=e.product_id JOIN product_translations t ON t.product_id=p.id AND t.locale='vi' GROUP BY p.id,p.model_code,t.name
    """;break;
   case "promotions":definition="Lượt voucher của các đơn được tạo trong khoảng ngày; tách reserved/consumed/released. Doanh số hóa đơn xem báo cáo hóa đơn.";query="""
    SELECT p.id,p.code_normalized,count(*) FILTER(WHERE r.status='consumed')::int consumed,count(*) FILTER(WHERE r.status='reserved' AND r.expires_at>now())::int reserved,count(*) FILTER(WHERE r.status='released')::int released,COALESCE(sum(r.amount_vnd) FILTER(WHERE r.status='consumed'),0)::text consumed_discount_vnd FROM promotions p LEFT JOIN promotion_redemptions r ON r.promotion_id=p.id LEFT JOIN orders o ON o.id=r.order_id WHERE o.id IS NULL OR (o.created_at>=$1 AND o.created_at<$2) GROUP BY p.id,p.code_normalized
    """;break;
   case "trade_in":definition="Hồ sơ thu cũ được tạo trong khoảng ngày; credit đã cấp và phí vận đơn thuộc hồ sơ, tách khỏi doanh số hàng mới.";query="""
    SELECT r.id,r.customer_id,r.reception_method,r.state,r.created_at,(SELECT count(*)::int FROM trade_in_items i WHERE i.request_id=r.id) item_count,(SELECT COALESCE(sum(e.delta_vnd),0)::text FROM store_credit_entries e JOIN trade_in_items i ON i.id=e.trade_in_item_id WHERE i.request_id=r.id AND e.kind='trade_in') issued_credit_vnd,(SELECT COALESCE(sum(fee_vnd),0)::text FROM trade_in_shipments s WHERE s.request_id=r.id) freight_vnd FROM trade_in_requests r WHERE r.created_at>=$1 AND r.created_at<$2
    """;break;
   case "warranty":definition="Hồ sơ Care được tạo trong khoảng ngày và chi phí thiết bị thay thế đã ghi nhận. Không cộng doanh thu hàng bán khi đổi Care.";query="""
    SELECT c.id,c.state,c.damage_kind,c.created_at,v.sku,(SELECT COALESCE(sum(-sm.quantity_delta*l.unit_cost_vnd),0)::text FROM stock_movements sm JOIN inventory_lots l ON l.id=sm.lot_id WHERE sm.kind='replacement' AND sm.source_event_key='care-replace:'||c.id::text) replacement_cost_vnd,(SELECT COALESCE(sum(cn.total_vnd),0)::text FROM credit_notes cn WHERE cn.warranty_claim_id=c.id AND cn.status='issued') credit_note_vnd FROM warranty_claims c JOIN warranty_entitlements e ON e.id=c.entitlement_id JOIN order_item_unit_amounts u ON u.id=e.order_item_unit_amount_id JOIN order_items i ON i.id=u.order_item_id JOIN product_variants v ON v.id=i.variant_id WHERE c.created_at>=$1 AND c.created_at<$2
    """;break;
   default:throw new BusinessException("VALIDATION_ERROR","Loại báo cáo không hợp lệ.",400);
  }
  await using var tx=await sql.Db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,sql.Token);await sql.Exec("SET TRANSACTION READ ONLY");string order=kind=="top_products"?"product_net_vnd::numeric DESC,id":"id";var rows=await sql.Rows($"SELECT * FROM ({query}) report WHERE ($3='' OR to_jsonb(report)::text ILIKE '%'||$3||'%') ORDER BY {order} LIMIT 10000",start,end,q.Length<=200?q:q[..200]);if(actor.Role=="warehouse_staff"){foreach(var row in rows){row.Remove("unit_cost_vnd");row.Remove("stock_value_vnd");}definition="Tồn hiện tại theo serial/lô: khả dụng, đang giữ, cách ly. Phạm vi nhân viên kho chỉ hiển thị số lượng.";}var readAt=(await sql.One("SELECT transaction_timestamp() read_at"))["read_at"];await tx.CommitAsync(sql.Token);return new{rows,columns=rows.FirstOrDefault()?.Keys.ToArray()??[],readAt,definition,fromInclusive=from,toExclusive=to};
 }
}
