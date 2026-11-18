# BotZone Admin — trải nghiệm thao tác, vòng 5

Vòng này tiếp tục thiết kế hiện có theo phản hồi ngày 18/11/2026. Giữ ứng dụng WinForms, logo chính thức, sidebar sáng, API, quyền tài khoản và các lệnh nghiệp vụ. Không đổi dữ liệu hoặc công thức tài chính để tạo biểu đồ.

## Các luồng đã chỉnh

| Màn hình | Cách dùng hiện tại |
| --- | --- |
| Cài đặt cửa hàng | Bốn khối riêng: phí giao hàng theo ngưỡng, thời hạn trả hàng trên trục thời gian, sơ đồ tích–đổi điểm và quy đổi tiền. Nút sửa mở đúng nhóm trường. Đọc lại chính sách trước khi mở; giữ các trường khác trong lệnh lưu. |
| Tổng quan chủ cửa hàng | Ba số tiền theo kỳ, biểu đồ vòng trạng thái đơn hiện tại và thanh hàng sắp hết. Chọn trạng thái rồi Enter/nhấp đúp mở danh sách đơn đã lọc. Phạm vi tiền theo kỳ khác với trạng thái đơn/tồn hiện tại, có nhãn rõ. |
| Công việc kho | Phiếu nhập gần đây, số mã cần bổ sung, phiếu nhận dở và biểu đồ số lượng. Không truy vấn chỉ tiêu tiền dành cho chủ cửa hàng. |
| Thống kê hóa đơn | Mặc định biểu đồ đường theo thời gian; thanh theo nhân viên/sản phẩm. Nút Đường / Thanh / Bảng chuyển trực tiếp. Giá trị được chọn giữ đồng bộ với hàng dữ liệu nguồn. Ngày kết thúc trên giao diện được tính cả ngày; API vẫn dùng mốc loại trừ. |
| Báo cáo hoạt động | Thanh tổng hợp từ các dòng API đã tải; tối đa 12 nhóm lớn nhất. Bảng chi tiết luôn mở được và chứa toàn bộ dòng đã tải. Số dòng/phạm vi được giải thích, không gọi đó là toàn bộ dữ liệu cửa hàng. |
| Sản phẩm, nhóm hàng, nhà cung cấp, nhập kho, điều chỉnh tồn | Nút tạo riêng; các nút sửa/xử lý nằm cạnh hồ sơ đang chọn. Bỏ cơ chế chọn lệnh từ dropdown rồi bấm Thực hiện. Duyệt/nhận phiếu vẫn kiểm trạng thái và quyền. |
| Đơn hàng | Nút xác nhận trực tiếp từ danh sách, đọc lại trạng thái và mở xác nhận trước khi ghi. Chi tiết có bốn bước xử lý trực quan; chỉ nhấn mạnh bước hiện tại. |
| Trả hàng, hoàn tiền, thu cũ, bảo hành | Hành động có tên rõ, hướng dẫn thứ tự, giữ biểu mẫu và xác nhận hiện có. Bảo hành tách nút theo bước và chỉ bật khi trạng thái phù hợp; trường máy/lô thay thế chỉ xuất hiện ở bước xuất máy. |
| Khách, nhân viên, ưu đãi, tài nguyên, nhật ký | Giữ danh sách khi cần tra cứu nhiều hồ sơ; thống nhất thuật ngữ tiếng Việt và nhãn trường. Dữ liệu mã/ID gốc vẫn được giữ để gửi API. |
| Nội dung trang chủ | Nút sửa khối được chọn mở thẳng phần biên tập. Giữ quyền chỉ xem của chủ cửa hàng và quyền xuất bản của nhân viên nội dung. |
| Chi tiết hóa đơn | Các nhóm dữ liệu phụ chuyển bằng nút trực tiếp; chỉ tạo nội dung nhóm đang mở. Dữ liệu gốc/số lượng bản ghi không bị bỏ. |

## Kiểm tra và bằng chứng

`tests/BotZone.Admin.UxChecks` chạy trên cửa sổ WinForms thật, đăng nhập API local và chụp bằng `Graphics.CopyFromScreen`. Mỗi lần chạy so SHA-256 của DLL đang nạp với DLL publish, kiểm cửa sổ foreground/bounds trước khi chụp.

