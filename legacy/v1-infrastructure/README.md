# BotZone

Đồ án cửa hàng Apple chạy local theo [BA v1.0](BotZone_BA_v1.0.md) và [yêu cầu đổi kiến trúc 13/11/2026](docs/architecture-change-2026-11-13.md). Storefront Việt/Anh là web; **toàn bộ quản trị tiếng Việt phải dùng C# Windows Forms**. Thanh toán/vận chuyển mô phỏng; tiền nghiệp vụ VND nguyên.

**Điểm dừng hiện tại:** dừng admin web, giữ source/data. DB/Mailpit và migration 0001/0002 đã chạy; chưa có seed/worker, API nghiệp vụ chưa nối đầy đủ, storefront còn lỗi typecheck. Chưa tạo WinForms, backend mới hoặc bộ migration thứ hai. Hướng dẫn chạy toàn bộ/seed bên dưới là bản nháp đích bàn giao, chưa xác nhận demo chạy được.

Xem [trạng thái triển khai và bằng chứng kiểm thử](docs/implementation-status.md) trước khi nghiệm thu. Việc có mã nguồn hoặc kịch bản bên dưới không đồng nghĩa mọi tiêu chí BA đã đạt.

## Yêu cầu máy

- Windows 10/11, PowerShell 5.1 trở lên, Docker Desktop ở chế độ Linux containers.
- Node.js 22.20 trở lên thuộc nhánh hỗ trợ, npm và Git nếu phát triển trên host.
- Cổng local: ứng dụng `3000`, PostgreSQL `54329`, SMTP `1025`, Mailpit `8025`.
- Lần đầu cần Internet để tải npm packages, Docker images và tài nguyên. Sau khi chuẩn bị, ứng dụng không gọi dịch vụ thanh toán/cloud để xử lý demo.

Docker Compose dùng PostgreSQL `18.1-bookworm`, Mailpit `v1.27.10` và Node `22.20.0-bookworm-slim`. Cổng mặc định chỉ bind `127.0.0.1`; database và hộp thư không mở ra LAN. PostgreSQL 18 lưu volume tại `/var/lib/postgresql`.

## Chạy trên Windows / PowerShell

Mở PowerShell tại thư mục có `compose.yaml`, khởi động Docker Desktop và chờ engine sẵn sàng. Không dùng cú pháp `&&` của PowerShell 7 trong PowerShell 5.1. Dùng `npm.cmd` để tránh lỗi policy khi gọi `npm.ps1`.

Lần đầu chạy toàn bộ bằng Docker:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-local.ps1 -Seed
```

Script chỉ sao chép `.env.example` khi chưa có `.env`. Thứ tự chạy là database healthy → bootstrap role → migration thành công → app/worker. Seed chỉ chạy khi truyền `-Seed`, không chạy lại ngầm mỗi lần restart.

Mở:

- Storefront: [http://localhost:3000](http://localhost:3000)
- Quản trị: ứng dụng C# Windows Forms theo yêu cầu mới; chưa có chương trình để khởi động. Không tiếp tục triển khai `/admin` web.
- Hộp thư thử nghiệm: [http://localhost:8025](http://localhost:8025)

Chạy phát triển để sửa mã và hot reload:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-local.ps1 -Development -Seed
npm.cmd run dev
```

Mở terminal thứ hai tại cùng thư mục:

```powershell
npm.cmd run worker
```

Worker xử lý tác vụ local theo code hiện hành. Khi phát triển trên host, không chạy thêm app/worker Docker cùng lúc để tránh trùng cổng hoặc nhầm tiến trình. Dừng chúng trước bằng `docker compose stop app worker` nếu đã chạy chế độ Docker.

Các lệnh tương đương khi cần chạy từng bước:

```powershell
Copy-Item -LiteralPath .env.example -Destination .env # Chỉ làm khi chưa có .env
docker compose up -d db mailpit bootstrap
docker compose wait bootstrap
npm.cmd ci --no-audit --no-fund
npm.cmd run db:migrate
npm.cmd run db:seed
npm.cmd run dev
```

Sau mỗi lệnh, chỉ tiếp tục nếu không có lỗi. Giữ `package-lock.json`; dùng `npm ci` để cài đúng phiên bản đã khóa.

## Biến môi trường và quyền database

Mẫu cấu hình nằm trong `.env.example`:

| Biến | Dùng cho |
| --- | --- |
| `DATABASE_URL` | Runtime, role `botzone_app` chỉ có quyền DML cần thiết, không superuser/DDL |
| `MIGRATION_DATABASE_URL` | Role `botzone` sở hữu schema, chạy migrations/seed được phép |
| `BETTER_AUTH_URL` | URL local thống nhất với origin mở trong trình duyệt |
| `BETTER_AUTH_SECRET` | Khóa phiên local; đổi khỏi giá trị mẫu khi dùng dữ liệu cá nhân |
| `DEMO_MODE` | Bật các adapter thanh toán/vận chuyển mô phỏng có kiểm quyền |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_FROM` | Mailpit local, không gửi email Internet |
| `UPLOAD_DIR` | Media công khai, bằng chứng riêng và file xuất trên máy |

Trên host, database ở `127.0.0.1:54329`, Mailpit ở `127.0.0.1:1025`. Trong Docker, Compose đổi host thành `db:5432` và `mailpit:1025`. Không dùng địa chỉ host trong container. Role `postgres` và mật khẩu `botzone_admin_local` chỉ phục vụ bootstrap/sao lưu local; không dùng cho runtime.

Các tài khoản/mật khẩu mẫu chỉ dành cho đồ án local. Không dùng lại mật khẩu cá nhân và không commit `.env`, uploads riêng hoặc backup.

## Tài khoản demo

Danh sách dự kiến, **chưa seed**: DB hiện có 0 tài khoản. Mật khẩu demo dự kiến: `BotZoneDemo!2026`.

| Email | Vai trò |
| --- | --- |
| `owner@botzone.local` | Chủ cửa hàng; xem landing, không được sửa/xuất bản/khôi phục |
| `order1@botzone.local` | Nhân viên xử lý đơn NV01 |
| `order2@botzone.local` | Nhân viên xử lý đơn NV02 |
| `order3@botzone.local` | Nhân viên xử lý đơn NV03 |
| `warehouse@botzone.local` | Nhân viên kho |
| `content@botzone.local` | Nhân viên nội dung; được biên tập/xuất bản/khôi phục landing |
| `customer@botzone.local` | Khách hàng demo |

Yêu cầu thiết kế: signup công khai luôn là customer; đăng ký mới xác minh qua Mailpit. Luồng này chưa chạy kiểm chứng. MoMo/Visa demo không nhận số thẻ/CVV; banking không hiển thị tài khoản nhận tiền thật. COD đã giao và COD đã thu là hai sự kiện riêng.

## Migration, seed và kiểm thử

```powershell
npm.cmd run db:migrate
npm.cmd run db:seed
npm.cmd run typecheck
npm.cmd run test:db
npm.cmd run build
```

Migrations có version và không tự reset dữ liệu. Seed demo phải chạy lặp an toàn. Fixture golden tách riêng với `npm.cmd run db:seed:golden`; đọc hướng dẫn/script thực tế và dùng database test đã cấu hình trước khi chạy. Không dùng golden hoặc lệnh reset để ghi đè database nhóm đang demo.

Đối chiếu golden: 8 hóa đơn phát hành, tổng `97.130.000`, chứng từ giảm `3.500.000`, ròng `93.630.000` VND. Draft 99 triệu không thuộc số liệu phát hành. File PDF/XLSX phải sinh từ PostgreSQL và dùng cùng dataset/filter; không dùng file mẫu tĩnh để chứng minh nghiệp vụ đã chạy.

Trạng thái từng tiêu chí AC/DB-T, kết quả thực tế và phần chưa kiểm chứng được ghi tại `docs/implementation-status.md`. Hướng dẫn nghiệp vụ trình bày tại [kịch bản demo](docs/demo-script.md).

## Dừng, chạy lại và bảo toàn dữ liệu

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\stop-local.ps1
docker compose up -d
docker compose logs --tail 100 app worker migrate
```

`stop` giữ nguyên database, thư và uploads. `docker compose down` cũng giữ named volumes nếu không thêm `--volumes`. Không chạy `down -v`, xóa volume hoặc xóa thư mục uploads để “sửa lỗi”. Không có reset tự động khi khởi động.

## Backup / restore trên database test mới

Backup cần DB và media ở cùng trạng thái. Dừng app/worker Docker và mọi `npm run dev`, `npm run start`, `npm run worker` đang chạy trên host; để database tiếp tục chạy.

```powershell
docker compose stop app worker
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\backup-local.ps1 -WritersStopped
```

Script tạo thư mục `backups/botzone-...` chứa custom-format `database.dump`, `uploads.zip` nếu có media và `manifest.json` với SHA-256. Binary dump được chuyển bằng `docker cp`, không qua toán tử `>` hoặc text pipeline của PowerShell 5.1.

Khôi phục từ đường dẫn đầy đủ mà lệnh backup đã in:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\restore-test.ps1 -BackupPath 'C:\duong-dan\BotZone\backups\botzone-...'
```

Restore chỉ nhận backup bên trong workspace này, kiểm checksum và tạo database **mới** tên `botzone_restore_...`; không drop/ghi đè database gốc. Media được giải nén vào `backups/restore-.../uploads`. Lệnh trả URL database và đường dẫn uploads để kiểm thử riêng. Khi kiểm thử app restored, dùng cả hai đường dẫn này, rồi kiểm tổng hóa đơn, constraints, tải file riêng tư và quyền; chỉ thành công của `pg_restore` chưa đủ để đánh dấu DB-T20 đạt.

Chạy lại môi trường chính sau backup:

```powershell
docker compose up -d app worker
```

Backup chứa thông tin cá nhân/token/media riêng; giữ local, không đưa lên repository.

## Lỗi thường gặp

| Hiện tượng | Cách xử lý |
| --- | --- |
| Không tìm thấy Docker named pipe | Mở Docker Desktop, chọn Linux containers, chờ `docker version` có Server |
| Docker/WSL Access denied trong công cụ tự động | Chạy lệnh được phê duyệt ngoài sandbox; không đổi ACL hoặc tắt bảo vệ máy |
| Cổng 3000 bị chiếm | Dừng app host hoặc app Docker cũ; kiểm `Get-NetTCPConnection -LocalPort 3000` |
| Authentication URL/origin mismatch | Dùng nhất quán `http://localhost:3000`, đối chiếu `BETTER_AUTH_URL` |
| DB role không tồn tại | Chạy `docker compose up -d bootstrap` và xem `docker compose logs bootstrap` |
| Connection refused 54329 | Kiểm `docker compose ps`, chờ db healthy; host dùng 54329, container dùng 5432 |
| Không thấy thư xác minh | Kiểm Mailpit port8025, SMTP host đúng môi trường và log app/worker |
| npm.ps1 bị chặn | Gọi `npm.cmd`; không cần thay policy toàn máy |
| Build/download cần Internet | Chuẩn bị dependencies/images trước buổi demo, kiểm offline sau đó |

Blender/MCP chỉ được coi là kết nối khi có kiểm tra thực tế. [Ghi nhận môi trường](docs/environment.md) nêu kết quả đã quan sát, không suy diễn từ việc máy từng cài plugin.
