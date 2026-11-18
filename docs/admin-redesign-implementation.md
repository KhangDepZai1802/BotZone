# BotZone Admin — cập nhật 18/11/2026

Vòng hiện tại là **vòng 5**, tiếp tục từ bản vòng 4: cài đặt bằng minh họa chính sách, tổng quan/báo cáo bằng biểu đồ, thao tác trực tiếp và thuật ngữ dễ hiểu. Chi tiết thay đổi, phạm vi kiểm và cách chạy: [admin-ux-round5.md](admin-ux-round5.md). Các bằng chứng vòng 1–4 bên dưới là lịch sử, không thay cho kết quả bản mới.

Nguồn thiết kế giữ nguyên tại `BotZone_Admin_Redesign_v1/`; không tạo bộ thiết kế thay thế. Phạm vi và tình trạng từng màn/action nằm ở `UI_REDESIGN_TRACKING.md` tại root. Bản sao source trước sửa: `backups/admin-redesign-source-20261117-152035/`.

Vòng 2 thực hiện phản hồi của Khang về màn hình nhiều chữ, quá nhiều tab và bảng báo cáo bỏ trống bên phải. Thông tin chính hiện trước; dữ liệu phụ mở theo nhóm, menu hoặc dialog. Phạm vi là giao diện và điều hướng admin: giữ API, DTO, quyền, dữ liệu, công thức tài chính, filter AST và nghiệp vụ hiện có. Phần bàn giao vòng 1 được giữ ở mục lịch sử bên dưới, không dùng làm bằng chứng cho binary vòng 2.

Vòng 3 tiếp tục trên source hiện có theo yêu cầu “làm tiếp”: khôi phục phiên giữ công việc, lịch tháng/năm, bộ chọn dữ liệu và lỗi tại trường của các form phụ. **96 kiểm tra chức năng/layout PASS, 80 PNG `DrawToBitmap` của vòng 3 không phải nghiệm thu thị giác trên desktop thật.** Khang đã phát hiện góc nút trắng/viền bẩn trong cửa sổ đang dùng và sidebar chưa thể hiện rõ nhóm/mục con; tuyên bố nghiệm thu thị giác vòng 3 được rút lại. Source vòng 4 đã hoàn tất, gồm sidebar sáng theo yêu cầu mới nhất, sửa control và cột/empty state. Đã có ảnh `CopyFromScreen` của shell sáng cùng kiểm component/grid trên Release cuối; shell cuối đạt 43 PASS/7 ảnh trên đúng Release 5B9BF00E…; executable đã mở và đăng nhập trên desktop Default. Không dùng PASS của binary trước thay thế.

## Component và toolkit

Giữ WinForms .NET 10 và control native hiện có. Không thêm AntdUI: baseline đã chạy ổn với DataGridView, native input và dialog; bổ sung wrapper nhỏ trên Button/TabControl giữ semantic và bàn phím phù hợp hơn việc đổi toàn bộ dependency. `Ui.cs` và `UiComponents.cs` tập trung palette, Segoe UI 10.5 pt, header, label/field, grid, busy, form/work area. Vòng 2 đồng bộ màu với web bán hàng theo yêu cầu của Khang; các giá trị thực trong `Ui.cs` là:

| Thành phần | Token hiện tại |
| --- | --- |
| Hành động chính / focus | `Primary #6B263D` / `Focus #764355` |
| Nền trang / bề mặt | `Paper #F5F5F6` / `Surface #FFFFFF` |
| Viền / nền chọn nhẹ | `Line #E3E3E5` / `Soft #F5EBEF` |
| Chữ chính / chữ phụ | `Ink #171719` / `Muted #636367` |
| Sidebar sáng / chữ nhóm | `NavSurface #FFFFFF` / `NavMuted #706C73` |
| Lỗi | `Danger #A13035` |

Lượt polish theo phản hồi trực tiếp của Khang đã thay sidebar chữ viết tắt bằng icon vector, khung ô nhập đồng bộ, tab phẳng, thẻ KPI có phân cấp, badge trạng thái và toolbar gọn hơn. `PageHeading` đo tiêu đề/nút theo chiều rộng thực; `InputSurface` giữ control gốc và binding. Khung input và button có đường vẽ bo góc 10 logical px, chiều cao input đủ chứa combo native ở DPI thực. Date picker đang focus giữ native selection của ngày/tháng/năm. Các nút cuộn tab native được bảo toàn khi header được vẽ lại. Kiểm bố cục và ảnh `DrawToBitmap` trước đây không bắt được lỗi nền ngoài góc button khi Windows vẽ cửa sổ thật; phần này được mở lại ở vòng 4.

