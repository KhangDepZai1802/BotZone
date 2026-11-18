# Kiểm thử C# với PostgreSQL thật

Console integration runner .NET 10 tham chiếu trực tiếp Infrastructure. Chỉ cho phép database clone có prefix `botzone_baseline_test_` hoặc `botzone_integration_test_`; không reset/drop/migrate database.

```powershell
dotnet restore tests/BotZone.IntegrationTests/BotZone.IntegrationTests.csproj --ignore-failed-sources
dotnet build tests/BotZone.IntegrationTests/BotZone.IntegrationTests.csproj --no-restore
dotnet tests/BotZone.IntegrationTests/bin/Debug/net10.0/BotZone.IntegrationTests.dll
```

`BOTZONE_TEST_CONNECTION` nhận connection string Npgsql (host/port/user/password); `BOTZONE_TEST_DATABASE` chỉ tên clone. Mặc định là clone đã được xác nhận `botzone_baseline_test_20261113135743_c0d401c6` trên `127.0.0.1:54329` với role local `botzone_app`. Source database không được runner sử dụng.

`--catalog-only` tạo tài khoản, catalog lịch sử, stock, khuyến mãi, settings. `--seed-only` tạo thêm bộ golden rồi dừng. Chạy không flag kiểm tra seed idempotent, Identity password hash, 10 trường hợp tương ứng TS filter tests, golden/list/detail/report, scope, 75 invoice phân trang, file XLSX/PDF thực và download privacy. Có thêm 15 trường hợp SQL guard và các kịch bản hậu mãi gọi C# service thật trong transaction rollback, ép kiểm tra deferred constraints trước rollback. Kết quả từng assertion thực lưu `last-result.json`; không xem build thành công là pass integration.

Seed API: `new DemoSeeder(db).SeedAsync("catalog" | "golden" | "pagination" | "demo", ct)`. Demo dùng bộ golden. IDs xác định bằng `DemoSeeder.Id(key)`, marker `app_settings.seed:v1.1:*`, transaction Serializable và advisory lock. Không cập nhật dữ liệu invoice đã phát hành, không reset mật khẩu tài khoản đã tồn tại. Catalog fixture có nhãn giá mô phỏng. Đây là fixture kiểm thử được bật tường minh, không tự chạy ở production.

Tài khoản mới đều email verified, mật khẩu local `BotZoneDemo!2026`: `owner@botzone.local`, `nv01@botzone.local`, `nv02@botzone.local`, `nv03@botzone.local`, `warehouse@botzone.local`, `content@botzone.local`, `customer@botzone.local`, `customer2@botzone.local`. `staff_profiles` là nguồn role; hash tạo bởi ASP.NET Core Identity PasswordHasher.

Seed mới đặt NV03 inactive để chứng minh doanh số lịch sử vẫn được giữ; dùng NV01/NV02 để thao tác order staff. Chín nhóm có tên hiển thị iPhone, Mac, iPad, Apple Watch, AirPods, Apple Vision, TV & Nhà, Phụ kiện, Màn hình. Các chỉnh sửa seed này chỉ áp dụng INSERT mới; không sửa tên nhóm, trạng thái staff hoặc mật khẩu trong clone đã seed trước đó.

Golden gồm G01–G08 (97.130.000 VND), CN01/CN02 (3.500.000 VND), draft D01 (99.000.000 VND) và đơn cancelled chưa xuất hóa đơn. Pagination75 prefix PAG được seed sau phần golden. Khi cả hai profile tồn tại, bộ lọc OR `invoiceNo = G01 ... G08` cô lập golden; lọc nhanh PAG cô lập pagination. Không dùng tổng mặc định sau khi thêm dữ liệu khác để đối chiếu bộ golden.

Nguồn tên/model historical đã kiểm ngày 13/11/2026: [iPhone 13](https://support.apple.com/en-us/111872), [iPhone 14](https://support.apple.com/en-us/111850), [AirPods Pro 2 USB-C](https://support.apple.com/en-us/111834). Giá/stock/orders là dữ liệu mô phỏng có chủ đích, không phải dữ liệu Apple công bố.

Hậu mãi đã có kiểm thử trực tiếp: thu cũ có duyệt giá/phiên bản/hạn trả lời/cấp một lần, gửi từ xa từ chối trả lại và chi phí vận chuyển, trả một phần đúng frozen unit/điểm/nguồn tiền, Care chưa dùng/phí giao hoàn toàn bộ, COD giao thất bại không tạo khoản hoàn giả, hủy đơn đã thu trước phát hành, Care loại trừ rơi vỡ và đổi cùng SKU cho cả serial/lô quantity. Các kịch bản checkout gọi Quote → place/payment/confirm/handoff/delivery thật; tất cả transaction nghiệp vụ bổ sung rollback để không thay golden.

Runner hiện có 73 ca đạt trong `last-result.json`: bổ sung tranh tồn cuối/voucher cuối/payment cùng key/refund hai key bằng các DbContext riêng; đơn fixture được hủy qua command và SKU fixture khóa lại. Policy điểm 2.000 VND được kiểm bằng checkout thật trong transaction rollback, unit cũ giữ snapshot 1.000 VND. Payment dispatcher được kiểm cho MoMo/Visa mọi outcome/retry, quyền banking/COD/customer khác, duplicate place và late reconciliation; `payment-dispatch-result.json` ghi IDs cùng cancel/refund cleanup. `BaRequirementChecks` kiểm voucher invalid, toàn36 tháng/12 quý, rename/reassign staff trong rollback và hai AST precedence; `ReadProjectionChecks` kiểm payload kho và chạy9 query SQL catalog read-only.

`InvoiceIssueChecks` kiểm no-draft/existing-draft, retry number và rollback thật khi Invoice thất bại sau trừ tồn. Các issued fixture được thu hồi/kiểm mới/owner restock và phát hành CN qua command; giữ lịch sử, không xóa invoice. Lượt chạy sau có thể tăng tổng invoice toàn DB; golden/PAG được lọc riêng. `invoice-issue-result.json` lưu IDs và cleanup. `python tests/BotZone.IntegrationTests/review_pdf.py` (cần PyMuPDF local) render/check toàn bộ trang của ba file thực tại `artifacts/pdf-review-fixed`; tổng PDF là table row giữ với data row cuối và filter là mô tả dễ đọc.

`InvoiceSnapshotChecks` buộc writer connection khác commit trong lúc export RepeatableRead đã đọc header và đang chờ đọc items. XLSX giữ JSON cũ, fresh read thấy JSON mới, PDF mở và cả hai checksum khớp manifest chung. Chỉ draft fixture D01 được thay JSON tạm rồi phục hồi, không sửa issued. Bằng chứng gồm hai PID và thời điểm trong `export-snapshot-result.json`. `QueryPlanEvidence` lưu SQL+parameter+EXPLAIN ANALYZE BUFFERS thật cho list và ba reports trong `query-plans.json`.

Chưa bao phủ đầy đủ bởi runner này: WinForms thao tác thủ công/DPI khác, mọi tổ hợp sửa chữa/hoàn Care, hai checkout tranh cùng credit balance. Backup/restore DB+media có evidence riêng trong `docs/database/restore-verification.json` và `artifacts/api/restore-results.json`. Không tự suy ra toàn bộ AC/DB-T đã pass. Xem đối chiếu đầy đủ trong [test-evidence.md](../../docs/test-evidence.md).
