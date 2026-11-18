# Constraints và ranh giới transaction

Tài liệu đối chiếu mã thực ngày 13/11/2026, BA v1.1 WinForms. PostgreSQL là nguồn dữ liệu chung của API C#, storefront và WinForms. EF Core là chủ sở hữu duy nhất của migration hiện hành; không chạy lại legacy migration runner song song.

## Nguồn schema

- Legacy được giữ nguyên để truy nguyên: [0001_core.sql](../../migrations/0001_core.sql), [0002_guards.sql](../../migrations/0002_guards.sql).
- Chuỗi EF: AdoptLegacyAndIdentity → IntegrityRepairAndPreview → WarrantyQuantityAndLedgerSources → PolicySnapshotsAndCustody → IdentityNamesAndFinancialHistory, trong [Data/Migrations](../../src/BotZone.Infrastructure/Data/Migrations).
- Bản sửa guard thực thi qua EF: [integrity-repair.sql](../../src/BotZone.Infrastructure/Data/Sql/integrity-repair.sql), [aftersales-sources.sql](../../src/BotZone.Infrastructure/Data/Sql/aftersales-sources.sql), [financial-history.sql](../../src/BotZone.Infrastructure/Data/Sql/financial-history.sql).
- [current-schema.sql](current-schema.sql) là ảnh 90 bảng gốc trước EF để đối chiếu; schema hiện hành 100 bảng nằm trong [schema-snapshot.json](schema-snapshot.json); [data-dictionary.md](data-dictionary.md), [erd.md](erd.md) mô tả cột/quan hệ. Không dùng schema dump để ghi đè database hiện hành.

## Các invariant do database bảo vệ

| Nhóm | Ràng buộc thực tế | Ý nghĩa |
|---|---|---|
| Định danh | PK/FK; unique business SKU, invoice_no, order_id trên invoices, normalized identifier, event/idempotency keys | Không trùng SKU/số hóa đơn, một invoice cho một order, không phát lại một nguồn ledger/event |
| Quan hệ đúng cha | Composite FK invoice/order/customer; invoice item/order item; parent Care/invoice; variant/product; receipt/refund/unit cùng order | UUID tồn tại vẫn bị từ chối nếu thuộc sai order/invoice/cha |
| Miền số | Tiền bigint VND; quantity dương; discount không âm và net=gross−voucher−points; điểm nguyên; point_value_vnd snapshot | Không dùng float cho tiền; đổi giá trị điểm sau này không tính lại đơn cũ |
| Order snapshot | guard_order_header/order_snapshot; trạng thái building chỉ để dựng trong transaction | Sau chốt không sửa customer/tiền/method/address/currency/policy/lines/units; handler hiện tại có thể đổi nhưng invoice giữ snapshot riêng |
| Invoice | invoice_immutable, invoice_item_immutable | Issued không UPDATE/DELETE, không issued→draft để sửa; dòng issued không bị thêm/chuyển cha/sửa/xóa |
| Credit note | credit_note_immutable, credit_items_immutable, credit_units_immutable | CN issued bất biến; guard xem cả OLD parent khi chuyển con; quantity hoặc amount phải dương; nguyên đơn vị nguồn được tham chiếu |
| Tender/refund | receipts_append_only, tenders_append_only, refund_allocations_immutable, refund_mutation_guard | Khoản thu và phân bổ bất biến; refund completed/failed không mở lại hoặc sửa nguồn/amount; retry tạo record mới |
| Khuyến mãi | promotion_version_guard, promotion_target_guard | Version/target đã có redemption không được viết lại; rule mới phải version mới |
| Kho/giá vốn | allocation_source_guard/reservation_source_guard/receipt_item_source_guard/lot_source_guard; allocation_history_guard/lot_history_guard | Đúng SKU/tracking mode/lot/giá vốn, mỗi allocation một frozen unit; không nhận quá PO; nguồn/giá vốn lịch sử bất biến, chỉ trạng thái/tồn hiện hành thay đổi hợp lệ |
| Thu cũ | trade_quote_history_guard, trade_quote_item_history_guard, trade_quote_append_guard; unique nguồn cấp credit | Không sửa offered amount/expiry/version đã lưu; quyết định chốt không bị đảo; không thêm item vào quote đã có quyết định; không cấp trùng giá trị |
| Care/quarantine | replacement_source_guard/replacement_immutable/quarantine_source_guard; unique(unit,lot) | Thay đúng entitlement/cùng SKU, serial pair hoặc lot pair; quantity quarantine không tự cộng tồn bán; nguồn quarantine không đổi |
| Ledger/audit | stock_append_only, credit_ledger_append_only, loyalty_append_only, audit_append_only; nguồn ledger kiểm deferred | Lịch sử chỉ append; điều chỉnh bằng sự kiện bù; credit spend/refund và loyalty phải đúng customer/order/unit |
| CMS/settings | landing version/publication/settings history append-only; cms_permission | Draft có revision, published history bất biến; DB session role content_staff mới ghi CMS theo command API |