`BzContextMenu` trong `Disclosure.cs` là ContextMenuStrip có renderer dùng cùng token, khung bo góc 12 px và mục đang chọn bo góc 8 px. `Ui.MenuButton` dùng menu này cho tác vụ phụ. `BzComboBox` tiếp tục dùng danh sách dropdown native, bo vùng cửa sổ popup và mở rộng theo nội dung trong giới hạn màn hình. `CalendarPopup` là popup WinForms bo góc 12 px cho `BzDateTimePicker`; giữ MinDate/MaxDate, phần thời gian của giá trị khi chọn ngày và giá trị cũ khi Escape. Không nhúng HTML hoặc browser vào admin để tạo các control này.

Logo Khang gửi được tích hợp vào web bán hàng (header/auth/footer/favicon), native login, sidebar thường/thu gọn và icon executable/window. Asset và prompt xử lý ảnh ở `public/brand/README.md`; branding web là phạm vi bổ sung được Khang yêu cầu riêng, không thay nghiệp vụ storefront. Ảnh kiểm web và kết quả tại `artifacts/web/brand-redesign/`.

`MainForm` giữ một instance mỗi module trong phiên, hủy truy vấn đọc khi rời trang, giữ filter/sort/trang/selection trong control. Mỗi lần điều hướng xác thực lại session; kết quả điều hướng cũ bị bỏ. Logout và dispose giải phóng toàn bộ trang. Đổi quyền server loại cache cũ. Ctrl+K chỉ liệt kê module được phép; F5 qua dirty guard. Owner vào tổng quan, xử lý đơn vào hàng chờ, kho vào công việc kho, content vào công việc nội dung. Không có role switcher.

Sidebar chia thành Tổng quan, Đơn & thanh toán, Hàng hóa, Khách & hậu mãi, Báo cáo, Nội dung và Cửa hàng. Chỉ nhóm và module được cấp quyền mới xuất hiện; nhóm đang dùng mở các module con. Khi thu gọn, icon nhóm mở menu chức năng. Cách chia này giảm số mục luôn hiển thị mà vẫn giữ đường đến từng module và tìm nhanh Ctrl+K.

Các chỉ tiêu owner đọc ba báo cáo riêng `issued`, `adjustments`, `net` từ API hiện có, ghi thời điểm dữ liệu; không giả là cùng một transaction snapshot. Không tự tính lại tài chính trên UI. Các giới hạn thiếu projection ở baseline được ghi rõ trong tracking.

## Phân lớp thông tin ở vòng 2

Header ưu tiên một hành động chính và menu Thao tác. Định nghĩa, thời điểm đọc dữ liệu và mô tả phạm vi xuất nằm trong Thông tin; vẫn truy cập được khi cần đối chiếu. Loading, lỗi, trạng thái rỗng và thay đổi chưa áp dụng vẫn hiện tại vị trí thao tác. Các cột phụ được mở bằng Xem thêm hoặc Cột hiển thị, không loại khỏi dữ liệu trả về.

Chi tiết hóa đơn có ba nhóm cấp đầu: **Mặt hàng / Thanh toán / Thông tin**. Mặc định chỉ hiện mã hóa đơn, khách/ngày ngắn, một số tiền có nhãn **Giá trị phát hành** và bảng mặt hàng. Thanh toán có lựa chọn phụ Tổng quan, Khoản thu, Chứng từ giảm, Khoản hoàn, Lần thanh toán; chỉ dữ liệu đang chọn được trình bày. Thông tin chia Khách và giao hàng, Thông tin chứng từ, Ưu đãi và phiên bản. Phân bổ từng món mở riêng từ Xem thêm. Xuất là hành động chính; nguồn đơn và tải lại nằm trong Thao tác; Đóng ở footer và nút X chuẩn của Form. Không còn tám tab ngang và các bảng phụ cùng xuất hiện.

Chi tiết khách hàng chia **Hồ sơ / Đơn hàng / Lịch sử**. Hồ sơ mặc định chỉ hiện liên hệ, credit và điểm; địa chỉ và thông tin tài khoản bổ sung mở riêng. Lịch sử dùng lựa chọn phụ cho credit, điểm, trả hàng, thu cũ và bảo hành. Số dòng và giới hạn snapshot vẫn được giữ; metadata nằm trong Thông tin. Bộ lọc AST giữ cấu trúc AND/OR/NOT, giới hạn bốn cấp/30 điều kiện và validation hiện có; diễn giải đầy đủ mở qua Đọc lại bộ lọc, tự hiện khi validation lỗi.

Các bảng tài chính có cột mặc định theo nhiệm vụ:

