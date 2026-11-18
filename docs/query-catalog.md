# Query catalog — hóa đơn và báo cáo C#

Nguồn truy vấn đang chạy là [InvoiceQueryService.cs](../src/BotZone.Infrastructure/Invoices/InvoiceQueryService.cs), [InvoicePredicateBuilder.cs](../src/BotZone.Infrastructure/Invoices/InvoicePredicateBuilder.cs), [InvoiceExportService.cs](../src/BotZone.Infrastructure/Invoices/InvoiceExportService.cs). [query-plans.json](../tests/BotZone.IntegrationTests/query-plans.json) lưu **nguyên SQL, tên/kiểu/giá trị parameter và plan JSON thực tế** của bốn query; không phải SQL minh họa được gắn số đo giả.

## Truy vết DB-01…DB-07

| Tiêu chí | Bảng/cột chính | API / service | Native / test |
|---|---|---|---|
| DB-01 | promotions, promotion_versions, promotion_targets, promotion_redemptions; order_items và order_item_unit_amounts frozen discounts | POST /api/checkout/quote; POST /api/commands place_order; CommerceService | Cart storefront, promotion form; ConcurrencyChecks cuối quota, guard used target |
| DB-02 | invoices, invoice_items; receipt/credit/refund aggregates | POST /api/admin/invoices/search → ListInvoicesAsync; GET /api/invoices/{id} → GetInvoiceDetailAsync | InvoicesView/InvoiceDetailForm; golden/detail tests |
| DB-03 | invoice_facts CTE, typed parameters, scope + AST | InvoicePredicateBuilder.Build dùng chung list/search/report/export | FilterDialog; TS01…TS10 parity |
| DB-04 | SQL projections sang InvoiceListItemDto | EF Database.SqlQueryRaw<InvoiceListItemDto>().ToListAsync() → List<InvoiceListItemDto> | InvoicesView `List<InvoiceListItemDto> items=data.Items; binding.DataSource=items` |
| DB-05A | invoices.issued_at; credit_notes.issued_at; receipt.received_at; refund.completed_at | POST /api/admin/reports/invoices groupBy=period, granularity=month/quarter/year | ReportsView, golden year/event/zero tests |
| DB-05B | invoices.responsible_staff_id, staff_snapshot | cùng endpoint groupBy=staff; order_staff scope bản thân | ReportsView; staff golden/scope tests |
| DB-05C | invoice_items.line_type='product', product_id/variant_id, net_amount_vnd | groupBy=product, productLevel=model/sku | ReportsView; product golden tests |
| DB-06 | parameterized fields/EXISTS product, NULL node | Predicate compiler; parenthesized AST + access scope ngoài AST | FilterDialog; exact46.8m/injection/precedence tests |
| DB-07 | invoice_exports.query_json/read_at/file_manifest; private files | POST /api/invoices/exports; GET /api/invoices/exports/{id}/{format} | InvoicesView SaveFileDialog; 75 export/ownership/checksum/writer tests |

## Những quyết định tránh sai tổng và vượt quyền

`invoice_facts` giữ đúng một hàng mỗi invoice. Tổng credit note, receipt, refund và số lượng sản phẩm dùng aggregate độc lập/LATERAL trước khi join vào header. Không join các collection lines × attempts × receipts rồi SUM header; không dùng SUM(DISTINCT amount) để chữa fan-out, vì G03 và G04 cùng 18 triệu đều phải được cộng.

Điều kiện SQL cuối có dạng `access_scope AND lifecycle_scope AND (user_filter)`. Owner và order staff đọc danh sách để xử lý; report order staff bắt buộc `responsible_staff_id=actor.Id`; customer chỉ detail issued thuộc mình. Content và warehouse không có financial list/export. Giá trị tìm kiếm luôn là NpgsqlParameter; tên trường, operator, groupBy, granularity, sort lấy từ whitelist. Sort thêm invoice UUID để ổn định khi thời điểm/tiền giống nhau.

