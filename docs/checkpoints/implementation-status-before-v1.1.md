# Tiến độ triển khai BotZone

Chốt hiện trạng **13/11/2026** sau yêu cầu đổi kiến trúc của Khang. Đây là hồ sơ công việc dang dở, không phải biên bản nghiệm thu ứng dụng.

## Yêu cầu mới có hiệu lực

- Storefront tiếp tục là web; **toàn bộ giao diện quản trị phải dùng C# Windows Forms**.
- Đã dừng phát triển admin web và các tác nhân triển khai để ghi nhận hiện trạng.
- Giữ nguyên source, database, volumes và tài nguyên; không reset/xóa dự án.
- Chưa tạo WinForms, backend mới hoặc bộ migration thứ hai.
- Giữ BA v1.0 gốc; yêu cầu mới thay phần admin web của BA cũ. Xem [hồ sơ đổi kiến trúc](architecture-change-2026-11-13.md).

## Mã và tài nguyên đã có

| Phần | Tệp chính | Trạng thái thực tế |
| --- | --- | --- |
| Khảo sát | BA và yêu cầu đính kèm | Đã đọc toàn bộ; workspace lúc đầu chỉ có BA, chưa có `.git`; không ghi đè công việc cũ |
| Dependencies | package.json, package-lock.json | Đã cài Next/React/TypeScript/Drizzle/Better Auth, UI/3D/export; khóa phiên bản; chưa build thành công |
| Hạ tầng | Dockerfile, compose.yaml, .env.example, scripts/*.ps1 | PostgreSQL/Mailpit đang chạy, bootstrap roles xong; app/worker Docker chưa build/chạy |
| Database | migrations/0001_core.sql, 0002_guards.sql, scripts/migrate.ts | Hai migration đã áp dụng. Có schema và guards; chưa kiểm chứng invariants bằng integration tests |
| Drizzle/auth | src/db/schema.ts; src/server/db.ts, auth.ts, errors.ts; api/auth route | Có mapping auth, Better Auth/session/role/email outbox; mapping Drizzle bảng nghiệp vụ chưa đầy đủ; chưa test auth |
| Tiền/thương mại | src/lib/money.ts; src/server/commerce.ts | Mã phân bổ VND, quote, giữ tồn/voucher/benefits, payment demo, xuất kho/invoice, expiry/cancel; chưa tích hợp API dispatcher, chưa test DB |
| Hóa đơn/báo cáo | src/server/invoice-filter.ts, invoices.ts | Query AST/permission/list/detail/report/snapshot đã viết; 10 unit tests filter đạt; chưa chạy golden hoặc query DB nghiệp vụ |
| Export | src/server/exports.ts; public/fonts | Có ExcelJS/PDFKit, checksum/manifest/private download; chưa sinh/mở file XLSX/PDF thực |
| Operations | src/server/operations.ts | Dang dở: helper và một số nhánh address/variant/translation/staff/promotion/CMS; dispatcher hậu mãi/kho còn trống; chưa nối route/test |
| Storefront | src/app/[locale], src/components, src/styles/globals.css | Có shell VI/EN, catalogue/detail, cart context, UI primitives và viewer 3D đang viết; chưa chạy xuyên suốt |
| Admin web | Chưa có src/app/admin hoặc component admin riêng | Đã dừng; giữ CSS `.admin-*` và hợp đồng dự kiến cũ để đối chiếu |
| Assets | public/media/*.svg; NotoSans-Regular.ttf, OFL.txt | 12 SVG minh họa gốc và font local; chưa có GLB, asset manifest tổng thể hoặc tài liệu design system |
| Tài liệu | README, api-contract, environment, demo-script | Bản nháp cho triển khai; đã gắn nhãn trạng thái/chuyển sang WinForms |

## Database tại điểm dừng

Truy vấn chỉ đọc sau yêu cầu đổi kiến trúc xác nhận:

- PostgreSQL `18.1 (Debian 18.1-1.pgdg12+2)`.
- Đã áp dụng `0001_core.sql` và `0002_guards.sql`; không tạo migration 0003.
- Có **90 base tables**, tính cả `schema_migrations`.
- `orders = 0`, `invoices = 0`, `products = 0`, auth `user = 0`.
- Chưa có scripts/seed.ts, scripts/worker.ts, fixture golden hoặc expected-results.json; tài khoản README mới là dự kiến.
- Sau yêu cầu mới không chạy migration/seed hoặc ghi dữ liệu nghiệp vụ. DB/volumes được giữ nguyên.

## Bằng chứng đã chạy

| Lệnh/kiểm tra | Kết quả |
| --- | --- |
| Node/npm/PowerShell | Node 22.20.0, npm 10.9.3, PowerShell 5.1.26100.9818 |
| Docker manifests; docker compose config --quiet | Image tags và Compose hợp lệ |
| docker compose up -d db mailpit bootstrap | DB/Mailpit healthy; bootstrap exit 0 |
| SQL server/roles | PostgreSQL 18.1; botzone và botzone_app không superuser; runtime không có quyền tạo schema |
| Mailpit /api/v1/info | HTTP 200; chưa test email từ app |
| Parse 4 script PowerShell | Không lỗi cú pháp trên 5.1; chưa thực thi backup/restore end-to-end |
| npm.cmd run db:migrate | Applied 0001_core.sql và 0002_guards.sql; chưa test chạy lặp/restore |
| node_modules\.bin\tsx.cmd --test tests/unit/invoice-filter.test.ts | **10/10 đạt**; main đã chạy lại xác nhận. Không phải test query/transaction PostgreSQL |
| npm.cmd run typecheck | **Thất bại** tại src/components/product-scene.tsx(25,1320): TS1003 Identifier expected. Chưa xác nhận code còn lại biên dịch được |
| dotnet --list-sdks | Có 9.0.201, 10.0.103, 10.0.203 |
| dotnet --list-runtimes | Có WindowsDesktop.App, gồm 10.0.7; chưa chọn target framework hoặc tạo WinForms |

Npm báo 6 cảnh báo moderate sau khi cài dev dependencies; chưa phân tích audit hoặc áp fix. UI/UX Pro Max đã đọc/chạy truy vấn hướng dẫn; BA đen/trắng/sans serif được ưu tiên hơn gợi ý không phù hợp. Blender chưa xác nhận: không có tool session, probe localhost:9876 không kết nối.

## Phần dang dở cần tiếp tục sau khi đối chiếu BA cập nhật

1. Ánh xạ toàn bộ quản trị sang WinForms; thống nhất API/auth desktop và một lõi nghiệp vụ/một chuỗi migrations trước khi code. Chưa thay backend hiện tại.
2. Sửa lỗi viewer và hoàn thiện storefront. storefront.tsx còn gọi Cart, AuthPage, Account, Orders, OrderDetail, InvoiceDetail, Compare, TradeIn, Care, Policy chưa định nghĩa.
3. Rà soát/typecheck backend; hiện mới có route auth. Các endpoint nghiệp vụ trong api-contract phần lớn chưa được tạo.
4. Hoàn thiện operations hậu mãi/kho/settings; helper loyalty chưa thống nhất convention snapshot multiplier; không đăng ký dispatcher dang dở như chức năng hoàn thành.
5. Seed/worker/golden, kiểm thử DB thật/quyền/đồng thời, wrong-parent IDs, tiền/tồn; không suy đạt từ migration thành công.
6. Tài liệu ERD/data dictionary/query catalog/rubric, asset manifest, file xuất mẫu và demo WinForms; kiểm backup/restore và offline.

**AC-01…AC-16 và DB-T01…DB-T20 chưa nghiệm thu.** Chưa build app, test browser/mobile/WebGL/offline hoặc mở file export. Không deploy/push/reset. Bản kê SHA-256 nguồn: `docs/checkpoints/2026-11-13.json`, không thay cho backup DB/media.
