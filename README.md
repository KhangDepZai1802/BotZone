# BotZone

Đồ án cửa hàng Apple theo [BA v1.1](BotZone_BA_v1.1_WinForms.md): storefront Việt/Anh dùng Next.js; toàn bộ quản trị tiếng Việt dùng **C# Windows Forms**. Hai ứng dụng gọi chung ASP.NET Core API; PostgreSQL chỉ được truy cập từ backend .NET. Thanh toán, vận chuyển và email phục vụ demo local.

Tên loại hệ thống: **Hệ thống thương mại điện tử và quản lý bán hàng**. Tên đề tài đề xuất: **Xây dựng hệ thống bán lẻ sản phẩm Apple BotZone với website thương mại điện tử và ứng dụng quản trị Windows Forms**.

Xem [trạng thái và bằng chứng kiểm thử](docs/implementation-status.md), [hướng dẫn WinForms](docs/winforms.md), [môi trường](docs/environment.md) và [kịch bản demo](docs/demo-script.md). Các lần kiểm thử có thay đổi nghiệp vụ chạy trên database clone; không coi số liệu của clone là số liệu database chính.

Storefront đã có [609 thông số VI/EN cho 35 model](docs/catalogue-spec-sources.md), bảng so sánh tối đa3 sản phẩm, phản hồi giỏ hàng có ảnh và trang đăng nhập/đăng ký riêng. [So sánh chi tiết](docs/product-comparison.md) dùng giao diện trắng–đen, nhấn CPU/GPU và các ưu điểm có điều kiện đối chiếu rõ, kèm lý do; không tự chấm điểm chất lượng tổng thể. Xem [chi tiết triển khai và kiểm chứng](docs/storefront-ux-localization.md). Với database vừa seed, nhập thông số bằng cùng backend C#; không chạy lại seed để cập nhật nội dung:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\scripts\import-product-specifications.ps1
powershell.exe -ExecutionPolicy Bypass -File .\scripts\import-product-specifications.ps1 -Apply
```

Lệnh đầu chỉ lập kế hoạch. Import giữ metadata cũ và ghi audit; chạy lại cùng dữ liệu là no-op, nội dung kỹ thuật khác với manifest sẽ bị từ chối để bảo toàn chỉnh sửa. Main hiện đã nhập đủ35 model.

## Chuẩn bị

- Windows 10/11, PowerShell 5.1+, Docker Desktop ở chế độ Linux containers.
- .NET SDK **10.0.203** theo `global.json`, .NET Desktop Runtime 10 để chạy bản WinForms framework-dependent.
- Node.js 22.20+ và npm. Dùng `npm.cmd` trong PowerShell nếu `npm.ps1` bị policy chặn.
- Cổng local: Next.js `3000`, API `5080`, PostgreSQL `54329`, SMTP `1025`, Mailpit `8025`.
- Lần đầu cần tải các dependency/image. Giữ `package-lock.json`, các `packages.lock.json`, `NuGet.Config` và `global.json`.

Compose chỉ có `db`, `mailpit`, `bootstrap`; giữ project `botzone` và hai named volumes hiện có. API cùng background worker chạy trong một tiến trình .NET trên host. WinForms là chương trình native, không có trang quản trị web.

## Khởi động

Mở PowerShell tại thư mục dự án, mở Docker Desktop và chờ engine sẵn sàng. Kiểm tra chỉ đọc:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-local.ps1 -CheckOnly
```

Đọc và cấu hình `config/backend.local.json` theo mẫu `config/backend.local.example.json`. Script chỉ tạo file nếu chưa tồn tại, không ghi đè cấu hình đang có. Web dùng `.env` theo `.env.example`, chỉ chứa `BOTZONE_API_URL=http://127.0.0.1:5080` và tùy chọn telemetry; không đặt connection string, SMTP hay secret backend trong env của Next.js.

Với database đã migrate/seed:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-local.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-admin.ps1 -Publish
& .\artifacts\admin\win-x64\BotZone.Admin.WinForms.exe
```

Mặc định chạy Next dev; thêm `-Production` để build rồi chạy Next standalone. `-InstallDependencies` chạy lại `npm ci` và chỉ được dùng khi storefront đã dừng. Script khởi động API/Next ẩn, đợi health check và lưu log/PID/thời điểm tạo/executable trong `artifacts/local-runtime`. Nếu cổng đã có dịch vụ BotZone hợp lệ, script tái sử dụng và không nhận quyền dừng tiến trình đó. Cổng có dịch vụ khác sẽ báo lỗi, không tự kill.

Truy cập storefront tại [localhost:3000/vi](http://localhost:3000/vi), hộp thư tại [localhost:8025](http://localhost:8025). WinForms mặc định gọi `http://localhost:5080`; có ô sửa URL ở màn đăng nhập.

## Đặt hàng, hồ sơ khách hàng và hóa đơn