Product contains dùng EXISTS, not_contains dùng NOT EXISTS trên invoice_items. Do đó NOT AP2 loại cả hóa đơn có AP2 lẫn P13. NULL có node riêng; `field = null` bị từ chối. AST tối đa bốn cấp, ba mươi leaves; pageSize tối đa100. VND lưu bigint, DTO số tiền là chuỗi; tổng C# dùng BigInteger. Export từ chối tổng vượt miền BIGINT của metadata, không làm tròn.

Ngày `YYYY-MM-DD` được hiểu từ 00:00 Việt Nam. Khoảng là `[fromInclusive,toExclusive)`. Timestamp phải có timezone; giá trị .NET có bảy chữ số phần giây được nhận và cắt xuống microsecond khi bind PostgreSQL, metadata trả cùng bound đã chuẩn hóa. Các kỳ dùng `date_trunc(... AT TIME ZONE 'Asia/Ho_Chi_Minh')`, `generate_series` và LEFT JOIN để có kỳ trống0; nhãn kỳ có cả năm.

List/count/tổng/detail/report được đọc trong một transaction RepeatableRead READ ONLY. Export materialize một dataset trong transaction đó, sau đó cả XLSX/PDF chỉ nhận dataset đã có; không query lại khi render. Manifest lưu invoice IDs, dataset SHA-256 và SHA-256 từng file. Download kiểm requester, current role/active staff, ownership hoặc report staff scope, expiry, size và checksum. Kiểm DB-T14 dùng writer thật được ghi trong [export-snapshot-result.json](../tests/BotZone.IntegrationTests/export-snapshot-result.json).

## Định nghĩa năm chỉ tiêu

| Metric | Ngày sự kiện | Giá trị header | Khi nhóm product |
|---|---|---|---|
| issued | invoices.issued_at | invoices.grand_total_vnd | invoice_items.net_amount_vnd, chỉ product |
| adjustments | credit_notes.issued_at, status issued | credit_notes.total_vnd | credit_note_items.amount_vnd liên kết dòng gốc product |
| net | ngày từng invoice/CN | issued dương, adjustments âm, UNION ALL events | cùng quy tắc trên từng dòng |
| receipts | payment_receipts.received_at | amount_vnd từng receipt | order_tender_allocations → frozen unit → dòng invoice gốc |
| refunds | refunds.completed_at, status completed | refund_allocations.amount_vnd | allocation → frozen unit → dòng invoice gốc |

Report chọn cửa sổ trên **ngày event**, không ép invoice.issued_at vào cửa sổ trước khi tính adjustments/receipt/refund. Vì vậy CN01 tháng5 được tính dù G04 phát hành tháng4. Receipt/refund tách `cash_demo` và `store_credit` theo nguồn gốc; points và voucher không phải nguồn tiền hoàn. `invoiceCount` của report là số invoice phân biệt có event trong kỳ; report export `rowCount` là số invoice nguồn phân biệt, không phải số hàng kỳ×dimension.

## SQL đối chiếu golden chỉ đọc

Các câu dưới dùng trên clone phục vụ đối chiếu của owner; không thay thế permission service của API. G01…G08 được cô lập để không cộng thêm75 PAG hoặc đơn browser. Các tổng expected đã được service kiểm tự động trong runner; SQL dưới là dạng ngắn cho người đọc tự chạy.

