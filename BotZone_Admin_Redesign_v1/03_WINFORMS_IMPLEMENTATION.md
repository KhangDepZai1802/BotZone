# BotZone Admin — Hướng dẫn triển khai native WinForms

Phiên bản thiết kế: v1 · Phạm vi: Windows laptop và PC. Tài liệu này đi cùng `01_REDESIGN_SPEC.md`, `02_SCREEN_BLUEPRINTS.md` và bản tham chiếu trực quan trong bộ redesign. Nếu tên file khác trong gói cuối, dùng manifest/README của gói để tìm đúng tài liệu tương ứng.

## 1. Ranh giới triển khai

Đây là đợt redesign giao diện, tương tác và tổ chức thông tin của ứng dụng quản trị C# Windows Forms đang có. BA v1.1 định hướng .NET 10 + ASP.NET Core API + PostgreSQL chạy local. Đọc `.csproj` và cấu trúc code thực tế để xác nhận; không nâng framework hoặc thay backend chỉ vì đổi UI.

Phải bao phủ đăng nhập, shell, toàn bộ module mà bốn role hiện có sử dụng, các form tạo/sửa, chi tiết, lọc, thống kê, export và trạng thái lỗi/rỗng/loading. Một màn tổng quan đẹp cho mỗi role chưa phải hoàn thành redesign.

Giữ nghiệp vụ, điều kiện chuyển trạng thái, phép tính, transaction, endpoint, quyền, validation, DTO và dữ liệu hiện tại. UI gửi cùng command/service, với cách trình bày dễ dùng hơn. Nếu phát hiện lỗi nghiệp vụ khi sửa UI, ghi rõ và sửa theo phạm vi thực sự cần; không âm thầm đổi công thức để phù hợp số minh họa.

Giao diện quản trị chỉ chạy Windows laptop/PC. Không dựng giao diện admin mobile, website admin, WPF/WinUI thay thế hoặc bọc toàn bộ app bằng WebView2. WebView2 chỉ dành cho preview landing/PDF theo BA hiện có; bản HTML của bộ redesign là tài liệu tham chiếu, không phải admin để nhúng vào executable.

## 2. Quyết định toolkit

| Hiện trạng repo | Hướng triển khai |
|---|---|
| Đã có toolkit hoạt động ổn định | Giữ toolkit, xây palette và component wrapper BotZone để thống nhất giao diện |
| Chưa có toolkit | Đề xuất AntdUI bản stable tương thích cho shell/forms; DataGridView native được style đồng bộ cho bảng nghiệp vụ cần giữ binding/keyboard/performance |
| Đang dùng Krypton | Giữ Krypton, chỉnh custom palette và layout; không chuyển sang AntdUI chỉ vì bản mockup giống web |
| Toolkit không đạt kiểm thử cụ thể | Thay control bị ảnh hưởng bằng native WinForms tương đương; không hạ toàn bộ thiết kế thành form mặc định |

Không thêm dependency trả phí, bản trial hoặc yêu cầu license key mới. Chọn version cụ thể sau khi kiểm target framework và dependency hiện có. Không cài bản preview/nightly để lấy hiệu ứng trang trí. Không trộn nhiều toolkit chủ đạo.

AntdUI công bố hệ control GDI, SVG/DPI và theme; giấy phép Apache-2.0. Krypton Standard Toolkit dùng BSD-3-Clause và có thay đổi API/framework support giữa release. Giữ license/notice dependency đúng phiên bản. Khả năng được mô tả trong README không thay cho test keyboard, UI Automation và dữ liệu thật của BotZone. [S1][S2]

## 3. Bắt buộc áp dụng UI/UX Pro Max

Codex phải mở skill `ui-ux-pro-max` đang cài và thực hiện workflow, không chỉ nhắc tên skill trong báo cáo:

1. Phân tích retail backoffice, bốn role, tác vụ chính, Windows laptop/PC và stack WinForms thực tế.
2. Chạy tìm hệ thiết kế bằng `--design-system`, với query tập trung như `retail operations desktop tables`; đọc kết quả và loại các phần không phù hợp.
3. Tìm UX theo vấn đề quan sát được, ví dụ `keyboard focus modal`, `error summary validation`, `dense tables filtering` hoặc `loading cancellation feedback`, mỗi lần một ý chính.
4. Đối chiếu master redesign và nguyên tắc native Windows; ghi ngắn gọn insight được áp dụng vào component hoặc màn nào.
5. Rà checklist skill trên UI đã triển khai, có kết quả kiểm thật.