- Khách chưa đăng nhập bấm **Tiến hành thanh toán** sẽ được chuyển đến đăng nhập và quay lại checkout sau khi đăng nhập thành công. Truy cập trực tiếp checkout cũng áp dụng luồng này.
- Tài khoản cần xác minh email trước khi xác nhận đặt hàng. Checkout và trang tài khoản có nút **Gửi email xác minh** và **Tôi đã xác minh — kiểm tra lại**. Trong môi trường local, mở liên kết nhận được trong Mailpit tại cổng `8025`.
- Thông tin giao hàng, phương thức thanh toán, điểm và số dư đang nhập được lưu nháp trên trình duyệt theo từng tài khoản. Tải lại trang vẫn giữ thông tin; giá và tồn kho được tính lại khi rà soát đơn hàng. Nháp được xóa sau khi đặt hàng thành công.
- Checkout lấy địa chỉ đã lưu từ hồ sơ. Tùy chọn **Lưu địa chỉ giao hàng vào hồ sơ khách hàng** lưu địa chỉ cùng giao dịch đặt hàng, không tạo bản sao địa chỉ giống hệt; địa chỉ đầu tiên là mặc định. Số điện thoại còn trống trong hồ sơ được bổ sung, tên tài khoản hiện có được giữ nguyên.
- Hóa đơn được phát hành khi nhân viên kho bàn giao đơn hàng. Khách mở từ chi tiết đơn hàng hoặc **Tài khoản → Hóa đơn**, bấm **Tạo XLSX & PDF**, rồi tải định dạng muốn dùng. Đây là chứng từ bán hàng mô phỏng của đồ án.

Kiểm tra hồi quy luồng checkout trên dịch vụ local đang chạy:

```powershell
node tests/web/checkout-recovery-live.mjs
```

Bài kiểm tra tạo tài khoản và đơn demo, kiểm tra xác minh email, tải lại biểu mẫu, thử lại sau mất phản hồi mà không tạo trùng đơn, lưu hồ sơ và tải PDF/XLSX. Đơn còn chờ được hủy khi kết thúc; kết quả và ảnh chụp nằm trong `artifacts/web/`.

## Migration và seed có chủ đích

EF Core là cơ chế migration đang hoạt động. Source/migration Node cũ được giữ để tham chiếu và không thuộc runtime. Không chạy script cũ trong `legacy/`, `scripts/*.ts`, `src/server` hoặc `src/db` để thay đổi database.

Dừng API/worker trước khi migrate hoặc seed. Đặt `BOTZONE_MIGRATION_CONNECTION` trong phiên PowerShell bằng connection string của role sở hữu schema `botzone` và đúng database đích; không commit giá trị này. Sau đó:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-local.ps1 -Migrate -Seed
Remove-Item Env:BOTZONE_MIGRATION_CONNECTION
```

`-Migrate` gọi `scripts/migrate-ef.ps1`; `-Seed` gọi CLI demo seed của API .NET. Hai cờ độc lập và không chạy ngầm mỗi lần khởi động. Runtime dùng role `botzone_app`; bootstrap dùng `postgres` để tạo role nếu thiếu. Không đổi target database khi API vẫn đang chạy. Backup trước khi áp dụng thay đổi schema lên dữ liệu cần giữ.

Sau khi seed catalogue trên một database mới, nhập ảnh đã xác minh bằng backend C# (database hiện tại đã nhập đủ):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\import-product-photos.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\import-product-photos.ps1 -Apply
```

Lệnh đầu chỉ lập kế hoạch; lệnh sau cập nhật đường dẫn ảnh và audit, không seed lại hoặc thay schema. [Manifest ảnh](data/product-photos.json) có nguồn chính hãng cho 35 model/37 ảnh. Web phân trang 6 sản phẩm trên điện thoại và 12 trên laptop, dùng dropdown tùy biến có bàn phím và hiệu ứng hỗ trợ giảm chuyển động.

Landing dùng bốn GLB của bộ bàn giao **v003**, chọn mobile/desktop trước tải. Xem [tài liệu tích hợp 3D](docs/3d/v003/integration.md); tài liệu nguồn và hash giữ tại `docs/3d/v003/source/`. CMS WinForms có lựa chọn Pro Max/Duo v003. Script `node scripts/publish-landing-v003.mjs` lập kế hoạch qua API; thêm `--apply` để xuất bản local bằng quyền nội dung, từ chối nếu đang có bản nháp chưa xuất bản.

## Tài khoản demo

Sau khi seed demo thành công, mật khẩu các tài khoản dưới đây là `BotZoneDemo!2026`:

