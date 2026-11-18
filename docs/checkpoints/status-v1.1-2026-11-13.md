# Tiến độ triển khai BotZone v1.1

Cập nhật 13/11/2026. Đã tiếp tục triển khai theo `BotZone_BA_v1.1_WinForms.md`; không còn trạng thái chờ đổi kiến trúc. Hồ sơ trước chuyển đổi được giữ tại `docs/checkpoints/implementation-status-before-v1.1.md`. Chi tiết quyết định: `docs/architecture-change-winforms.md`.

## Kiến trúc và dữ liệu

- Next.js/TypeScript chỉ storefront/proxy API. Toàn bộ quản trị là WinForms native .NET 10. ASP.NET Core API sở hữu auth, nghiệp vụ, query/export và worker; một PostgreSQL Docker, một migration owner EF Core/Npgsql.
- Hai SQL migration cũ giữ nguyên checksum. Năm migration EF đã kiểm clone rồi áp dụng main. Không InitialCreate lên bảng hiện hữu, không copy password hash/session Better Auth sang Identity.
- Main `botzone`: 100 bảng, seed golden + 35 model/37 SKU/chín nhóm. Golden đúng **97.130.000 − 3.500.000 = 93.630.000 VND**. NV03 inactive vẫn có lịch sử. D01 draft không cộng issued; PAG75 chỉ ở test clone.
- Fresh `botzone_integration_test_fresh_20261113` chạy cùng EF và seed hai lần; không nhân dữ liệu. Test clone `botzone_baseline_test_20261113135743_c0d401c6` chứa cả fixture/browser/concurrency, nên tổng toàn clone lớn hơn golden.
- Restore `botzone_restore_20261113153420_6d0c9293` khớp fingerprints/counts 100 bảng, sequences, golden và SHA256 ba media có bản ghi DB.
- Backup gốc: `backups/botzone-20261113-135647-8e85499a`; clone sau nghiệp vụ/media: `backups/botzone-20261113-153319-9685ef00`; main sau seed: `backups/botzone-20261113-153323-0bfe29ae`.
- Không reset Git/drop DB/xóa volume/xóa hàng loạt source, không deploy/push. Workspace không có Git metadata tại kiểm kê đầu. SQL/TS legacy giữ để truy vết, runner TS migration từ chối chạy.

## Mã đã triển khai

| Phần | Phạm vi thực tế |
|---|---|
| Auth | Identity hash/token, verify/reset Mailpit, cookie/session DB, CSRF hai client, revoke/current role/ownership. |
| Catalogue/kho | Category/model/SKU, VI/EN, compatibility, suppliers/PO/nhận một phần, serial/lô/quarantine, kiểm kê/owner duyệt. |
| Thương mại | Giỏ DB, voucher version/quota, quote server, điểm/credit snapshot, giữ tồn, checkout, COD/banking/MoMo/Visa demo, idempotency, confirm/handoff/invoice/shipment/reconciliation. |
| Hóa đơn | C# query dùng lại, `List<InvoiceListItemDto>`, scope + AST whitelist, pagination/matched total/detail; báo cáo tháng/quý/năm/staff/model/SKU; sáu báo cáo vận hành riêng. |
| Export | ClosedXML/PDFsharp-MigraDoc, font local, một dataset RepeatableRead cho cả XLSX/PDF, manifest/checksum/private download/expiry, xuất toàn bộ filter. |
| Hậu mãi | Trả từng unit/kiểm tra/CN/hoàn đúng nguồn, failed delivery, thu cũ quote/duyệt/khách quyết định/credit/freight, Care serial/quantity/repair/replace/refund. |
| Native admin | Login/Main, 21 module, BindingSource/DataGridView, native command forms/filter/report, async/cancellation, bốn role, CMS TreeView/VI–EN/asset/preset/history. |
| Storefront | Landing/catalogue/biến thể/compare/wishlist/cart/checkout/account/order/invoice/trade-in/Care/policies; VI/EN, VND/USD; checkout VND. |
| Media/CMS/3D | Ảnh/GLB validation, private ownership, revision/publish/restore immutable; preview grant một lần/snapshot/revoke; phone/duo GLB/explore/fold/reset/fallback. |
| Local/tooling | EF migrate/seed, native build/publish, API/web host + Docker DB/Mailpit, backup/restore DB mới, README/ERD/data dictionary/query catalog. |

## Bằng chứng và phạm vi đã quan sát

- Checkpoint C# integration **69/69**, DB thật: golden/guards/concurrency/financial snapshots/export writer overlap/dispatcher payment. Kết quả mới nhất: `tests/BotZone.IntegrationTests/last-result.json`.
- Bộ filter TypeScript cũ **10/10**. Next typecheck và production build đạt; lỗi viewer trước đây đã sửa.
- WinForms chạy **thực trên Windows**, 1366×768/1920×1080 tại DPI120/125%: login/list/detail/filter/report/export, 21 modules/sáu operational reports, CRUD native13 assertions và inventory API13 assertions. Build/publish native đạt.
- Browser thật: COD checkout→invoice→đối soát, khách tải PDF/XLSX, return private evidence, trade-in chấp nhận→credit; CMS publish/restore/preview/revoke; VI/EN bảy viewport BA, touch emulation/reduced motion/WebGL fallback.
- Auth HTTP/Mailpit tám ca; migration/seed chạy lặp; fresh DB; restore toàn bộ dữ liệu và media. Bằng chứng: `docs/database/main-seed-verification.json`, `fresh-seed-verification.json`, `restore-verification.json`.
- Đã xem ảnh native/web và render PDF để kiểm bố cục. Phát hiện tổng bị đẩy thành trang riêng, đã sửa source exporter để đặt tổng cùng dòng dữ liệu cuối; đang chốt ảnh sau sửa.

`docs/test-evidence.md` là ma trận từng mã DB-01…DB-07, AC-01…AC-16, DB-T01…DB-T20, WF-01…WF-08 và liên kết artifact. Mở module không được coi là đã chạy mọi nhánh mutation. Tổng clone có fixture khác phải lọc G01…G08 khi đối chiếu BA.

## Giới hạn còn phải ghi rõ

Chưa coi toàn bộ BA đã pass. Chưa nghiệm thu thủ công DPI100%/150%, nhiều màn hình, bàn phím vật lý, SaveFileDialog save/cancel, lỗi mạng/session trên native và demo khi tắt Internet. Không dùng build Linux, scale ảnh hoặc code kiểm focus để thay bằng chứng tương tác thật.

Theo chỉ đạo mới nhất, **Blender/model 3D giao cho Codex session khác của Khang**. Giữ model minh họa hiện có để tích hợp asset mới; không ghi đã dựng bằng Blender. Source fixture mới sửa loyalty maturity, còn ledger append-only của clone seed cũ được giữ nguyên và không dùng làm bằng chứng chính sách mới.

Chạy/build/tài khoản trong `README.md`; demo trong `docs/demo-script.md`; truy vết SQL→List→WinForms→file trong `docs/database/rubric-mapping.md`.
