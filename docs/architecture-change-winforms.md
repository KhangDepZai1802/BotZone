# Chuyển kiến trúc BotZone theo BA v1.1

Ngày 13/11/2026. Lệnh tiếp tục triển khai của Khang thay trạng thái tạm dừng trước đó. Đã đọc toàn bộ `BotZone_BA_v1.1_WinForms.md`; bản này là căn cứ kiến trúc, auth, truy cập DB và exporter. [Hồ sơ tạm dừng](architecture-change-2026-11-13.md) được giữ như lịch sử.

## Kiểm kê trước chuyển đổi

Workspace không có `.git` tại thời điểm kiểm tra; không reset, commit hoặc push. PostgreSQL 18.1 và Mailpit đã chạy, hai migration SQL đã được áp dụng, 90 bảng kể cả `schema_migrations`. Kiểm kê từng bảng xác nhận chưa có dữ liệu nghiệp vụ hoặc tài khoản, không chỉ suy từ trạng thái seed. Các SQL gốc, storefront, fixture/bộ lọc, asset và backend TypeScript được giữ.

| Migration đã thực sự áp dụng | SHA-256 đối chiếu history và bytes gốc |
|---|---|
| `0001_core.sql` | `a22cc34f8aa4415965825b95e7788459e7d8d225066be04013a743e96316e5a8` |
| `0002_guards.sql` | `937c733b762cda5e63ba6c45c98eece383882f174144ffc28f92fc9ffc03a282` |

Backup trước chuyển: `backups/botzone-20261113-135647-8e85499a`; SHA-256 database.dump `A0DCACC66C885A5D9B46CF860AA78C1BF53E62874243F40B47F317067B944208`. Clone `botzone_baseline_test_20261113135743_c0d401c6` được restore và so từng bảng trước khi EF chạy. [Kiểm kê](database/inventory-before-ef.md), [schema gốc](database/current-schema.sql), [manifest fingerprints](database/ef-baseline-manifest.json), [đối chiếu baseline](database/baseline-clone-verification.json).

## Một lõi nghiệp vụ, hai client

| Thành phần | Chủ sở hữu và đường đi thực tế |
|---|---|
| Next.js + TypeScript | Storefront/renderer 3D; `/api/*` proxy về C#. `.env` web chỉ có base URL API và telemetry. Không query PostgreSQL, seed, chạy worker hoặc migration. |
| C# WinForms .NET 10 | LoginForm/MainForm/UserControl/DataGridView/BindingSource/DateTimePicker/TreeView/native dialogs; HttpClient async + CookieContainer trong bộ nhớ. Không connection string DB. |
| ASP.NET Core API | Identity, session/CSRF, permissions, business transactions, read models, media, exporter và worker local. |
| EF Core + Npgsql | Model/schema duy nhất, migration owner duy nhất. Query hóa đơn materialize `List<InvoiceListItemDto>` bằng `ToListAsync()`. SQL tham số hóa dùng cùng connection/transaction EF. |
| PostgreSQL Docker | Dữ liệu nghiệp vụ, Identity/session, audit/outbox, constraints/triggers. Runtime `botzone_app` khác migration owner `botzone`. |
| Local files/Mailpit | `uploads` chứa media public/private, exports và Data Protection keys; email chỉ SMTP local. Không dịch vụ cloud. |

`src/server`, `src/db` và hai SQL migration được giữ để truy vết/đối chiếu. `scripts/migrate.ts` chủ động từ chối chạy; các runner cũ không còn là lệnh hoạt động. Giữ 10 kiểm thử filter TypeScript và đối chiếu hành vi với C#. CSS admin lịch sử không tạo ra giao diện admin web.

## Baseline có kiểm chứng

EF không chạy InitialCreate lên bảng hiện hữu. `MigrationSql.Adopt()` kiểm checksum lịch sử và fingerprints columns/constraints/indexes/triggers/functions của schema gốc; drift không nhận diện thì dừng. Trên DB mới hoàn toàn, EF áp dụng SQL lịch sử embedded rồi tiếp tục cùng chuỗi migration. Không tồn tại hai runner đồng thời.

| EF migration | Thay đổi có kiểm soát |
|---|---|
| `20261113070723_AdoptLegacyAndIdentity` | Nhận baseline sau khi đối chiếu; thêm Identity, session và preview grant; giữ khóa người dùng TEXT và FK nghiệp vụ. |
| `20261113072619_IntegrityRepairAndPreview` | Sửa trigger legacy tham chiếu field không tồn tại đã phát hiện khi seed; bổ sung guard tài chính/quan hệ và snapshot preview. |
| `20261113073844_WarrantyQuantityAndLedgerSources` | Care cho hàng theo số lượng, quarantine có nguồn, kiểm credit/loyalty ledger. |
| `20261113075041_PolicySnapshotsAndCustody` | Lưu giá trị điểm theo unit, unique quarantine và chống nhận trùng định danh hàng thu cũ chưa trả. |
| `20261113080440_IdentityNamesAndFinancialHistory` | Chuẩn hóa snake_case Identity bằng rename giữ dữ liệu; bất biến báo giá đã quyết định, giá vốn/nguồn kho và điều chỉnh tài chính. |

