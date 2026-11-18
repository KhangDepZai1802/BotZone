# Báo cáo kiểm bộ thiết kế

**Ngày:** 17/11/2026. Báo cáo này nói về tài liệu và mẫu giao diện được bàn giao, không phải kết quả kiểm thử ứng dụng BotZone hiện tại.

## 1. Đã kiểm

| Hạng mục | Kết quả |
|---|---|
| Mẫu hình ảnh | 14 PNG mở/giải mã hợp lệ, 14 SVG XML hợp lệ; metadata có 14 ID duy nhất |
| Độ phân giải | PNG xuất 1.5× canvas; 13 mẫu 2160×1350, mẫu compact 1602×876 |
| Sách thiết kế | 19 trang PDF; có text tiếng Việt có thể trích xuất; đã render và kiểm bố cục các trang đại diện |
| Text fit | Không phát hiện vượt chiều rộng ở các vùng có khai báo giới hạn trong trình dựng; kết hợp kiểm trực quan các mẫu |
| Trình xem HTML | 14 ảnh nhúng trực tiếp, không phụ thuộc script/stylesheet mạng; JavaScript qua kiểm tra cú pháp Node |
| Liên kết giữa mẫu | 19 hotspot có đích tồn tại và tọa độ nằm trong canvas |
| Độ phủ ID | 55/55 ID nghiệp vụ trong inventory và 14/14 visual ID được map ở file06; 12 nhóm bằng chứng EV được đối chiếu |
| Phạm vi | Đăng nhập và bốn vai trò; laptop/PC Windows; không có admin mobile |
| Phân quyền nhạy cảm | Spec giữ chủ chỉ xem CMS; content sửa/publish theo quyền và không sửa tiền/kho; kho không xem/xuất hóa đơn tài chính |
| Nhất quán giao diện | Sửa lại nhãn COD, nhóm OR, tên nhân viên theo role, mẫu sản phẩm/media, bảng nhận một phần, đơn vị/nhãn tổng và kích thước token |

Rà soát nội dung độc lập theo các nhóm vận hành ERP, nội dung/CMS và khả thi WinForms đã được dùng để sửa lỗi trước khi đóng gói. Toàn bộ con số trên mockup là minh họa, không phải kết quả đọc database.

## 2. Kiểm cặp màu chính

Tỷ lệ dưới đây được tính theo relative luminance của các mã màu. Đây là kiểm token, chưa phải tuyên bố ứng dụng hoặc tài liệu đã đạt toàn bộ tiêu chuẩn accessibility.

| Cặp | Tỷ lệ tương phản |
|---|---:|
| Chữ chính `#202126` / trắng | 16.07:1 |
| Chữ phụ `#63656D` / trắng | 5.81:1 |
| Trắng / nút burgundy `#6F233F` | 10.56:1 |
| Thành công / nền xanh nhẹ | 5.64:1 |
| Cảnh báo / nền vàng nhẹ | 5.73:1 |
| Lỗi / nền đỏ nhẹ | 6.13:1 |
| Thông tin / nền xanh dương nhẹ | 5.85:1 |
| Chữ phụ sidebar / graphite | 9.08:1 |
| Viền input `#8A8C95` / trắng | 3.35:1 |
| Viền focus `#2459B8` / trắng | 6.57:1 |
| Cột biểu đồ phụ `#A58B97` / trắng | 3.12:1 |
| Thanh biểu đồ phụ `#9E7F8C` / nền `#F4F2F3` | 3.21:1 |

Đường phân cách surface và trạng thái disabled không được dùng thay tín hiệu focus hoặc thông tin duy nhất. Khi thay toolkit/font, kiểm lại diện mạo và hành vi thực tế.

## 3. Chưa kiểm và phải kiểm khi triển khai

- Chưa xem mã nguồn WinForms local hoặc chạy executable trên máy người dùng; chưa có ảnh native trước–sau.
- HTML được kiểm cấu trúc/dữ liệu/cú pháp, chưa chạy kiểm tương tác trong trình duyệt ở lượt này. Nó chỉ là trình xem mẫu, không thay app native.
- Chưa kiểm Windows DPI, resize, Narrator, bàn phím, API chậm, lỗi kết nối hoặc lifecycle control của ứng dụng thật.
- Chưa kiểm dữ liệu DB, tính tiền, quyền server, hóa đơn, thống kê hay file Excel/PDF được ứng dụng xuất ra. Các tiêu chí này nằm trong file04 và prompt, không đánh dấu đạt dựa vào mockup.
- Chưa xác nhận phiên bản .NET/toolkit hiện tại của repo local. Codex phải đọc `.csproj` và dependency trước khi chọn hoặc cập nhật control.

Baseline Windows cần thực hiện: 1366×768 ở 100% và 125%; 1920×1080 ở 125% và 150%. Kiểm thêm cấu hình khác khi có phần cứng hoặc rủi ro liên quan. Màn/chức năng chưa có ảnh riêng vẫn phải được triển khai theo BA và pattern; không dùng số lượng ảnh để cắt phạm vi.