Các CHECK/UNIQUE/FK không thay thế kiểm quyền và state transition trong service. Ví dụ thời hạn trả14 ngày, owner duyệt giá/hoàn, warehouse kiểm hàng, entitlement còn hiệu lực và customer ownership đều được API kiểm lại. Session variables cho DB guard do API đặt trong transaction; đây không phải một hệ thống PostgreSQL RLS độc lập cho người giữ runtime connection string.

## Kiểm tra deferred ở lúc commit

`order_totals`, `order_lines_totals`, `order_units_totals` bảo đảm header–lines–fixed ordinals khớp và không commit order còn building. `invoice_totals`/`invoice_lines_totals` kiểm header với dòng và nguồn order khi phát hành. `receipt_totals`/`tender_totals` kiểm tổng phân bổ receipt và trần frozen unit. `credit_totals`/`credit_item_totals`/`credit_unit_totals` kiểm số lượng/giá trị CN với dòng và đơn vị gốc. `refund_totals`/`refund_allocation_totals` kiểm tổng, source receipt/unit, trần đã thu và CN. `credit_ledger_source`/`loyalty_ledger_source` kiểm nguồn và customer sau khi các dòng liên quan đã được ghi.

Các constraint trigger này `DEFERRABLE INITIALLY DEFERRED`: order building, draft invoice/CN và các children có thể tạm chưa đủ trong cùng transaction, nhưng trạng thái cuối phải hợp lệ. Integration tests dùng `SET CONSTRAINTS ALL IMMEDIATE` trước rollback để không bỏ qua invariant chỉ được kiểm tại commit. Invoice sequence dùng `nextval`; transaction rollback có thể để lại khoảng trống số, không tái sử dụng số đã cấp.

## Transaction ghi nghiệp vụ

[SqlStore.Transaction](../../src/BotZone.Infrastructure/Services/SqlStore.cs) dùng ReadCommitted trên một EF-owned connection; mặc định lấy `pg_advisory_xact_lock(12648321)`. Role/active staff và session còn hiệu lực được kiểm trong transaction, sau đó đặt `botzone.actor_role`/`botzone.actor_id` bằng `set_config(...,true)` để không rò qua connection pool. Resource rows cần thiết dùng FOR UPDATE; kiểm role hiện hành dùng FOR SHARE.

[CommandService](../../src/BotZone.Infrastructure/Services/CommandService.cs) kiểm command permission và customer ownership trước khi đọc cache idempotency. Khóa `(actor:command,key)` cùng request SHA-256 trả lại kết quả đã commit khi retry cùng payload; đổi payload với key cũ trả409. Business mutation, idempotency result và audit commit chung. Bất kỳ lỗi nào trước commit đều rollback; không trả success khi commit thất bại. Retry từ UI giữ key cho cùng nội dung.

| Luồng | Các ghi phải nguyên tử |
|---|---|
| Checkout | Tính lại quote phía server → so quote hash → order building/lines/ordinals frozen → pending → giữ allocation/serial/quantity/voucher/benefit holds |
| Payment | Kiểm ownership/method/state/amount → payment event → receipt+tender allocations → consume holds hoặc reconciliation nếu kết quả đến muộn |
| Handoff/invoice | Kiểm confirmed → trừ tồn/chuyển serial, allocation và movement → tạo hoặc dùng draft → issue invoice → shipment/event → entitlement; lỗi invoice phải hoàn tác cả kho |
| Cancel/expiry | Release stock/voucher/benefit holds, cập nhật trạng thái; nếu đã thu, để owner hoàn đúng nguồn, không xóa receipt |
| Return/CN/refund | Kiểm intake/inspection/owner approval → CN frozen units → hủy/hoàn Care theo quyền lợi → refund theo receipt/unit original tender → ledger credit/points; không hoàn voucher/điểm thành cash |
| Trade-in | Quote version/approval → customer quyết định → finalize một lần → store-credit ledger và balance; hàng cũ không đi vào tồn hàng mới |
| Care replacement | Owner approval, warehouse same-SKU custody/allocation/movement và entitlement liên kết mới; giữ ngày quyền lợi, không tạo doanh thu bán mới |

