# Môi trường local BotZone v1.1

Tài liệu đối chiếu mã nguồn và môi trường ngày 13/11/2026, múi giờ Asia/Saigon. Runtime gồm website Next.js, API ASP.NET Core có hosted worker, PostgreSQL và Mailpit; quản trị là ứng dụng C# Windows Forms. Tất cả chạy local. Kết quả nghiệp vụ/khôi phục được ghi riêng trong [implementation-status.md](implementation-status.md).

## Thành phần và địa chỉ

| Thành phần | Phiên bản / cách chạy | Địa chỉ local |
| --- | --- | --- |
| Website | Node 22.20.0, npm 10.9.3; Next 16.4.6, React 19.3.1 theo package-lock | `http://localhost:3000/vi`, `/en` |
| API + worker | .NET SDK 10.0.203 theo global.json; ASP.NET Core 10, EF Core 10.0.7, Npgsql EF 10.0.1 | `http://127.0.0.1:5080`; health `/health` |
| Admin | `src/BotZone.Admin.WinForms`, target `net10.0-windows`; .NET Windows Desktop runtime 10 | Cửa sổ native; URL API có thể đổi ở màn hình login |
| PostgreSQL | Docker image `postgres:18.1-bookworm` | `127.0.0.1:54329`, database chính `botzone` |
| Mailpit | Docker image `axllent/mailpit:v1.27.10` | SMTP `127.0.0.1:1025`; UI `http://localhost:8025` |
| Docker / shell | Docker Engine 28.4.0, Desktop 4.47.0, Compose 2.39.4-desktop.1; PowerShell 5.1 | Linux containers trên Docker Desktop |

Version là bộ đã dùng trong workspace, không phải chỉ dẫn nâng lên bản mới nhất. `global.json`, `packages.lock.json` của từng project và `package-lock.json` giữ dependency cụ thể. Container bind loopback. PostgreSQL cài trên host chỉ cung cấp công cụ `psql` cho kiểm tra; dữ liệu BotZone nằm trong container/volume Compose.

## Cấu hình và quyền

- `.env` của website lấy mẫu từ [.env.example](../.env.example): chỉ `BOTZONE_API_URL=http://127.0.0.1:5080` và `NEXT_TELEMETRY_DISABLED=1`. Next proxy `/api/*` về .NET, không có connection string PostgreSQL hoặc migration credential.
- `config/backend.local.json` là cấu hình local được ignore. Mẫu [backend.local.example.json](../config/backend.local.example.json) chứa `ConnectionStrings:BotZone`, `StorefrontUrl`, `DemoMode`, `Smtp:{Host,Port,From}`, `UPLOAD_DIR`. File đang có phải được giữ nguyên; chỉ sao chép mẫu khi chưa tồn tại.
- API nạp file backend rồi biến môi trường. Override .NET dùng `ConnectionStrings__BotZone`, `Smtp__Host`, `Smtp__Port`; `BOTZONE_ROOT` xác định thư mục dự án. `StorefrontUrl` phải trỏ website để link email verify/reset mở đúng origin.
- Runtime dùng `botzone_app`: không superuser, không createdb/createrole. Role `botzone` sở hữu schema và chạy migration; không dùng làm tài khoản runtime. `postgres` chỉ dành cho bootstrap/backup/restore local có kiểm soát.
- `BOTZONE_MIGRATION_CONNECTION` chỉ cấp cho thao tác EF design-time. Không ghi nó vào `.env` Next. Biến này không tự override connection runtime khi chạy API/seed; seed dùng `ConnectionStrings:BotZone` của tiến trình được gọi.
- ASP.NET Identity dùng cookie `BotZone.Session`, `auth_sessions` và anti-forgery `X-CSRF-TOKEN`; WinForms dùng CookieContainer của .NET API. Không có Better Auth runtime hoặc đăng nhập quản trị bằng localStorage.
- `DemoMode=true` bật thanh toán/hoàn mô phỏng, không gọi cổng thanh toán thật. Customer chỉ được chọn kết quả MoMo/Visa cho attempt của chính mình; COD/banking do nhân viên nghiệp vụ ghi nhận.

File `config/legacy.env` được giữ để bảo toàn cấu hình cũ và phục vụ kiểm chứng có chỉ định. Các tệp TypeScript server/DB lịch sử không phải runtime hay migration owner hiện tại.

