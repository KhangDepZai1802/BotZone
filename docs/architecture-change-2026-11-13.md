> **Hồ sơ lịch sử tại điểm tạm dừng.** Lệnh tiếp tục theo BA v1.1 đã thay trạng thái này; xem [hồ sơ chuyển đổi đã triển khai](architecture-change-winforms.md) và [trạng thái hiện tại](implementation-status.md).

# Chuyển quản trị BotZone sang C# Windows Forms

Ngày 13/11/2026. Trạng thái: **ghi nhận yêu cầu, bảo toàn hiện trạng; chưa triển khai kiến trúc mới**.

## Yêu cầu bắt buộc mới nhất

> Website bán hàng vẫn là web, nhưng toàn bộ giao diện quản trị phải là ứng dụng C# Windows Forms. Tạm dừng phát triển giao diện admin web. Giữ nguyên mã và dữ liệu hiện có, không reset hoặc xóa dự án. Ghi lại hiện trạng để chuẩn bị chuyển kiến trúc theo BA cập nhật; chưa tự dựng thêm backend hoặc bộ migration thứ hai.

Yêu cầu này thay phần kiến trúc/giao diện admin web của BA v1.0. File BA gốc được giữ nguyên; các trang `/admin` trong BA cũ không còn là đầu ra UI cần triển khai.

## Ranh giới sau cập nhật

| Phần | Yêu cầu | Hiện trạng |
| --- | --- | --- |
| Khách mua hàng | Storefront web | Giữ Next.js/TypeScript đang làm |
| Toàn bộ quản trị | C# Windows Forms | Chưa tạo .sln/.csproj/Form; đã dừng admin web |
| Auth/nghiệp vụ/query/export | Cần hợp đồng dùng chung cho web và desktop | Giữ service TypeScript/Better Auth dang dở để đánh giá tái sử dụng |
| PostgreSQL | Giữ local Docker | Hai migration SQL đã áp dụng; không có database/migration stack thay thế |
| Media/Mailpit/demo payment | Giữ yêu cầu local | Font/SVG/Mailpit có sẵn; nghiệp vụ chưa nghiệm thu |

Quản trị WinForms phải bao gồm tổng quan, catalogue/giá/nội dung VI–EN/media, kho/serial/nhập hàng, đơn/giao nhận, hóa đơn/tìm kiếm, thanh toán/hoàn, trả hàng, thu cũ/credit, Care, khách/điểm/hạng, khuyến mãi, CMS landing, báo cáo/XLSX/PDF, nhân viên/quyền, audit/cấu hình. Storefront vẫn hiển thị landing; màn biên tập/duyệt thuộc WinForms.

Giữ bốn vai trò và ngoại lệ CMS: chỉ content sửa/publish/restore; owner chỉ xem/giám sát landing. Quyền phải kiểm ở server, kể cả khi client là desktop.

## Phương án chuẩn bị để đối chiếu BA cập nhật

Đề xuất tái sử dụng dịch vụ/HTTP API hiện có cho web và WinForms, với một PostgreSQL và một chuỗi migrations. Đây mới là phương án đối chiếu BA cập nhật, chưa phải quyết định tạo hoặc thay backend.

Trước khi viết WinForms cần thống nhất:

- Backend sở hữu nghiệp vụ và DTO dùng chung; VND trao đổi JSON bằng chuỗi số nguyên.
- Đăng nhập desktop, lưu/thu hồi session, role thay đổi và tải file riêng qua API. Không đưa credentials PostgreSQL vào client WinForms.
- Query AST, pagination, lỗi và idempotency keys giữa hai client. `/api/admin/...` là namespace API, không bắt buộc có trang web `/admin`.
- Editor landing/preview 3D trong WinForms theo BA mới; chưa tự chọn phương án nhúng web hoặc xây viewer desktop.
- Startup local và test liên ứng dụng web/WinForms/dịch vụ dùng chung.

Lượt ghi nhận này không scaffold WinForms, không di chuyển source và không thêm backend/migration framework.

## Bảo toàn

Giữ source, lockfile, SQL migrations, Docker volumes, `.env` local và assets. Không reset/xóa, không sửa BA gốc, không commit/push/deploy. Chưa có thư mục admin web; CSS/hợp đồng dự kiến cũ được giữ để đối chiếu.

- [Hiện trạng và bằng chứng](implementation-status.md).
- [Bản kê SHA-256 nguồn](checkpoints/2026-11-13.json), không chứa secrets/cache/DB volume và không thay cho backup.

Đã quan sát SDK .NET 10.0.203 và Windows Desktop runtime 10.0.7. Chưa chọn target framework hoặc build WinForms.