Global advisory lock hiện ưu tiên tính đúng cho demo; các test hai DbContext chứng minh không oversell/overquota/double receipt/over-refund nhưng không chứng minh throughput production. Không được bỏ lock để tối ưu mà chưa bổ sung concurrency tests và ràng buộc tương ứng.

## Transaction đọc và export

[InvoiceQueryService](../../src/BotZone.Infrastructure/Invoices/InvoiceQueryService.cs) mở RepeatableRead READ ONLY cho list/count/sum/detail/report, dùng chung whitelist predicate và access scope nằm ngoài user OR/NOT. `readAt` lấy transaction_timestamp. Product/receipt/refund aggregates giữ đúng cấp dữ liệu để không nhân tổng qua joins.

[InvoiceExportService](../../src/BotZone.Infrastructure/Invoices/InvoiceExportService.cs) materialize dataset trong một snapshot, kết thúc read transaction, tạo metadata building và render XLSX/PDF từ cùng object. Hai định dạng không query lại khi render. File tạo private bằng CreateNew, manifest có dataset checksum và SHA-256 từng file; chỉ ready mới được download. File system và PostgreSQL không phải distributed transaction: lỗi render đánh dấu failed, không quảng bá file dở dang. Download kiểm requester, current role/active staff hoặc customer ownership, scope report, expiry, size và checksum.

DB-T14 đã ép writer trên PostgreSQL backend khác commit giữa đọc header và đọc item của export: export giữ dữ liệu cũ, fresh read thấy dữ liệu mới. Dòng tổng PDF nằm trong table, giữ với data row cuối; điều kiện PDF là mô tả dễ đọc, JSON audit giữ trong XLSX.

## Outbox và tác vụ ngoài database

[LocalWorker](../../src/BotZone.Api/LocalWorker.cs) xử lý expiry/loyalty/trade-in trong transaction; gửi email ở bước riêng. Outbox claim dùng FOR UPDATE SKIP LOCKED, chuyển processing, tăng attempts và lease `next_attempt_at` thêm2 phút rồi **commit trước khi gọi SMTP**. Hoàn tất chỉ khi attempt còn khớp lease; notification ID bằng outbox ID và ON CONFLICT để không nhân notification. Retry có backoff, tối đa5 attempts; email giữ Message-Id từ event UUID.

SMTP có ngữ nghĩa **at least once**. Nếu process chết sau SMTP thành công nhưng trước đánh dấu completed, lease hết hạn có thể gửi lại email. Stable Message-Id hỗ trợ nhận biết, không tạo bảo đảm exactly-once cho nhà cung cấp SMTP. Không giữ transaction DB/global command lock trong lúc chờ mạng SMTP.

## Bằng chứng và giới hạn

[73 ca integration](../../tests/BotZone.IntegrationTests/last-result.json) gồm15 SQL guard negatives, checkout/hậu mãi,4 tranh chấp connection, export có writer, payment dispatcher và DB-T17 rollback thật sau trừ tồn. [invoice-issue-result.json](../../tests/BotZone.IntegrationTests/invoice-issue-result.json) ghi IDs và cleanup bằng nghiệp vụ; không xóa issued history. [restore-verification.json](restore-verification.json) đối chiếu100 bảng/sequence/constraints trên restored DB; [restore-results.json](../../artifacts/api/restore-results.json) kiểm lại Identity/export/media/privacy qua HTTP.

Expected độc lập nằm ở [expected-results.json](../../expected-results.json), không sinh bằng query đang kiểm. Toàn bộ trạng thái nghiệm thu và phần chưa đủ được giữ ở [test-evidence.md](../test-evidence.md). Không suy từ schema/migration thành công rằng mọi tổ hợp nghiệp vụ hay mọi race đều đã được kiểm.