Phiên bản skill đã nghiên cứu **không có stack WinForms**. Không dùng output WPF, WinUI hoặc Avalonia rồi ghi đó là hướng dẫn WinForms. Dùng phần design-system/domain UX có liên quan và tài liệu Microsoft WinForms cho implementation. Không áp nguyên các khuyến nghị web/mobile như CSS, ARIA, bottom tab bar hoặc breakpoints điện thoại vào executable native.

Master của bộ redesign là hướng thiết kế đã chọn. Skill là công cụ kiểm và bổ sung, không tự động thay burgundy bằng màu ngẫu nhiên hoặc sinh lại một dashboard khác. Không chạy persist/force ghi đè master ngoài ý muốn.

## 4. Tokens và quy tắc hiển thị

| Token | Giá trị định hướng |
|---|---|
| Workspace | `#FAFAF9` |
| Surface / vùng bảng / form | `#FFFFFF` |
| Sidebar graphite | `#19191C` |
| Primary burgundy | `#6F233F` |
| Font | Segoe UI; fallback font Windows tương thích |
| Body mặc định | 10.5–11 pt, kiểm dấu tiếng Việt và DPI |
| Radius | 6–8 đơn vị thiết kế ở 96 DPI, chỉ nơi cần |
| Spacing | 4 / 8 / 12 / 16 / 24 |
| Sidebar | Mở khoảng 224; thu 64; nhãn có tooltip khi thu |
| Row dữ liệu | Khoảng 40; compact 36 chỉ khi vẫn đủ dễ đọc |
| Nút thao tác | Khoảng 36–40 cao; text không clip khi scale |

Đây là đơn vị thiết kế cơ sở, không phải pixel vật lý cố định trên mọi màn. Font, control height và padding phải scale cùng nhau. Dùng token trong một nơi thay vì rải mã màu và kích thước trên các form. Màu trạng thái lấy từ master: luôn có chữ/icon đi cùng, không dùng màu làm tín hiệu duy nhất.

Tách sắc burgundy của hành động chính khỏi màu cảnh báo/xóa bằng nhãn, icon và vị trí. Không để cả trang có hàng chục nút primary. Một trang có một thao tác chính rõ; bulk actions chỉ xuất hiện khi đã chọn dòng.

Ưu tiên đường phân cách nhẹ, phân cấp chữ và khoảng cách. Không gradient trang trí trên bảng, không shadow lớn ở mọi card, không emoji làm icon hệ thống, không dùng ảnh nền dưới dữ liệu. Cảm giác cao cấp đến từ hệ thống nhất quán và thao tác ít ma sát.

## 5. Bản đồ component native

Tên `Bz...` là tên đề xuất; có thể theo convention repository. Không cần thêm một abstraction layer mới nếu đã có component tương đương.

| Component | Cấu trúc WinForms đề xuất | Hành vi nghiệm thu |
|---|---|---|
| `BzLoginForm` | Form + TableLayoutPanel + input/button native hoặc toolkit | Enter submit; focus đúng; lỗi gần field; không chọn role để tăng quyền |
| `BzAppShell` | Một Form chính, sidebar/header/content host | Điều hướng nhanh; giữ window chrome và trạng thái màn trước |
| `BzNavigation` | Menu hoặc Button có nhãn/icon | Chọn hiện tại rõ; menu và tìm nhanh tuân role |
| `BzPageHeader` | LayoutPanel: title/subtitle/actions | Một primary action; secondary không lấn át |
| `BzDataGrid` | DataGridView + BindingSource hoặc table hiện có đã kiểm | Sort/filter thật; selection, Enter, scroll ổn định |
| `BzViewTabs` | Tab/segmented buttons + count từ query | Saved view tương ứng bộ lọc thật, không số giả |
| `BzFilterBuilder` | Trường + operator + editor theo kiểu + group control | `> < = <>`, AND/OR/NOT ánh xạ đúng bộ lọc backend |
| `BzDetailPane` | SplitContainer/panel bên phải; Drawer nếu đạt focus/UIA | Escape đóng; trở về dòng đã chọn; không mất vị trí danh sách |
| `BzEditPage` | UserControl với form groups và command row | Lưu/huỷ rõ; giữ dữ liệu khi lỗi; cảnh báo thay đổi chưa lưu |
| `BzStateTag` | Text + biểu tượng nhỏ, style dùng chung | Accessible name đọc được; không chỉ đổi màu |
| `BzFeedback` | Inline alert/status + dialog có ngữ cảnh khi cần | Loading/error/retry rõ; không toast che action |
| `BzReportPage` | Bộ lọc trên, biểu đồ vừa đủ, bảng dưới | Tổng và export dùng cùng query/định nghĩa hiện có |
| `BzLandingEditor` | Native list/tree sections + property form + preview | Nội dung có quyền sửa/xuất; chủ cửa hàng chỉ xem CMS |