## Khởi động và build

Chạy tại thư mục gốc workspace. Docker Desktop phải sẵn sàng. Lệnh khởi động/dừng chuẩn và các cờ được ghi trong [README](../README.md), [start-local.ps1](../scripts/start-local.ps1), [stop-local.ps1](../scripts/stop-local.ps1). Compose chỉ chạy `db`, `mailpit`, `bootstrap`; API/hosted worker và Next chạy trên host. Không cần chạy một worker Node riêng.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start-local.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/stop-local.ps1
```

Start mặc định development (`-Development` là cờ tường minh); `-Production` build/chạy Next standalone; `-InstallDependencies` cài lại lockfile khi web đã dừng. `-Migrate` chạy EF với connection owner; `-Seed` chạy `.NET --seed demo`; cả hai yêu cầu API/worker đã dừng. `-CheckOnly` chỉ kiểm công cụ/cổng/cấu hình/Compose, không khởi động hay sửa DB. Script giữ cấu hình đang có, ghi log và danh tính tiến trình tại `artifacts/local-runtime`, chạy helper hidden. Phiên đã mở ngoài script chỉ được tái sử dụng sau health check, không nhận quyền dừng nó.

Stop mặc định chỉ dừng PID có workspace, thời điểm tạo và executable khớp record, cùng các tiến trình con đã xác minh. `-Containers` dừng thêm db/mailpit; vẫn giữ volumes. `-CheckOnly` in kế hoạch dừng, không thực hiện. Những cờ này được đối chiếu source; kết quả live startup/stop được ghi riêng trong báo cáo vận hành.

Các lệnh thành phần sau hữu ích khi chạy thủ công; không mở thêm tiến trình nếu cổng đã có phiên đang được quản lý:

```powershell
docker compose up -d db mailpit bootstrap
dotnet restore src/BotZone.Api --locked-mode
dotnet run --project src/BotZone.Api --no-launch-profile --urls http://127.0.0.1:5080
```

Ở terminal website:

```powershell
npm.cmd ci --no-audit --no-fund
npm.cmd run dev
```

Build website production dùng `npm.cmd run build`; postbuild sao chép `public` và `.next/static` vào `.next/standalone`; `npm.cmd start` chạy standalone server. API cần hoạt động độc lập ở `BOTZONE_API_URL`.

Build/publish admin:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-admin.ps1 -Publish
```

Output `artifacts/admin/win-x64/BotZone.Admin.WinForms.exe`. Bản framework-dependent cần .NET Windows Desktop runtime 10; thêm `-SelfContained` nếu cần kèm runtime. `-Smoke -ApiUrl http://127.0.0.1:5080` chạy native smoke có thao tác API, vì vậy chỉ dùng trên database kiểm thử được chỉ định. LoginForm đọc `BOTZONE_API_URL` nếu có, mặc định `http://localhost:5080`.

## EF migrations và seed

EF Core là migration owner duy nhất. Migration đầu tiên `AdoptLegacyAndIdentity` nhận schema lịch sử có kiểm tra baseline/fingerprint, không chạy một InitialCreate mù lên database đã có dữ liệu. `schema_migrations` giữ lịch sử SQL đã nhận; `__EFMigrationsHistory` là sổ migration hiện hành. Không chạy song song migration runner TypeScript.