| Màn / chế độ | Cột mặc định | Thông tin bổ sung |
| --- | --- | --- |
| Danh sách hóa đơn | Mã hóa đơn, Phát hành, Khách hàng, Tổng HĐ, Thanh toán | Cột hiển thị và chế độ xem trong Thao tác |
| Mặt hàng hóa đơn | Mặt hàng, SL, Đơn giá, Thành tiền | Xem thêm mở cột chi tiết và phân bổ từng món |
| Báo cáo theo kỳ | Kỳ, Số hóa đơn, Giá trị | Cột hiển thị mở số lượng, tiền demo và store credit |
| Báo cáo nhân viên / sản phẩm | Kỳ, Đối tượng, Số hóa đơn, Giá trị | Cùng lựa chọn cột đối soát; giữ drill-down |
| Tồn kho | SKU, Khả dụng, Đang giữ, Chờ kiểm tra, Giá trị tồn | Quyền kho vẫn nhận dữ liệu đã loại giá trị tiền từ API |
| Lãi gộp / sản phẩm bán chạy | Sản phẩm, Số lượng ròng, Doanh thu ròng, Giá vốn VND, Lãi gộp VND | Cột còn lại mở theo nhu cầu đối chiếu |
| Hiệu quả ưu đãi | Mã ưu đãi, Đã dùng, Đang giữ, Đã trả, Giảm giá đã dùng | Cột hiển thị giữ quyền truy cập các trường còn lại |
| Thu cũ / cấp credit | Tạo lúc, Tiếp nhận, Trạng thái, Số món, Credit đã cấp | Cột hiển thị giữ quyền truy cập các trường còn lại |
| Bảo hành / Care | SKU, Trạng thái, Tạo lúc, Chi phí thay thế, Chứng từ giảm | Cột hiển thị giữ quyền truy cập các trường còn lại |

Báo cáo dùng cột Fill theo trọng số để tận dụng chiều rộng bảng; tiền căn phải, số lượng căn giữa. Theo kỳ bỏ giá trị Đối tượng lặp “Tất cả hóa đơn”; kỳ tháng hiển thị `MM/yyyy`, quý và năm có nhãn phù hợp. Giữ các kỳ rỗng do service trả về, dòng tổng hợp và số liệu gốc. Bộ lọc chính gồm khoảng ngày và chỉ tiêu; độ hạt, cấp sản phẩm, đối tượng và AST nằm trong Thêm bộ lọc. Export luôn dùng request đã áp dụng cho dữ liệu đang xem; sửa control chưa áp dụng không âm thầm thay phạm vi xuất.

## Motion và busy

`Motion.cs` dùng 100 ms cho phản hồi màu và 160 ms cho chuyển trạng thái/tab/panel, ease-out cubic. Nút không đổi kích thước hoặc vị trí khi hover/pressed/busy. Request bắt đầu ngay; không chờ animation, không delay tối thiểu cho loading. Button native tiếp tục giữ focus trong busy, chặn Click trùng và có nhãn thao tác. Các nút cạnh bên vẫn hoạt động theo điều kiện của form.

Spinner vô định phản ánh thời gian request thật. PageView giữ bảng cũ khi refresh, dùng generation + cancellation để bỏ kết quả cũ. Hết thao tác hoặc dispose luôn dừng timer; vùng ẩn dừng animation. Ghi dữ liệu giữ command key/idempotency cũ và không tự retry giao dịch chưa rõ kết quả. Không dùng Thread.Sleep, Wait, Result hoặc DoEvents để tạo animation.

Preference đọc bằng `SystemParametersInfo(SPI_GETCLIENTAREAANIMATION)` và high contrast; không sửa Windows settings. Khi giảm motion, bỏ interpolation/spin nhưng giữ trạng thái, nhãn busy và focus. `ReduceForTesting` chỉ là override nội bộ phục vụ kiểm thử, không đổi setting máy. Không dùng opacity của toàn Form hay giả Panel có opacity.

## UI/UX Pro Max đã áp dụng

Đọc SKILL.md, quick-reference và pro-rules; chạy `--design-system "retail operations desktop tables"`, thử lại `enterprise operations dashboard`. Cả hai sinh pattern Hero + Features + CTA không phù hợp backoffice nên không áp dụng pattern, palette hoặc font tự sinh. Giữ master bàn giao.

