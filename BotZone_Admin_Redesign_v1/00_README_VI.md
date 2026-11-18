# BotZone Admin Redesign v1 — Bắt đầu ở đây

**Ngày:** 17/11/2026. **Nền tảng:** C# Windows Forms native. **Thiết bị:** laptop và PC Windows.

Bộ bàn giao này thiết kế lại đăng nhập, khung ứng dụng và các luồng quản trị cho Chủ cửa hàng, Nhân viên xử lý đơn, Nhân viên kho và Nhân viên nội dung. Nghiệp vụ, phân quyền, API và database hiện có phải được giữ nguyên khi triển khai.

## 1. Xem thiết kế

- `BotZone_Admin_Design_Book_v1.pdf`: 19 trang; xem nhanh định hướng, bốn vai trò, hệ thiết kế, cơ sở nghiên cứu và 14 mẫu màn hình.
- `BotZone_Admin_Review_v1.html`: mở trực tiếp bằng trình duyệt để xem 14 mẫu, chuyển nhóm và bấm các vùng liên kết. File tự chứa ảnh, không cần server hay mạng.
- `BotZone_Admin_Overview_v1.jpg`: ảnh tổng hợp.
- `visuals/`: 14 PNG và 14 SVG chỉnh sửa được; `screens.json` mô tả kích thước, quyết định và liên kết giữa các mẫu.

**Đây là bộ thiết kế đề xuất và dữ liệu minh họa, chưa phải giao diện WinForms đã được sửa trên máy của bạn.** Trình xem HTML chỉ giúp duyệt mẫu; các nút trên ảnh không lưu, thanh toán hay xuất bản dữ liệu. Bộ chọn nhóm trong trình xem không phải role switcher cho ứng dụng thật.

## 2. Đưa cho Codex

1. Giải nén gói vào `docs/design/admin-redesign-v1/` trong repository BotZone. Trong thư mục này phải thấy trực tiếp `00_README_VI.md` và `05_PROMPT_CODEX.md`; tránh thêm một tầng thư mục cùng tên.
2. Mở Codex ở thư mục gốc dự án đang có ứng dụng WinForms.
3. Dán toàn bộ nội dung `05_PROMPT_CODEX.md` vào session. Prompt yêu cầu Codex kiểm hiện trạng, dùng UI/UX Pro Max thực sự, triển khai giao diện và kiểm trên Windows.
4. Codex ghi tiến độ theo toàn bộ màn của BA/code, đối chiếu ảnh trước–sau và ghi rõ phần chưa thể kiểm. Không nghiệm thu chỉ vì build thành công hoặc tạo được ảnh HTML đẹp.

Nếu vị trí giải nén khác, sửa đúng dòng đường dẫn trong prompt. Không trỏ vào thư mục tài sản 3D v003 thay cho gói redesign này.

## 3. Đọc tài liệu theo thứ tự

| File | Mục đích |
|---|---|
| `01_REDESIGN_SPEC.md` | Phạm vi, quyền, hệ thiết kế, bốn vai trò và danh mục module đầy đủ |
| `02_SCREEN_BLUEPRINTS.md` | Bố cục và hành vi cụ thể cho màn hình, trạng thái và luồng liên quan |
| `03_WINFORMS_IMPLEMENTATION.md` | Ánh xạ sang control native, toolkit, DPI, keyboard, async và tái sử dụng code |
| `04_ACCEPTANCE_CHECKLIST.md` | Tiêu chí nghiệm thu và bằng chứng cần thu khi chạy app |
| `05_PROMPT_CODEX.md` | Prompt thực thi đầy đủ để dán vào Codex |
| `06_SCREEN_MAP.md` | Đối chiếu mã màn nghiệp vụ với mã ảnh; tránh hiểu 14 ảnh là toàn bộ phạm vi |
| `07_RESEARCH_AND_DECISIONS.md` | Nguồn chính thức, pattern đã học và các quyết định áp dụng |
| `design-tokens.json` | Màu, chữ, khoảng cách, kích thước và breakpoint thống nhất |
| `QA_REPORT.md` | Những gì đã kiểm ở bộ thiết kế và những gì còn cần kiểm trên app thật |

Thứ tự giải quyết khác biệt: yêu cầu đã chốt của người dùng và BA/quyền/nghiệp vụ hiện hành → spec/blueprint → tokens cho giá trị hình thức → ảnh minh họa. Khi ảnh thiếu cột, trạng thái hoặc module, không được suy ra phải bỏ nghiệp vụ đó. Nếu code và BA lệch nhau, ghi gap cụ thể trước khi sửa phần liên quan; không tự tạo kiến trúc thứ hai.

## 4. Những quyết định đã chốt

- Admin native Windows Forms trên **laptop và PC**. Không làm admin mobile và không bọc HTML thành ứng dụng quản trị.
- Bố cục trắng ngà, sidebar graphite, burgundy tiết chế; bảng dữ liệu và hành động rõ ràng. Runtime dùng **Segoe UI**. Tài liệu/ảnh xuất bằng **Noto Sans** có dấu tiếng Việt; đây không phải yêu cầu đổi font runtime.
- Bốn vai trò có cùng ngôn ngữ thiết kế, nhưng trang đầu, hàng chờ và hành động được tối ưu theo công việc.
- Chủ cửa hàng chỉ xem CMS/landing; chỉ nhân viên nội dung được sửa/xuất bản/khôi phục theo quyền. Nội dung không được sửa giá, tồn hoặc tài chính.
- Tiền hóa đơn, tiền thu, hoàn và lãi gộp là các chỉ số khác nhau. Tổng, lọc, thống kê và xuất file phải dùng cùng phạm vi dữ liệu/quyền hiện có.
- UI/UX Pro Max là công cụ hỗ trợ thiết kế bắt buộc, phải lọc khuyến nghị theo bối cảnh WinForms. Không đổi yêu cầu đã chốt chỉ vì một kết quả tự sinh.
- Ưu tiên dùng lại toolkit ổn định của dự án. AntdUI là phương án nếu cần; không bắt đổi dependency chỉ để giống ảnh.
- Giữ website bán hàng, model 3D, API và database đã có. Bộ này không bổ sung Supabase, Vercel, backend hay migration khác.

## 5. Phạm vi kiểm trên Windows

Baseline: 1366×768 ở 100% và 125%; 1920×1080 ở 125% và 150%. Resize trong work area thật và kiểm bàn phím, lỗi mạng, dữ liệu chưa lưu, bốn role. Cấu hình cao hơn kiểm bổ sung khi có phần cứng hoặc rủi ro cụ thể.

Mẫu `LAPTOP-COMPACT` là 1068×584 logical px để thể hiện sidebar thu gọn và bỏ pane cố định. Nó không phải bằng chứng đã chạy tại một DPI Windows cụ thể.

Các tên khách hàng, nhân viên, mã đơn, số tiền, ngày và số lượng trên ảnh là dữ liệu giả lập dùng giải thích giao diện; không được seed chúng vào database như dữ liệu thật. Poster 3D sử dụng tài sản BotZone v003 đã có trong workspace để minh họa CMS; không xác nhận đó là thiết kế hay màu chính thức của Apple.