Cả năm đã qua clone trước khi áp dụng `botzone`. Fresh DB `botzone_integration_test_fresh_20261113` đã chạy được toàn chuỗi. Main migration lần hai báo không có migration mới; seed demo chạy hai lần không nhân bản. Model hiện tại: 100 bảng, 649 cột, 182 FK; xem [data dictionary](database/data-dictionary.md) và [ERD](database/erd.md) được sinh từ catalog thực tế.

`Down` chủ động từ chối phá hủy dữ liệu. Khôi phục cần backup có kiểm chứng vào DB riêng hoặc forward migration được đánh giá; không suy rằng reset là cách xử lý lỗi.

## Auth cutover

Không copy Better Auth password hash/cookie/session sang Identity. Với tài khoản cũ, giữ nguyên `user.id` và toàn bộ liên kết nghiệp vụ, bổ sung Identity fields, đánh dấu cần reset mật khẩu; bảng auth legacy được giữ nhưng runtime mất quyền truy cập. Không có tài khoản thực tại thời điểm kiểm kê, vì vậy seed tạo tài khoản demo mới bằng `PasswordHasher<User>` chuẩn Identity.

Hai client dùng cookie ticket được ASP.NET Core bảo vệ, tham chiếu `auth_sessions` trong DB. API kiểm expiry/revocation/security stamp/current staff role; reset/đổi mật khẩu/khóa hoặc đổi quyền thu hồi phiên. CSRF áp dụng cả WinForms và web, kể cả login và upload. Role lấy từ `staff_profiles`; owner không được vượt quyền content-only CMS. Demo không dùng token dài hạn lưu trên máy desktop.

## Phần tái sử dụng và kiểm chứng

- Storefront VI/EN, VND/USD, assets local và viewer 3D được giữ, sửa typecheck và nối API. Next production build đã đạt.
- Công thức tài chính chuyển sang C# số nguyên VND, phân bổ theo unit và giữ snapshot; không tính lại bằng giá hiện tại khi trả/hoàn. Seed golden đúng 97.130.000 − 3.500.000 = 93.630.000 VND.
- List/count/tổng/report dùng cùng predicate whitelist, quyền đặt ngoài biểu thức người dùng. Export vật chất hóa một dataset RepeatableRead rồi tạo XLSX/PDF; có test writer commit thật lúc snapshot còn mở.
- Native CMS có section tree, VI/EN, asset/product/preset, giới hạn 3D, nháp/publish/history/restore. Preview mở renderer storefront với quyền preview riêng, code một lần, TTL và revoke.
- WinForms đã chạy thực trên Windows ở DPI 125%, có ảnh/bằng chứng. Không gọi build Linux hoặc scale ảnh là test desktop. Các kiểm tra chưa chạy được ghi riêng trong [test evidence](test-evidence.md).

Model Blender do một Codex session khác phụ trách; Khang đã bàn giao v003 và yêu cầu tích hợp. Bốn GLB/hai poster cùng manifest được đưa vào storefront hiện tại, không sửa source gốc hoặc dựng lại Blender. CMS WinForms chọn các ID versioned, backend C# kiểm hash trước save/publish; không có schema hoặc migration mới. Xem [tích hợp v003](3d/v003/integration.md). GLB minh họa cũ vẫn giữ để truy vết.

Cập nhật ảnh thật của catalogue dùng `ProductPhotoImporter` qua kết nối EF hiện tại: chỉ `products.poster` và audit, không chạy lại seed. Màu/ảnh/nguồn được API cung cấp từ manifest nội dung chung; Next chỉ trình bày và phân trang, không thêm truy cập PostgreSQL hay quy tắc nghiệp vụ cạnh tranh.

Đợt thông số/so sánh/auth tiếp theo bổ sung609 mục VI/EN vào JSON `products.specs` bằng `ProductSpecificationImporter`, giữ metadata và audit. C# cung cấp nhóm thông số/nhãn biến thể/lỗi theo locale; Next chỉ trình bày bảng so sánh và hiệu ứng. Đăng ký dùng Identity/profile/address hiện có trong một transaction; rememberMe chỉ làm persistent cookie web với expiry8giờ cũ, WinForms không lưu cookie/mật khẩu. Không tạo bảng, migration hay cơ chế auth khác. Kiểm tra mutation trên clone dùng cùng binary ở cổng thử5091, không thay kiến trúc backend duy nhất. [Tài liệu và bằng chứng](storefront-ux-localization.md).

## Bảo toàn và bằng chứng

Không drop DB, xóa volume, reset Git, xóa hàng loạt source hoặc gửi remote. Fixture giao dịch test nằm trên clone; dọn fixture bằng trạng thái nghiệp vụ/cancel/refund, giữ audit và lịch sử. Main chỉ có seed demo có marker; bộ PAG75 dành cho test, không tự trộn vào tổng golden.

Các bằng chứng chạy và giới hạn nghiệm thu: [implementation status](implementation-status.md), [test evidence](test-evidence.md), [main seed](database/main-seed-verification.json), [fresh seed](database/fresh-seed-verification.json). Backup sau chuyển/restore nghiệp vụ + media đã khớp tại [restore-verification.json](database/restore-verification.json); [HTTP restore](../artifacts/api/restore-results.json) xác nhận Identity, export và quyền tải media trên DB/uploads phục hồi.