Đã chạy các domain UX: `keyboard focus modal`, `error summary validation`, `dense tables filtering` (thử lại `table data sorting`), `loading cancellation feedback`, `reduced motion interruption`. Áp dụng focus dễ thấy, nhãn thật, lỗi tại trường + giữ input, trạng thái tải ổn định, chữ trạng thái bên cạnh màu, request mới nhất thắng, hành vi quay lại giữ ngữ cảnh. Kết quả table chỉ có guidance mobile/web nên chỉ lấy nguyên tắc đọc đủ cột bằng cuộn, không dùng card mobile hoặc bulk action khi backend không hỗ trợ. Không có stack WinForms trong skill; không dùng WPF/WinUI/CSS/ARIA thay native APIs.

Vòng 2 tra thêm `progressive disclosure information hierarchy` và `complex forms progressive disclosure`. Kết quả chỉ cung cấp nguyên tắc UX chung, không có mẫu WinForms phù hợp để áp nguyên trạng. Việc chia ba nhóm, chọn cột mặc định và đưa dữ liệu phụ vào lựa chọn con được quyết định từ phản hồi của Khang, nội dung thực của từng màn và ảnh native sau triển khai.

Đối chiếu kỹ thuật với tài liệu Microsoft: [autoscale](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/forms/autoscale), [DataGridView shared styles](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/best-practices-for-scaling-the-windows-forms-datagridview-control), [native accessibility properties](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/properties-on-windows-forms-controls-that-support-accessibility-guidelines). Giữ PerMonitorV2 đã khai báo trong project, không thêm manifest DPI thứ hai. AccessibleName/Role, TabIndex và mnemonic không được coi là thay cho kiểm Narrator.

## Tiếp tục vòng 3 — phiên làm việc và biểu mẫu phụ

### Hết phiên và khôi phục

`ApiClient.SessionExpired` đưa trạng thái hết phiên về shell. `MainForm` hiện banner Đăng nhập lại, giữ page/cache và dữ liệu đang nhập; `SessionRecoveryForm` cố định email của tài khoản trước đó, kiểm lại ID và vai trò quản trị sau đăng nhập. Khôi phục cùng tài khoản/vai trò giữ nguyên instance trang và bản nháp. Không tự tải lại trang hoặc tự gửi lại request/lệnh đã lỗi; người dùng chọn thao tác tiếp theo.

`sessionVersion` gắn với request bảo vệ phiên mới trước 401 hoặc kết quả session rỗng trả muộn từ phiên cũ. Shell kiểm version trước khi hiện banner đã xếp vào message queue. Nếu vai trò thay đổi, cache cũ bị giải phóng và menu được xây lại theo quyền; nhánh này đã rà source, chưa kiểm bằng việc ghi đổi quyền nhân viên thật.

Review bắt được race khi đóng dialog sau khi server login đã thành công nhưng trước khi tải CSRF cuối hoàn tất. Dialog hiện chặn đóng và vô hiệu “Để sau” trong lúc xác thực, để kết quả còn được đối chiếu danh tính. Khi chưa gửi mật khẩu, “Để sau” vẫn giữ bản nháp và trở về shell. Hộp thoại lệnh lồng đang mở không tự bật đăng nhập lại; người dùng cần trở về shell để khôi phục phiên. Không khẳng định phục hồi input của một command dialog đã bị người dùng đóng.

Ở màn đăng nhập ban đầu, URL API sai trong phần Thiết lập kết nối đang ẩn sẽ tự hiện phần đó, cuộn toàn bộ khung nhập vào vùng nhìn thấy và focus ô URL. Link thiết lập dùng cùng helper. Validation dừng tại client, không gọi API với địa chỉ sai.

### Lịch và bộ chọn

Calendar thêm chọn trực tiếp tháng/năm từ tiêu đề, giữ MinDate/MaxDate của DateTimePicker và chưa đổi giá trị cho đến khi chọn một ngày. PageUp/PageDown giữ ngày trong tháng nếu hợp lệ, cuối tháng được chặn theo số ngày; phím mũi tên dịch từng ngày. Chọn ngày giữ phần giờ. Escape từ chọn tháng trở về ngày, Escape tiếp đóng lịch mà không đổi giá trị. Tên truy cập hai nút trước/sau chuyển thành điều hướng năm trong chế độ chọn tháng.

`MultiLookupPicker` thay checklist ngắn trong form tương thích/serial bằng tìm kiếm, số mục đã chọn và bộ lọc Chỉ mục đã chọn. ID đã chọn vẫn giữ khi tìm kiếm che mục đó; tìm/lọc chỉ đổi cách xem, không tự đánh dấu form có thay đổi nghiệp vụ. `OperationCheckedList` giữ native checked-item/keyboard/accessibility notifications và dùng màu burgundy, hàng chọn nhẹ như web.