```sql
BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ READ ONLY;

-- Header: 8 / 97.130.000. Draft D01 không được tính.
SELECT count(*) AS invoice_count, sum(grand_total_vnd) AS issued_vnd
FROM invoices
WHERE lifecycle='issued'
  AND invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08');

-- Năm Việt Nam: 28.000.000 / 44.000.000 / 25.130.000.
SELECT date_trunc('year', issued_at AT TIME ZONE 'Asia/Ho_Chi_Minh') AS period,
       sum(grand_total_vnd) AS issued_vnd
FROM invoices
WHERE lifecycle='issued'
  AND invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08')
GROUP BY 1 ORDER BY 1;

-- Trách nhiệm được snapshot, không lấy current handler của order.
SELECT responsible_staff_id, sum(grand_total_vnd) AS issued_vnd
FROM invoices
WHERE lifecycle='issued'
  AND invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08')
GROUP BY responsible_staff_id ORDER BY responsible_staff_id;

-- Theo model: dòng product; Care và shipping chỉ nằm ở tổng header.
SELECT ii.product_id, sum(ii.net_amount_vnd) AS product_vnd
FROM invoices i JOIN invoice_items ii ON ii.invoice_id=i.id
WHERE i.lifecycle='issued' AND ii.line_type='product'
  AND i.invoice_no IN ('G01','G02','G03','G04','G05','G06','G07','G08')
GROUP BY ii.product_id ORDER BY ii.product_id;

-- Điều chỉnh là sự kiện riêng: CN01 tháng5/2025; CN02 tháng11/2026.
SELECT c.credit_note_no,
       c.issued_at AT TIME ZONE 'Asia/Ho_Chi_Minh' AS issued_vietnam,
       c.total_vnd
FROM credit_notes c JOIN invoices i ON i.id=c.invoice_id
WHERE c.status='issued' AND c.credit_note_no IN ('CN01','CN02')
ORDER BY c.issued_at,c.id;

COMMIT;
```

## Kế hoạch thực thi thực tế

Lượt đo 13/11/2026 **08:44:16 UTC**, clone có89 invoice/87 issued. [QueryPlanEvidence.cs](../tests/BotZone.IntegrationTests/QueryPlanEvidence.cs) dùng interceptor thu SQL+parameter từ service thật, rồi chạy `EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON)` với đúng parameter. Bốn query dùng owner, cửa sổ2024-01-01 đến2027-01-01; báo cáo net/tháng. Toàn bộ cây plan và SQL ở [query-plans.json](../tests/BotZone.IntegrationTests/query-plans.json).

| Query | Hàng trả | Planning ms | Execution ms | Shared hit blocks | Shared read blocks |
|---|---:|---:|---:|---:|---:|
| invoice-list, page20 | 20 | 0,571 | 2,609 | 876 | 0 |
| report-period | 36 | 2,731 | 2,976 | 877 | 0 |
| report-staff | 108 | 1,340 | 3,040 | 877 | 0 |
| report-product | 108 | 2,157 | 13,439 | 903 | 0 |

Đây là dataset nhỏ với cache đã nóng; không phải load test hoặc cam kết SLA. Sequential scan có thể hợp lý ở kích thước này. Những index schema liên quan: `invoices_issued_idx(issued_at,id)` partial issued; `invoices_staff_idx(responsible_staff_id,issued_at,id)`; `invoices_customer_idx`; `invoice_items_invoice_idx`; `invoice_items_product_idx`; `invoice_items_variant_idx`; `credit_notes_invoice_idx`; `credit_note_items_original_idx`; unique order/invoice/source keys. Không thêm index chỉ để ép plan dùng index khi chưa có bằng chứng dataset lớn.

Các câu trong [database/query-catalog.sql](database/query-catalog.sql) đã được thực thi read-only thành9 resultsets với số hàng/golden đúng trong [query-catalog-result.json](../tests/BotZone.IntegrationTests/query-catalog-result.json). EF1002 được tắt có phạm vi trong hai lớp invoice sau khi rà soát: mọi fragment là constant/whitelist, mọi giá trị—including request JSON và file manifest—là NpgsqlParameter. Không dùng NoWarn toàn project; thêm SQL vào hai lớp này phải giữ cùng ranh giới parameter.

## Cách đọc kết quả và giới hạn

Golden chỉ là tập fixture xác định, không phải dashboard toàn database. Trong report net, kỳ có giảm có thể âm; đây là kết quả event đúng, không tự dồn CN về tháng invoice. XLSX lưu tiền lớn hơn15 chữ số dạng text để tránh Excel mất chính xác; serial luôn text.

Danh sách/export giới hạn10.000 invoice; report export tối đa100.000 dòng thống kê. Cần thu hẹp bộ lọc khi vượt giới hạn. Hướng mở rộng dataset lớn cần đo lại chi phí invoice_facts, phân trang OFFSET và dòng dimension×period; kết quả hiện tại chưa chứng minh throughput hay latency production. Những ca chưa đủ trong BA được nêu ở [test-evidence.md](test-evidence.md), không suy ra pass từ plan chạy thành công.
