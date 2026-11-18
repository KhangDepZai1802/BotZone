# Kiểm kê và sao lưu trước chuyển EF Core

Thực hiện ngày **13/11/2026**, khoảng **13:56–13:57 Asia/Saigon**, sau khi đọc toàn bộ `BotZone_BA_v1.1_WinForms.md`. Đây là ảnh chụp hiện trạng trước EF Core, không phải xác nhận nghiệm thu nghiệp vụ hoặc DB-T20.

## Kết luận đã kiểm chứng

- PostgreSQL Docker đang chạy `18.1 (Debian 18.1-1.pgdg12+2)`; database gốc `botzone`, cổng host `127.0.0.1:54329`.
- Có **90 base tables** trong schema `public`. Đã chạy `COUNT(*)` cho **từng bảng**, không dựa vào thống kê ước tính.
- **89 bảng nghiệp vụ/auth đều có 0 dòng**; riêng `schema_migrations` có 2 dòng. `user`, `account`, `session`, `verification`, `staff_profiles` và `customer_profiles` đều bằng 0. Tại thời điểm kiểm kê không có tài khoản thực hoặc phiên cần chuyển.
- Hai migration lịch sử đã áp dụng, checksum trong DB trùng SHA-256 tệp hiện tại. Chưa có migration EF hoặc migration SQL thứ ba được chạy trong lượt kiểm kê này.
- Không thấy client khác trong `pg_stat_activity` của database `botzone`; không thấy tiến trình host `node`/`dotnet`; Compose chỉ có `db`, `mailpit` chạy và `bootstrap` đã thoát thành công. Không có app/worker container đang ghi dữ liệu.
- Thư mục `uploads` chưa tồn tại. Không có media/private export local để đưa vào bản sao; không tạo dữ liệu media giả để đánh dấu kiểm thử đạt.
- Đã backup và restore vào **database mới**, đối chiếu toàn bộ số dòng/schema thành công. Không drop/reset/ghi đè database hoặc volume gốc.

Danh sách đếm đầy đủ: [inventory-before-ef-counts.txt](inventory-before-ef-counts.txt). Schema thực xuất bằng `pg_dump --schema-only`: [current-schema.sql](current-schema.sql). Không xuất giá trị mật khẩu, token, session hoặc thông tin cá nhân ra tài liệu này.

## Migration lịch sử

| Tệp | SHA-256 khớp DB | Applied at UTC |
| --- | --- | --- |
| `0001_core.sql` | `a22cc34f8aa4415965825b95e7788459e7d8d225066be04013a743e96316e5a8` | `2026-11-13 06:36:57.651906+00` |
| `0002_guards.sql` | `937c733b762cda5e63ba6c45c98eece383882f174144ffc28f92fc9ffc03a282` | `2026-11-13 06:36:58.455874+00` |

SHA-256 của schema dump gốc:

```text
3E253262DC5719C18E236A5D19810EB2F63004F6EF73BCE9DD57349948427AD3
```

`pg_dump`18 tạo khóa `\restrict`/`\unrestrict` ngẫu nhiên ở mỗi lượt. Khi so schema gốc với bản restore, chỉ loại hai dòng khóa này rồi đối chiếu toàn bộ văn bản còn lại; kết quả giống nhau. Không bỏ qua DDL, constraints, trigger hoặc grants khi so sánh.

## Backup và bản sao kiểm thử baseline

Backup local, đã nằm trong thư mục được `.gitignore` loại trừ:

```text
C:\Users\khang\OneDrive\Documents\BotZone\backups\botzone-20261113-135647-8e85499a
```

Tệp `database.dump` là custom-format PostgreSQL dump. SHA-256:

```text
A0DCACC66C885A5D9B46CF860AA78C1BF53E62874243F40B47F317067B944208
```