Drawer phù hợp xem nhanh, không phải nơi nhồi mọi form nhiều tab. Nếu vùng nội dung quá hẹp, mở một trang chi tiết trong content host với nút quay lại. Không mở dialog chồng dialog cho tác vụ thường xuyên. Dialog chỉ dùng khi quyết định ngắn và cần tập trung.

## 6. Role và xác thực

Role do hệ thống xác thực trả về, không do nút chọn giao diện ở màn đăng nhập. Demo switcher trong prototype không được đưa vào bản chạy như một cách đổi quyền.

- **Chủ cửa hàng:** xem toàn cảnh, các phân hệ và thao tác theo policy hiện có; riêng nội dung/landing chỉ xem tổng thể, không tự cấp quyền chỉnh hoặc xuất bản.
- **Nhân viên xử lý đơn:** hàng đợi và chi tiết xử lý theo quyền; không mở quyền kho, giá vốn, hệ thống hoặc CMS chỉ vì nhìn thấy số liệu liên quan.
- **Nhân viên kho:** chứng từ và tác vụ kho theo policy; không có nút đổi trạng thái kinh doanh ngoài quyền.
- **Nhân viên nội dung:** quản lý nội dung, asset và landing theo policy; không sửa tiền, giá giao dịch, tồn kho hay xác nhận thanh toán.

Ẩn menu không thay thế kiểm quyền ở service/API. Command palette, deep link, export, context menu và action trong pane đều phải đi qua policy cũ. Không thay auth/permission để làm mockup chạy nhanh. Chỉ hiển thị dữ liệu mà account có quyền xem.

## 7. Async, binding và bảng lớn

Giữ repository/service hiện tại. Các I/O load/save/report/export chạy async phù hợp; UI giữ phản hồi và tránh click gửi trùng. Hủy truy vấn hoặc bỏ kết quả cũ khi đổi filter/tab nhanh, tránh request chậm ghi đè kết quả mới. Không cập nhật control từ background thread. `.NET 10` có thể dùng `InvokeAsync` sau khi xác minh target; API này có từ .NET 9, không có trên .NET Framework. [S5]

Microsoft khuyến nghị chia sẻ DataGridView style, tránh object/style/context menu cho từng cell và dùng autosize hợp lý. [S4] Vì vậy, không tạo hàng nghìn Button con, không đo toàn bộ dataset sau mỗi refresh. Giữ paging/filter trên service hiện có. Chỉ áp virtual mode nếu dữ liệu và đo đạc chứng minh cần; không viết lại data layer như điều kiện để redesign.

Không bind lại cả grid chỉ để cập nhật selection; giữ row key, sort, page, filter và scroll khi quay từ chi tiết. Định dạng tiền/ngày ở UI theo culture mà không đổi kiểu dữ liệu/số gốc. Export Excel/PDF dùng cùng phạm vi lọc và định nghĩa số liệu hiện có; số hiển thị minh họa trong prototype không được seed vào DB thật.

## 8. DPI, kích thước cửa sổ và bàn phím

Đọc DPI config trong project, bootstrap và manifest trước khi sửa. Tài liệu Microsoft autoscale hiện nêu SystemAware là default/recommended và mô tả PerMonitorV2 như một mode có notification/scaling khác; lựa chọn phải phù hợp toolkit và được kiểm. Không bật đồng thời nhiều cấu hình mâu thuẫn hoặc scale thủ công hai lần. Designer DPI không phải runtime DPI. [S3]

Baseline bắt buộc khi môi trường Windows cho phép: 1366×768 ở 100% và 125%; 1920×1080 ở 125% và 150%. Các cấu hình 1920×1080/100% và 2560×1440/150–200% là kiểm bổ sung theo phần cứng và rủi ro, không tự tạo blocker khi không có màn tương ứng. Nếu chưa kiểm được baseline, ghi rõ phần chưa xác minh. Ma trận mục tiêu:

| Độ phân giải vật lý | Scale cần kiểm | Điều kiện |
|---|---|---|
| 1366×768 | 100%, 125% | Login, shell, bảng, dialog không vượt work area |
| 1920×1080 | 100%, 125%, 150% | Bảng và pane cân đối, text/icon không mờ hoặc clip |
| 2560×1440 | 150%, 200% | Form và command row vẫn truy cập được; kiểm chuyển màn nếu có |

1366×768 ở 125% có diện tích logic xấp xỉ 1093×614 trước khi trừ taskbar/chrome; không đặt minimum size vượt phần còn lại. Ưu tiên `Screen.WorkingArea`, layout co giãn, sidebar thu, form auto-scroll và pane đổi bố cục. Nút lưu, huỷ, xác nhận luôn có đường truy cập rõ ràng. Bảng nhiều cột được phép cuộn ngang/chọn cột có chủ đích, không thu font đến mức khó đọc.

Dùng AccessibleName/Role/Description, TabIndex, mnemonic cho control tương tác. Focus nhìn thấy, label rõ, lỗi gần field và trạng thái không phụ thuộc màu. Native controls cung cấp nền accessibility nhưng vẫn phải kiểm composition của app. [S6]

Shortcuts đề xuất: Ctrl+K tìm nhanh theo quyền; Ctrl+F tìm trong màn; F5 refresh; Enter mở dòng; Escape đóng pane; Ctrl+S lưu khi đang sửa. Không chiếm các phím này sai ngữ cảnh hoặc phá thao tác nhập IMEI/mã hàng. Test Narrator ở login, sidebar, grid, field lỗi và dialog xác nhận.

## 9. Quy trình và nghiệm thu không hồi quy

Lập `UI_REDESIGN_TRACKING.md`: route/form, role, tác vụ, component, trạng thái trước/sau, ảnh thật, kiểm đã chạy. Danh sách lấy từ BA **và** code đang có, bao gồm module phụ. Không đánh dấu hoàn tất chỉ vì đã thay base form hoặc shell.

Thực hiện login + shell + một danh sách có chi tiết/lọc trước để chứng minh component; tiếp tục nhân rộng cho toàn inventory, không dừng ở lát cắt đầu tiên. Chia agent theo file/module có ownership rõ, phối hợp shared component trước khi sửa đồng thời. Giữ working tree và thay đổi người dùng; không yêu cầu commit/branch như một bước chặn nếu repo không yêu cầu.

Gate bắt buộc: build target Windows; mở executable; bốn account/role; workflow chính với dữ liệu kiểm; permission CMS; hóa đơn/filter/tổng/export; keyboard/DPI; ảnh trước/sau. Kiểm chuyên sâu tập trung vào binding, event, async cancellation, permission và tổng/export, không viết hàng trăm test chỉ để so padding.

Nếu môi trường chỉ có Linux hoặc không mở được Windows app, vẫn hoàn thiện code/spec và chạy được những kiểm tra phù hợp, nhưng báo rõ runtime/DPI/screenshot native chưa kiểm. HTML prototype, build pass và screenshot thiết kế không phải bằng chứng WinForms chạy đẹp.

## Nguồn kỹ thuật

- [S1: AntdUI repo](https://github.com/AntdUI/AntdUI) · [LICENSE Apache-2.0](https://github.com/AntdUI/AntdUI/blob/main/LICENSE)
- [S2: Krypton Standard Toolkit](https://github.com/Krypton-Suite/Standard-Toolkit) · [LICENSE BSD-3-Clause](https://github.com/Krypton-Suite/Standard-Toolkit/blob/master/LICENSE)
- [S3: Microsoft Automatic scaling](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/forms/autoscale)
- [S4: Microsoft DataGridView scalability](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/best-practices-for-scaling-the-windows-forms-datagridview-control)
- [S5: Microsoft thread-safe control calls](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-make-thread-safe-calls)
- [S6: Microsoft accessibility properties](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/properties-on-windows-forms-controls-that-support-accessibility-guidelines)

Các kích thước, component và gate ở trên là quyết định thiết kế cho BotZone, không phải quy định mặc định của những thư viện tham khảo.