`LinesEditor` có Thêm dòng/Xóa dòng và dialog tìm món hàng, vẫn giữ cell editor cũ. Dòng mới dùng ID lookup gốc, số lượng một, còn giá/serial để người dùng nhập. Lỗi số lượng, giá và serial trùng chọn đúng ô lỗi; không làm mất các dòng khác. `MediaMetadataForm` có lỗi dưới nguồn/giấy phép, giữ input, focus trường thiếu và giữ footer nhìn thấy. Không đổi điều kiện dữ liệu hoặc thực hiện upload chỉ để lấy ảnh. So sánh phần ActionSpec/map trong `ModuleActions.cs` với backup vẫn bằng nhau, giữ 34 action và quyền hiện có.

### Chi tiết tài chính và nội dung

Các panel chứng từ/khách hàng phụ giữ đủ trường, cho cuộn đến trường cuối và dùng nhãn/dòng hàng từ DTO. Phân bổ và khoản thu dùng số tiền API trả về; không tính lại tài chính để làm gọn UI. Picker cột có owner đúng khi mở lồng, Enter áp dụng, chặn lựa chọn rỗng và Escape đóng. Filter có vùng đọc lại cuộn/copy đầy đủ, giữ lựa chọn không còn trong lookup, lỗi VND mở đúng ô và Enter áp dụng filter hợp lệ. Biểu đồ giữ mọi kỳ trả về, cuộn được đến cuối, hỗ trợ Home/End và Enter mở kỳ đang chọn. Export tiếp tục dùng query đã áp dụng; Enter tạo bộ file, sau đó bỏ default action để không tạo lại ngoài ý muốn.

Landing giữ lỗi theo đúng section: rời section không hiện lỗi của section khác; quay lại khôi phục lỗi, sửa đúng trường thì xóa lỗi. Validation mở nhóm tương ứng cho poster/model/preset/liên kết sản phẩm theo ràng buộc hiện có. Refresh giữ section, ngôn ngữ và phiên bản lịch sử. Lịch sử có người xuất bản, thời gian và scope 100 phiên bản; chỉ content có selection mới bật restore. Preview đang chờ chặn save/publish/restore/refresh xung đột và nhấn lặp, sau lỗi khôi phục action. Content queue giữ selection qua refresh/filter, đếm riêng title/description thiếu theo từng ngôn ngữ. Detail dài dùng vùng văn bản có nhãn, chọn/copy và cuộn tới cuối. Bộ kiểm content dùng snapshot API thật rồi phát lại trong handler local cho trường hợp dài/thiếu/lỗi; chặn mọi business command, không cấp preview grant thật hoặc mở trình duyệt.

### Bằng chứng vòng 3 — chức năng/layout, không thay nghiệm thu thị giác

| Bộ kiểm | Kết quả | Phạm vi |
| --- | --- | --- |
| Shared native | **28 PASS, 6 PNG** tại `artifacts/winforms/admin-refinement-shared-v3/result.json` | 12 kiểm lịch, 3 kiểm URL đăng nhập/layout chỉ ở client, 5 kiểm HTTP/auth race có kiểm soát, 8 kiểm phiên với API local thật |
| Operations / bốn vai trò ở thời điểm chạy | **61 PASS, 54 PNG**, exit 0 tại `artifacts/winforms/admin-refinement-operation-v3-2/smoke.json` | Giữ lựa chọn ẩn, tìm/chọn SKU đúng ID, thêm/xóa dòng, focus ô lỗi, media input và restore theo quyền/selection |
| Footer bộ chọn hàng | **61 assertion hình học/hành vi PASS, 2 PNG** tại `artifacts/winforms/admin-refinement-operation-footer-v3/result.json` | Kích thước thật 440×380 và 680×540; cả Chọn món/Hủy nhìn thấy đủ, cùng hàng, không bị parent cắt và hit-test đúng |
| Finance refinement | **40 PASS, 19 PNG** tại `artifacts/winforms/admin-refinement-finance-v3/after/finance-refinement.json` | Dialog phụ, trường cuối, filter, biểu đồ đủ kỳ và bàn phím/export |
| Finance regression | **48 PASS, 1 chưa xác minh, 16 PNG** tại `artifacts/winforms/admin-refinement-finance-v3/regression/finance-native.json` | Query/tổng/scope/AST/cache/dispose; SaveFileDialog chưa xác minh hoàn tất |
| Content refinement | **28 PASS, 12 PNG** tại `artifacts/winforms/admin-refinement-content-v3/run-5/result.json` | Validation theo section, preview guard, lịch sử/selection, nội dung dài và compact |
| Release vòng 3 — full | **35 PASS, 26 PNG** tại `artifacts/winforms/admin-refinement-final/smoke.json` | Chạy trực tiếp từ executable đã publish; kết thúc 23:38:53 giờ Việt Nam ngày 17/11/2026 |
| Release vòng 3 — bốn vai trò | **61 PASS, 54 PNG** tại `artifacts/winforms/admin-refinement-roles-final/smoke.json` | Chạy trực tiếp từ executable đã publish; kết thúc 23:39:07 giờ Việt Nam ngày 17/11/2026 |