`manifest.json` ghi `createdAt=2026-11-13T06:56:48.1545253Z`, `writersStopped=true`, media rỗng. Không có `uploads.zip` vì thư mục nguồn chưa tồn tại. `baseline-restore.json` trong cùng thư mục ghi metadata bản restore. Backup là bản giữ lịch sử, không phải dữ liệu để copy password hash/session sang ASP.NET Core Identity.

Database dành cho người phụ trách EF thử baseline:

```text
botzone_baseline_test_20261113135743_c0d401c6
```

Cùng PostgreSQL host/port và các role local hiện có: `botzone` sở hữu schema, `botzone_app` là runtime. Lấy thông tin kết nối từ cấu hình local, chỉ thay tên database khi kiểm thử. Không đặt connection string vào WinForms/storefront.

Thư mục restore riêng:

```text
C:\Users\khang\OneDrive\Documents\BotZone\backups\restore-20261113135743_c0d401c6
```

Thư mục con `uploads` cũng chưa tồn tại, tương ứng nguồn chưa có media.

## Đối chiếu sau restore

| Thuộc tính | Gốc | Bản restore |
| --- | ---: | ---: |
| Base tables | 90 | 90 |
| Tổng dòng nghiệp vụ/auth | 0 | 0 |
| Dòng `schema_migrations` | 2 | 2 |
| Constraints trong `public`, gồm NOT NULL của PostgreSQL18 | 986 | 986 |
| Indexes | 190 | 190 |
| Trigger ứng dụng, không tính trigger nội bộ FK | 33 | 33 |
| Functions trong `public`, gồm extension pgcrypto | 51 | 51 |
| Sequences | 1 | 1 |

Mọi tên bảng và số dòng khớp. Schema text sau chuẩn hóa khóa `pg_dump` khớp. Lượt này không chạy lệnh thay đổi tài khoản, không chuyển hash/session, không thay FK/ID và không tạo baseline/migration thay cho người phụ trách schema.

## Lệnh đã thực thi

```powershell
docker compose ps --all
docker compose exec -T db psql -U postgres -d botzone -At -c "SELECT current_database(),current_setting('server_version');"
docker compose exec -T db pg_dump -U postgres -d botzone --schema-only -f /tmp/botzone-before-ef-schema.sql
docker cp botzone-db-1:/tmp/botzone-before-ef-schema.sql docs/database/current-schema.sql
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\backup-local.ps1 -WritersStopped
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\restore-test.ps1 -BackupPath 'C:\Users\khang\OneDrive\Documents\BotZone\backups\botzone-20261113-135647-8e85499a' -DatabasePrefix botzone_baseline_test
```

Query đếm được chạy trong `BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY`:

```sql
SELECT table_schema, table_name,
       (xpath('/row/n/text()', query_to_xml(
           format('SELECT count(*) AS n FROM %I.%I', table_schema, table_name),
           false, true, '')))[1]::text::bigint AS row_count
FROM information_schema.tables
WHERE table_type = 'BASE TABLE'
  AND table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY table_schema, table_name;
```

Tên bảng chỉ lấy từ catalog và quote bằng `%I`. Không đọc hoặc in nội dung bản ghi. `Compare-Object` đối chiếu kết quả gốc/restore không có chênh lệch.

## Giới hạn và bước tiếp theo

Người phụ trách schema cần kiểm baseline/migration EF trên bản sao trên, rồi đối chiếu lại schema/IDs/FK/roles trước khi đổi nguồn ghi database chính. Không chạy `InitialCreate` trực tiếp lên các bảng đã tồn tại và không đánh dấu migration chưa thực thi là đã chạy.

DB-T20 chưa hoàn tất: chưa có hóa đơn/golden/media/file xuất hoặc phiên riêng tư để kiểm sau restore. Việc restore cấu trúc rỗng thành công chỉ chứng minh quy trình sao lưu và giữ schema hiện tại; phải lặp nghiệm thu với dữ liệu nghiệp vụ sau này.