| Email | Vai trò |
| --- | --- |
| `owner@botzone.local` | Chủ cửa hàng; CMS chỉ đọc |
| `nv01@botzone.local`, `nv02@botzone.local` | Nhân viên xử lý đơn theo phạm vi được giao |
| `nv03@botzone.local` | Nhân viên mẫu inactive sau seed, không đăng nhập cho đến khi chủ cửa hàng kích hoạt |
| `warehouse@botzone.local` | Kho, nhận hàng, bàn giao và kiểm tra hàng |
| `content@botzone.local` | Nội dung, CMS, media và xuất bản landing |
| `customer@botzone.local` | Khách hàng trên storefront |

Đăng ký công khai tạo customer và gửi xác minh vào Mailpit. MoMo/Visa là adapter demo; COD giao thành công và COD thu tiền là hai sự kiện riêng. Tài khoản mẫu chỉ dùng cho môi trường local.

## Build và kiểm thử

```powershell
npm.cmd ci --no-audit --no-fund
npm.cmd run typecheck
npm.cmd run build
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-home'
dotnet build BotZone.slnx --configuration Release
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-admin.ps1 -Publish
```

`npm run build` sao chép public/static vào standalone; `npm start` chạy bản đó. `Dockerfile` là image storefront tùy chọn, không chứa backend/database/migration. Image/container chưa được kiểm thử; `host.docker.internal` còn cần được xử lý trong allowlist proxy và cách API bind host trước khi dùng. Đường chạy được kiểm tra ở đây là host Next + host API + Docker infrastructure.

Integration runner là console project `tests/BotZone.IntegrationTests`, cần database test được migrate/seed đúng và chỉ chấp nhận tên test theo guard trong source. Các script Playwright trong `tests/web` và smoke WinForms gọi API thật; một số chế độ tạo đơn/phiếu/chứng từ. Đọc hướng dẫn và chọn clone trước khi chạy, không chạy toàn bộ trên dữ liệu đang trình diễn.

WinForms đã được build/publish và chạy thực tế trên Windows ở DPI 125%, hai kích thước 1366×768 và 1920×1080. Evidence gồm danh sách/chi tiết hóa đơn, filter native, báo cáo/export, CMS, vai trò, mua hàng/nhận hàng và CRUD qua native form. Chi tiết, ảnh, giới hạn DPI/kiểm thử bàn phím nằm trong [docs/winforms.md](docs/winforms.md); không suy diễn các môi trường chưa chạy từ kết quả build.

## Dừng và bảo toàn dữ liệu

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\stop-local.ps1 -CheckOnly
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\stop-local.ps1
```

Script chỉ dừng API/Next có PID, thời điểm tạo và executable khớp state của workspace này, gồm các tiến trình con đã xác minh. Tiến trình chạy ngoài script được giữ nguyên. Thêm `-Containers` nếu muốn dừng cả db/Mailpit. Không script nào xóa volumes, database hoặc uploads.

Ngày 13/11/2026 đã kiểm tra thực tế `start` mặc định → WinForms đọc dữ liệu main → `stop` xác nhận hai cổng đóng → `start` lại. Trước/sau đều 8 hóa đơn phát hành, 97.130.000 VND; container DB/Mailpit và volumes giữ nguyên. Evidence: `artifacts/local-runtime/lifecycle-result.json`. Vòng này không dùng `-Migrate`, `-Seed`, `-Production` hoặc `-Containers`; các cờ chưa chạy trong vòng này không được coi là đã nghiệm thu vận hành.

Backup cần dừng mọi API/worker đang ghi và xác nhận đúng database đích. Giữ database chạy:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\backup-local.ps1 -WritersStopped -Database botzone
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\restore-test.ps1 -BackupPath 'C:\duong-dan\BotZone\backups\botzone-...'
```

Backup chứa custom PostgreSQL dump, uploads và SHA-256 manifest. Restore tạo database mới cùng thư mục media riêng, kiểm checksum, không ghi đè database gốc. Bản restore cần được kiểm tra thêm qua API/quyền/tải media; kết quả `pg_restore` riêng lẻ chưa chứng minh toàn bộ ứng dụng hoạt động. Backup và cấu hình local chứa dữ liệu riêng nên được gitignore.

## Chẩn đoán nhanh

| Hiện tượng | Kiểm tra |
| --- | --- |
| Docker named pipe chưa sẵn sàng | Mở Docker Desktop, chọn Linux containers, đợi `docker version` có Server |
| Cổng bận | Kiểm tra dịch vụ đang chạy; start/stop không tự kill tiến trình lạ |
| API chưa ready | Đọc `artifacts/local-runtime/api-*.stderr.log`, kiểm tra backend config và EF migration |
| Web báo API lỗi | Kiểm tra API `/health`, `BOTZONE_API_URL`, log web và cổng5080 |
| Không có thư xác minh | Kiểm tra Mailpit8025 và SMTP backend1025 |
| Script bị execution policy chặn | Dùng lệnh `powershell.exe -ExecutionPolicy Bypass -File` như trên, không cần đổi policy toàn máy |

Bản hướng dẫn/cấu hình cũ được lưu trong `legacy/v1-infrastructure` để đối chiếu lịch sử.