Build/publish vòng 3 **0 warning / 0 error**. Hai bộ executable đạt tổng **96 PASS, 80 PNG `DrawToBitmap`**. Manifest được lưu riêng tại `artifacts/admin/build-manifest-round3.json`, ghi **41 source/asset**; kiểm tại thời điểm đó xác nhận **0 source hash mismatch**, DLL khớp. GUI đăng nhập visible được lưu tại `artifacts/admin/running-admin-round3.json`; điều này không chứng minh các góc nút được vẽ đúng trên desktop. API `5080/health` và web `3000/vi` trả HTTP 200 lúc bàn giao vòng 3. Đường dẫn publish hiện chứa vòng 4; hash/kết quả vòng 3 chỉ là lịch sử.

Shared kiểm logout/401 rồi đăng nhập lại cùng account/role, giữ page/CMS draft, hủy trước khi gửi giữ draft và server draft không đổi. HTTP có kiểm soát chứng minh 401/session-null cũ không xóa actor mới và dialog không đóng trong lúc final CSRF đang chờ. [README shared](../artifacts/winforms/admin-refinement-shared-v3/README.md) phân biệt live API và phản hồi giả lập. [README operations](../artifacts/winforms/admin-refinement-operation-v3-2/README.md) ghi thay đổi, contract và ảnh đã xem. Lượt operations đầu được giữ với failure do assertion restore cũ, không coi đó là PASS; assertion sau sửa đòi hỏi content chọn history mới bật restore, owner luôn không được ghi.

Review ảnh picker bắt thêm nút Hủy bị flow layout đẩy xuống khỏi footer. Source đã tắt wrapping cho hàng hành động và dành phần rộng còn lại cho số kết quả; harness footer riêng đã xác minh kích thước thật, bounds và hit-test của hai nút, gồm trạng thái không khớp tìm kiếm. Hai ảnh mới ở [evidence footer](../artifacts/winforms/admin-refinement-operation-footer-v3/README.md) thay cho ảnh picker trung gian bị lỗi bố cục.

Các kiểm này không submit business command; session login/logout có thật. CMS input chỉ sửa local; bản nháp/lịch sử server không thay đổi. DPI thực vẫn 120 (125%); DPI vật lý 100%/150%, Narrator, mất mạng trong giao dịch ghi và hoàn tất native SaveFileDialog còn chưa xác minh. Các khoảng thiếu API/workflow baseline tiếp tục giữ trong tracking.

## Vòng 4 — sidebar sáng, control và bảng; đã kiểm Release cuối

[Ảnh desktop trước sửa](../artifacts/winforms/live-ui-v4/user-window-before.png) cho thấy lỗi góc trắng/viền bẩn mà các ảnh `DrawToBitmap` trước không phản ánh. `BzButton` đã tắt `Opaque`, bật `SupportsTransparentBackColor` và đặt nền trong suốt để parent được vẽ lại ngoài đường bo; màu primary chỉ nằm trong đường bo. Focus, busy và bố cục nút được giữ, không đổi kích thước khi hover/pressed.

Sidebar hiện dùng nền sáng `NavSurface #FFFFFF` theo yêu cầu mới nhất của Khang, chữ tối và nhấn burgundy. `NavigationLevel` phân biệt root/group/child: nhóm dùng Segoe UI Semibold 9.25 pt, màu phụ và chevron bên phải; module con dùng chữ 10 pt, thụt thêm 26 logical px, không lặp icon. Chỉ child đang chọn có nền/vạch nhấn trong chế độ rộng. Thu gọn giữ icon/menu và dấu chọn mục hiện tại; metadata module, lọc quyền, Ctrl+K và cache page không đổi.

Dropdown thường và combo trong DataGridView dùng chung `ComboPopupChrome`, có popup native bo góc/viền `Ui.Line`, vẫn giữ keyboard, selection, commit/cancel và binding raw ID qua `DisplayMember`/`ValueMember`. `GridComboBox.cs` v3 sửa focus kép, paint dòng mới, shared row/getter, padding mặc định; độ rộng popup được đo sau handler native của DataGridView để không bị ghi đè. Date picker giữ segment native có thể sửa bằng bàn phím khi focus, chỉ đồng bộ phần nút lịch. `Ui.Bar` căn các nút theo đáy hàng input. Label thường/nav/combo giữ dấu `&` nguyên văn; `Ui.Field` vẫn bật mnemonic cho caption truy cập bằng Alt. Tổng quan compact giảm chiều cao phần KPI/thao tác để hiển thị đủ ba dòng dữ liệu.

