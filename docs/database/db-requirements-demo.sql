-- Đối chiếu yêu cầu DB khi bảo vệ đồ án BotZone.
-- Chạy trên database botzone. Chỉ đọc, không sửa dữ liệu.
-- Số lượng/tổng có thể tăng khi có đơn được bàn giao sau lượt kiểm tra 18/11/2026.
BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ READ ONLY;

-- 1. Danh sách hóa đơn đã phát hành: đối chiếu màn quản trị Hóa đơn.
SELECT invoice_no, issued_at, lifecycle, subtotal_vnd,
       discount_total_vnd, grand_total_vnd
FROM invoices
WHERE lifecycle = 'issued'
ORDER BY issued_at DESC, id;

SELECT count(*) AS invoice_count, sum(grand_total_vnd) AS total_vnd
FROM invoices WHERE lifecycle = 'issued';
-- Lượt kiểm tra: 11 hóa đơn, 101720000 VND. Draft 99000000 không được cộng.

-- 2. Khuyến mãi được lưu trong DB: G08 đã sử dụng DEMO1M phiên bản 1.
SELECT i.invoice_no, p.code_normalized, v.version_no, r.status,
       r.amount_vnd AS voucher_vnd, i.discount_total_vnd, i.grand_total_vnd
FROM invoices i
JOIN promotion_redemptions r ON r.order_id = i.order_id
JOIN promotions p ON p.id = r.promotion_id
JOIN promotion_versions v ON v.id = r.promotion_version_id
WHERE i.invoice_no = 'G08';
-- Expected: DEMO1M, version 1, consumed, voucher 1000000.

-- 3. Phạm vi mã DEMO1M: sản phẩm P13, không phải mọi sản phẩm.
SELECT p.code_normalized, v.discount_type, v.value, v.all_products,
       product.slug, product.model_code
FROM promotions p
JOIN promotion_versions v ON v.promotion_id = p.id
LEFT JOIN promotion_targets t ON t.promotion_version_id = v.id
LEFT JOIN products product ON product.id = t.product_id
WHERE p.code_normalized = 'DEMO1M';

-- 4. Đọc chi tiết từ DB và kiểm tra tổng đầu hóa đơn bằng tổng các dòng.
SELECT i.invoice_no, ii.line_no, ii.sku_snapshot, ii.name_snapshot,
       ii.quantity, ii.unit_price_vnd, ii.voucher_discount_vnd,
       ii.points_discount_vnd, ii.net_amount_vnd
FROM invoices i
JOIN invoice_items ii ON ii.invoice_id = i.id
WHERE i.invoice_no = 'G08'
ORDER BY ii.line_no;

SELECT i.invoice_no, i.grand_total_vnd,
       coalesce(sum(ii.net_amount_vnd), 0) AS sum_line_vnd,
       i.grand_total_vnd = coalesce(sum(ii.net_amount_vnd), 0) AS totals_match
FROM invoices i
LEFT JOIN invoice_items ii ON ii.invoice_id = i.id
GROUP BY i.id
ORDER BY i.invoice_no NULLS LAST;
-- Lượt kiểm tra: cả 12 hóa đơn, gồm draft, đều totals_match=true.

-- 5. Bộ lọc AND: 10 triệu < tổng hóa đơn < 20 triệu.
SELECT invoice_no, grand_total_vnd
FROM invoices
WHERE lifecycle = 'issued'
  AND grand_total_vnd > 10000000 AND grand_total_vnd < 20000000
ORDER BY invoice_no;
-- Expected hiện tại: G01,G02,G03,G04,G07,G08; 6 hóa đơn, 87300000 VND.

-- 6. Chứng minh toán tử khác và phủ định.
SELECT count(*) AS invoice_count, sum(grand_total_vnd) AS total_vnd
FROM invoices
WHERE lifecycle = 'issued' AND grand_total_vnd <> 18000000;
-- Expected hiện tại: 9 hóa đơn, 65720000 VND.

-- 7. Nhóm OR nằm trong AND, sau đó phủ định NOT.
SELECT invoice_no, grand_total_vnd
FROM invoices
WHERE lifecycle = 'issued'
  AND (grand_total_vnd > 10000000 OR invoice_no = 'BZ-HD-00000003')
  AND NOT (grand_total_vnd = 18000000)
ORDER BY invoice_no;
-- Expected hiện tại: BZ-HD-00000003,G01,G02,G07,G08; 5 hóa đơn, 52830000 VND.

-- 8. Metadata xuất file: chọn export mới nhất kind=list để đối chiếu Excel/PDF.
SELECT id, read_at, row_count, total_vnd, status,
       file_manifest -> 'files' AS files
FROM invoice_exports
WHERE query_json ->> 'kind' = 'list'
ORDER BY read_at DESC
LIMIT 3;

COMMIT;