Lượt ứng viên `artifacts/winforms/ux-round5/final-review/result.json` đạt **305 kiểm tra, 64 ảnh**: 23 module chủ cửa hàng, các màn liên quan ở ba vai trò nhân viên, chế độ rộng/thu gọn, góc nút, lịch/dropdown, chuyển biểu đồ/bảng, mở thẳng biểu mẫu, bảo toàn context và trường chính sách. Các biểu đồ âm/0 được kiểm riêng. HTTP audit ghi nhận không gửi lệnh nghiệp vụ khi mở/hủy biểu mẫu.

Lượt xác minh bản publish cuối nằm ở `artifacts/winforms/ux-round5/published-review-v2/result.json`; hash và tiến trình bàn giao nằm trong `artifacts/admin/build-manifest.json` và `running-admin.json`. Không dùng kết quả của ứng viên thay cho kết quả DLL khác. Lượt `published-review` trước đó bị dừng vì thông báo ứng dụng khác che cửa sổ; ảnh bị che không được nhận làm bằng chứng.

Bản publish vòng 5 trước lần sửa hộp thoại Tổng quan đạt **305/305 kiểm tra, 64 ảnh**, không gửi lệnh ghi nghiệp vụ. DLL SHA-256 của lượt đó: `B19A9A59B340AA3B0DE9C3EE92B8E9B235380ACFA5550D75BEAC092781B1A0D7`. Build Release không có cảnh báo/lỗi. Kiểm HTTP xác nhận API `ready` và storefront `/vi` trả 200.

Các lượt đầu `first-review`, `second-review`, `third-review` là lịch sử tìm lỗi, không phải bản bàn giao: bố cục compact, chữ bị cắt và lỗi gọi logout hai lần trong harness đã được sửa. Ảnh và manifest là artifact local, không đưa vào Git.

Giới hạn: kiểm tại DPI thật 125%, không coi resize là kiểm 100%/150%. Không xác nhận Narrator hoặc giao dịch ghi mới chỉ từ việc mở biểu mẫu. Backend vẫn quyết định quyền, trạng thái, tồn và tiền; các khoảng thiếu API cũ trong `UI_REDESIGN_TRACKING.md` vẫn còn hiệu lực.

## Chạy lại

Từ thư mục dự án, khởi động API/storefront theo `scripts/start-local.ps1`, rồi:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-admin.ps1 -Publish
dotnet run --project tests/BotZone.Admin.UxChecks -c Release -- artifacts/winforms/ux-checks
```

Harness cần desktop Windows đang mở và bộ tài khoản demo local. Có thể cấu hình `BOTZONE_API_URL`, `BOTZONE_SMOKE_PASSWORD`, `BOTZONE_AUDIT_ASSEMBLY`. Không tự seed hoặc xóa dữ liệu. Trong môi trường automation có desktop cô lập, phải khởi chạy harness trên desktop tương tác; chương trình từ chối ảnh desktop không khớp.

Mở bản quản trị bình thường:

```powershell
Start-Process .\artifacts\admin\win-x64\BotZone.Admin.WinForms.exe
```

Web bán hàng: `http://localhost:3000/vi`. Quản trị là ứng dụng Windows, không có URL quản trị web.

## Sửa hộp thoại Tổng quan — 18/11/2026

- Khoảng ngày: hàng nút riêng ở đáy; kích thước nhỏ nhất theo cửa sổ đã dựng tại DPI hiện tại để không cắt ô ngày hoặc lỗi tại trường. Ngày cuối vẫn được tính cả ngày; Hủy/Escape không áp dụng thay đổi, khoảng một ngày hợp lệ.
- Cách đọc số liệu: ba mục riêng có tên, số tiền và mô tả; giữ kỳ đã tải và giải thích phạm vi dữ liệu. Nội dung cuộn độc lập ở cửa sổ nhỏ, nút Đóng luôn nhìn thấy.
- Các hộp thông tin văn bản dùng xuống dòng Windows (`CRLF`) để không dính các đoạn khi nguồn truyền `LF`.

Kiểm đúng hai hộp thoại bằng `--overview-dialogs`: **31/31 PASS, 7 ảnh desktop thật**, tại `artifacts/winforms/overview-dialogs/final-review/`. Kiểm góc/nút, ô ngày và lỗi không bị cắt, kỳ một ngày, hủy/khôi phục kỳ, giá trị từng mục, cuộn tới ghi chú và số dòng của ô văn bản native. Không ghi dữ liệu nghiệp vụ. DLL được kiểm: `6496ABA0B22A17874E15008EE8907574405E74AACE90F81A5D112AD4D17F0A9F`; build Release 0 warning/0 error. Đây là lượt hồi quy tập trung cho bản sửa, không phải chạy lại toàn bộ 305 kiểm tra.