Các bảng module giữ tối đa sáu cột chính nhưng đủ phân biệt hồ sơ: trả hàng dùng `state` đúng contract cùng tham chiếu/đơn/lý do/ngày; hoàn tiền dùng `completed_at`; thu cũ hiện cách tiếp nhận; tồn kho thêm mã lô; audit có người thực hiện và hồ sơ liên quan. UUID được rút gọn khi vẽ, đầy đủ trong tooltip/detail và dữ liệu gốc dùng cho selection/lệnh. Enum `defective`, `wrong_item`, `store`, `remote` và các trạng thái đã biết được Việt hóa; không sửa free text. Overlay rỗng chỉ hiện sau response thành công không có dòng; ẩn khi tải/lỗi/có dữ liệu, Status vẫn hiện lỗi. Không thêm hồ sơ giả, không thay API/schema hoặc công thức backend.

| Bộ kiểm vòng 4 | Kết quả / bằng chứng | Binary và giới hạn |
| --- | --- | --- |
| Full shell sáng, 23 module owner | **208 PASS, 38 PNG `CopyFromScreen`** — [result](../artifacts/winforms/live-ui-v4/full-shell-light-review/result.json) | DPI thực 120; HWND/foreground/occlusion có guard; hash `40810E21F4FAA42ABA640B809DB080A9A54FD7C126C3D4556B6697AB6D196BBB`, trước sửa cuối grid |
| Module columns / ID / empty state | **70 PASS** — [result](../artifacts/winforms/live-ui-v4/operation-columns/result.json) | Fixture từ GET thật 17 module, 21 request GET giả lập; không network hoặc ảnh nghiệm thu trong harness; raw ID/selection/detail và empty→loading→error→data |
| Component Release cuối | **91 PASS, 16 PNG `CopyFromScreen`** — [result](../artifacts/winforms/live-ui-v4/after-final-light/result.json) | Đúng DLL `5B9BF00E…`; kiểm trạng thái nút/focus/busy và repaint |
| Grid combo Release cuối v3 | **22 PASS, 8 PNG `CopyFromScreen`** — [result](../artifacts/winforms/live-gridcombo-v4/final-release-v3/result.json) | Đúng DLL `5B9BF00E…`; popup/paint/binding/keyboard |
| Shell ưu tiên / dropdown / date trên Release cuối | **43 PASS, 7 PNG CopyFromScreen** — [result](../artifacts/winforms/live-ui-v4/full-shell-final/result.json) | Đúng DLL cuối 5B9BF00E…; không thay bằng 208 PASS/38 ảnh của binary cũ |

Release cuối hiện có SHA-256 DLL **`5B9BF00E69BAC03CF1B1F87DF67DD0893BDBF3FCAC6645592DD5DE3AEE841AB9`**, [manifest vòng 4](../artifacts/admin/build-manifest.json) ghi **42 source/assets**. Các kết quả cuối đã đối chiếu đúng hash này. Executable đã được mở trên desktop Default, đăng nhập owner qua Login bình thường, chụp [ảnh cửa sổ thực tế](../artifacts/winforms/live-ui-v4/published-app-live.png) và ghi [runtime](../artifacts/admin/running-admin.json). Một lượt helper kiểm thử crash ở bootstrap riêng được giữ trong lịch sử kiểm, không phải crash khởi động BotZone; lượt đó và các lượt không được cấp foreground không tính PASS. Khang đã cho phép kiểm trực tiếp desktop; không tự xác nhận lưu hoặc bỏ bản nháp. Các lượt kiểm không lưu nghiệp vụ; có auth login/logout, API đọc và input local. DPI vật lý khác 120, Narrator toàn luồng, SaveFileDialog hoàn tất và mutation chưa chạy giữ trạng thái chưa xác minh.

## Kiểm và cách chạy

Build: `dotnet build src/BotZone.Admin.WinForms/BotZone.Admin.WinForms.csproj --no-restore`.