Dừng mọi writer, đặt connection owner trong biến môi trường của riêng terminal, rồi chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/migrate-ef.ps1
```

Script yêu cầu `BOTZONE_MIGRATION_CONNECTION`; `dotnet ef database update` dùng `BotZone.Infrastructure`. Năm migration trong snapshot clone:

1. `20261113070723_AdoptLegacyAndIdentity`
2. `20261113072619_IntegrityRepairAndPreview`
3. `20261113073844_WarrantyQuantityAndLedgerSources`
4. `20261113075041_PolicySnapshotsAndCustody`
5. `20261113080440_IdentityNamesAndFinancialHistory`

Seed là thao tác tường minh của .NET API, không chạy mỗi lần khởi động. `dotnet run --project src/BotZone.Api -- --seed demo` dùng connection được cấp cho tiến trình đó, tạo dataset demo/catalogue/landing rồi thoát trước khi server/worker bắt đầu. Profile `catalogue` và `golden` phục vụ tập dữ liệu có chủ đích. Dùng đúng database đích, quyền seed được cấp và runbook bàn giao; không đổi credential runtime dài hạn sang owner. Dữ liệu demo không phải dữ liệu thương mại thật.

## File local, email và lịch chạy nền

| Dữ liệu | Vị trí / quy tắc |
| --- | --- |
| File upload public đã validate | `uploads/catalogue-public`, hoặc dưới `UPLOAD_DIR` đã cấu hình |
| Bằng chứng riêng tư | `uploads/evidence-private`; truy cập qua API có scope, không phục vụ bằng thư mục static |
| Hóa đơn xuất | `uploads/export-private`; manifest/checksum trong DB, download kiểm quyền và hạn |
| Data Protection keys | `uploads/data-protection` dưới BOTZONE_ROOT; vị trí này không đổi theo UPLOAD_DIR |
| Assets và font nguồn | `public/media`, `public/models`, `public/fonts/NotoSans-Regular.ttf`; license tại asset manifest/font OFL |
| Dữ liệu PostgreSQL / email | Named volumes `postgres-data`, `mailpit-data` của Compose |
| Backup local | `backups/`; dump, uploads archive và manifest checksum cùng một lần sao lưu |

PDF dùng font NotoSans local có glyph tiếng Việt; `PDF_FONT_PATH` có thể override đường dẫn font trong backend. Không cần tải font từ CDN lúc render/export. File nguồn và license hiện có được giữ nguyên; công việc Blender/model mới đã chuyển sang phiên Codex riêng theo yêu cầu người dùng.

`LocalWorker` chạy trong API mỗi khoảng 10 giây: hết hạn đơn/giữ hàng, settle điểm theo policy snapshot/cửa sổ trả, hết hạn quote thu cũ và gửi outbox. Outbox có lease, `FOR UPDATE SKIP LOCKED`, retry tối đa5 lần với backoff; Mailpit nhận email verify/reset local. Dừng API đồng thời dừng worker này.

## Kiểm chứng, backup và tái tạo tài liệu

Health và catalogue là kiểm tra chỉ đọc:

```powershell
Invoke-RestMethod http://127.0.0.1:5080/health
Invoke-RestMethod 'http://localhost:3000/api/catalogue?locale=vi'
Invoke-RestMethod http://localhost:8025/api/v1/info
```

`status=ready` chỉ xác minh kết nối DB, không thay cho kiểm thử nghiệp vụ. `npm.cmd run typecheck` kiểm TypeScript; `npm.cmd run test:e2e` chạy web smoke; các harness C# và native có bằng chứng riêng trong `tests/BotZone.IntegrationTests` và `artifacts/winforms`. Các test có ghi phải chạy trên clone, không mặc định main.

Backup nhất quán cần dừng tất cả writer, gồm API/worker và công cụ ghi trực tiếp. [backup-local.ps1](../scripts/backup-local.ps1) yêu cầu `-WritersStopped`; [restore-test.ps1](../scripts/restore-test.ps1) kiểm checksum và khôi phục vào database mới, không ghi đè `botzone`. Giữ DB dump cùng uploads, Data Protection keys và manifest. Kết quả restore thực được ghi trong báo cáo bàn giao; chỉ parse script không được tính là đã khôi phục thành công.

Schema bàn giao được chụp từ `botzone_baseline_test_20261113135743_c0d401c6` lúc `2026-11-13T08:20:29.145581Z`, sau năm migration EF: 100 bảng,649 cột,182 FK,218 indexes. Lệnh tái tạo chỉ đọc:

```powershell
python docs/database/capture-schema.py --database botzone_baseline_test_20261113135743_c0d401c6 --owner-env config/legacy.env
python docs/database/capture-schema.py --from-snapshot
```

Owner chỉ được dùng để đọc metadata/history trong transaction `REPEATABLE READ READ ONLY`, `default_transaction_read_only=on`, kết thúc bằng ROLLBACK. Không export dòng khách/đơn hoặc credential. [ERD SVG](database/erd.svg), [data dictionary](database/data-dictionary.md) và [schema snapshot](database/schema-snapshot.json) có đủ nguồn và hash tái lập. Trạng thái main sau migration/seed/backup phải đối chiếu báo cáo của lần bàn giao, không suy ra từ tên database trong tài liệu clone.