Đóng gói local: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-admin.ps1 -Publish`.

Chạy: `artifacts/admin/win-x64/BotZone.Admin.WinForms.exe`. API theo cấu hình local hiện có hoặc biến `BOTZONE_API_URL`. Màn đăng nhập có thiết lập kết nối; không lưu mật khẩu/cookie xuống đĩa.

Kết quả build/runtime, ảnh, ma trận motion và giới hạn DPI được cập nhật trong tracking sau khi chạy. Ảnh HTML không được dùng làm bằng chứng native; PNG `DrawToBitmap` cũng không được dùng thay cho kiểm repaint và nền góc trên desktop thật.

### Bằng chứng vòng 2 — lịch sử

| Bộ kiểm | Trạng thái đã xác nhận | Bằng chứng |
| --- | --- | --- |
| Finance native sau thay đổi phân lớp thông tin | 48 PASS, 1 mục bỏ qua có lý do | `artifacts/winforms/admin-redesign-finance-v2/finance-native.json` |
| Calendar và dropdown native | 9 PASS | `artifacts/winforms/admin-disclosure-controls/result.json` |
| Full smoke bản đóng gói vòng 2 | 35/35 PASS, 26 ảnh native | `artifacts/winforms/admin-density-final/smoke.json` |
| Bốn vai trò / lifecycle / dirty guard bản đóng gói vòng 2 | 56/56 PASS, 49 ảnh native | `artifacts/winforms/admin-density-roles-final/smoke.json` |

Finance kiểm số nhóm/cột mặc định, khả năng mở dữ liệu phụ, count của receipt/credit note/refund/attempts và lịch sử khách, sáu báo cáo nghiệp vụ, AST AND/OR/NOT, tổng API, giữ selection/cache, request mới nhất thắng và đóng khi đang tải. Ảnh compact/wide nằm cùng thư mục evidence; danh sách hóa đơn và báo cáo được kiểm ở 1366×768 và 1920×1080. XLSX/PDF được tải thật qua API, kiểm chữ ký file và phạm vi request đã áp dụng. Mục bỏ qua là xác nhận lưu bằng native SaveFileDialog: Windows không cấp foreground cho automation. Không coi API download là đã nghiệm thu nút Lưu của dialog này.

Chín kiểm control xác nhận mở lịch, giới hạn ngày, giữ phần giờ khi chọn, đóng sau chọn, Escape giữ giá trị, handle/vùng bo góc của dropdown native và dispose an toàn khi lịch đang mở.

Bản Release vòng 2 đã được chạy lại tại đường dẫn publish dùng chung `artifacts/admin/win-x64/` sau khi căn nút bộ lọc ngang hàng input và sửa nền bo góc của khung nhập. Full smoke hoàn tất lúc 17:03:59 giờ Việt Nam ngày 17/11/2026; kiểm vai trò hoàn tất lúc 17:04:15 cùng ngày. Hai lượt tạo tổng cộng **75 ảnh PNG native**. Manifest vòng 2 được lưu riêng tại `artifacts/admin/build-manifest-round2.json`, ghi SHA-256 của executable/DLL và 39 source/asset của lượt đó. Đường dẫn publish dùng chung hiện chứa bản vòng 4; archive vòng 2 chỉ dùng đối chiếu lịch sử.

### Lịch sử bàn giao vòng 1

Bản Release vòng 1 ngày 17/11/2026 đã được chạy trực tiếp từ executable trong `artifacts/admin/win-x64/` ở thời điểm bàn giao đó. Full smoke đạt 35/35 tại `artifacts/winforms/admin-redesign-final/`; bốn vai trò/lifecycle/dirty guard đạt 41/41 tại `artifacts/winforms/admin-redesign-roles-final/`. Hai lượt có tổng cộng 52 ảnh native. Manifest vòng 1 được lưu riêng tại `artifacts/admin/build-manifest-round1.json`. Kiểm tài chính vòng 1 đạt 16 mục, 1 mục native SaveFileDialog chưa xác minh vì giới hạn foreground. Web logo production đạt 16 kiểm tra tại `artifacts/web/brand-redesign/`.

### Giới hạn nghiệm thu

Các lượt kiểm đã nêu dùng monitor thực DPI 120 (125%); kích thước cửa sổ compact/wide không thay cho kiểm DPI. Vòng 4 có bằng chứng desktop thật theo hash, shell của Release cuối đã đạt 43 PASS/7 ảnh; không dùng ảnh `DrawToBitmap` để nghiệm thu repaint. Chưa nghiệm thu manual DPI 100%/150%, Narrator, hoặc tình huống mất kết nối giữa giao dịch ghi. Test dirty CMS chỉ sửa input local rồi giữ/bỏ; bản nháp và lịch sử server vẫn r3. Kiểm finance không tạo giao dịch tài chính hoặc thay đổi dữ liệu nghiệp vụ. Khoảng thiếu API/business workflow từ baseline tiếp tục được liệt kê trong tracking, không tuyên bố đã triển khai bằng UI.
