# BotZone Admin — Đặc tả redesign WinForms

**Phiên bản:** 1.0 · **Ngày:** 17/11/2026 · **Phạm vi:** đăng nhập và toàn bộ quản trị cho bốn vai trò trên laptop/PC Windows.

## 1. Mục tiêu và ranh giới

Đưa BotZone từ giao diện quản trị khó sử dụng thành một ứng dụng desktop rõ ràng, nhất quán và có chất lượng thị giác tốt: người dùng nhận ra việc cần làm, mở đúng dữ liệu, hoàn thành thao tác và biết kết quả. Điểm nhấn là bảng dữ liệu, thứ bậc chữ, trạng thái và luồng công việc; màu burgundy tạo nhận diện vừa đủ.

Đây là **đề xuất thiết kế chưa triển khai**. Nhóm nghiên cứu chưa quan sát màn WinForms hiện tại chạy trên máy người dùng. BA v1.1 là nguồn phạm vi; thông tin .NET 10/ASP.NET API/PostgreSQL là kiến trúc đã chốt trong BA, chưa phải kết quả xác minh mã nguồn hiện hành. Codex phải kiểm repository, đối chiếu từng form/service và chụp baseline trước khi sửa.

Nghiệp vụ hiện tại được người dùng đánh giá tạm ổn. Redesign không tự đổi schema, công thức tiền, trạng thái, quyền, API hoặc tạo backend khác. Giao diện quản trị là **C# Windows Forms native**; không thay bằng React nhúng WebView2. Website bán hàng và model 3D đã bàn giao thuộc phạm vi liên quan, không phải phần dựng lại trong tác vụ này.

Admin ưu tiên **laptop và PC Windows**, không có giao diện admin mobile. Lựa chọn viewport mobile trong CMS chỉ để xem website bán hàng. Admin dùng tiếng Việt, tiền báo cáo VND, giờ hiển thị Việt Nam theo BA. Nội dung sản phẩm/landing vẫn biên tập VI–EN.

## 2. Cơ sở thiết kế

| Tham khảo chính thức | Pattern chọn áp dụng |
|---|---|
| [Business Central Role Center](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-designing-role-centers) | Trang đầu và điều hướng theo công việc mỗi vai trò; chỉ số dẫn đến dữ liệu cần xử lý. |
| [Shopify order views](https://help.shopify.com/en/manual/fulfillment/managing-orders/viewing-orders/searching-orders) | Lưu bộ lọc/cột/sort thành chế độ xem; danh sách hỗ trợ thao tác lặp. |
| [Odoo inventory adjustments](https://www.odoo.com/documentation/19.0/applications/inventory_and_mrp/inventory/management/inventory_adjustments.html) | Tách sổ sách, thực đếm, chênh lệch và hành động áp dụng; giữ truy vết. |
| [ERPNext sales analytics](https://docs.frappe.io/erpnext/sales-analytics) | Chọn báo cáo theo câu hỏi; định nghĩa nguồn, kỳ, trạng thái và drill-down. |
| [Shopify theme editor](https://help.shopify.com/en/manual/online-store/themes/customizing-themes/theme-editor/features-overview) | Cây section, thuộc tính theo lựa chọn, preview phù hợp diện tích. |
| [Strapi Draft & Publish](https://docs.strapi.io/cms/features/draft-and-publish) | Phân biệt chưa lưu, bản nháp, đã xuất bản và thay đổi chưa xuất bản. |

Các nguồn được nghiên cứu qua tài liệu chính thức, không coi đó là thử nghiệm live app. Học nguyên tắc thao tác, không chép mã nguồn hoặc giao diện web nguyên khối. Chi tiết nguồn, giới hạn và diễn giải nằm trong `07_RESEARCH_AND_DECISIONS.md`.

## 3. Hệ thiết kế thống nhất

### 3.1 Màu, chữ và hình khối

| Token | Giá trị | Sử dụng |
|---|---|---|
| Main | `#FAFAF9` | Nền vùng làm việc |
| Surface | `#FFFFFF` | Form, bảng, panel |
| Sidebar | `#19191C` | Điều hướng tối |
| Text | `#202126` | Nội dung chính |
| Muted | `#63656D` | Chú thích và thông tin phụ |
| Divider | `#E2E3E7` | Đường chia và viền surface |
| ControlBorder | `#8A8C95` | Viền input cần nhận biết rõ |
| Burgundy | `#6F233F` | Hành động chính, lựa chọn thương hiệu |
| BurgundySoft | `#F5EBEF` | Nền nhấn nhẹ |
| Focus | `#2459B8` | Viền focus bàn phím |

Font **Segoe UI 10.5–11 pt** cho nội dung; tiêu đề trang 19–22 pt; nhãn nhóm 10–11 pt đậm vừa; số liệu chính 22–26 pt. Số tiền căn phải, mã/serial không bị rút gọn mất ký tự khi cần thao tác. Không dùng toàn chữ hoa cho câu dài. Mockup dùng Noto Sans để xuất tài liệu trong môi trường hiện tại; runtime dùng Segoe UI theo tokens.

Khoảng cách theo bội số 4/8; padding trang 24, nhóm 16, label–field 6–8. Radius 6–8 cho nút/panel phù hợp, không bo tròn mọi thành phần. Bảng dùng đường chia nhẹ, nền chọn rõ; tránh lưới đen dày. Shadow chỉ dành cho menu nổi/dialog nếu cần; không gradient hoặc ảnh nền gây nhiễu vùng dữ liệu.

Màu thành công/cảnh báo/lỗi là semantic token riêng, chỉ dùng ở vùng cần chú ý, luôn kèm nhãn/icon. Burgundy không đại diện trạng thái lỗi. Kiểm tra độ tương phản thực tế của mọi cặp token và focus; không suy từ màu đẹp rằng đã dễ đọc.

### 3.2 Control dùng lại

Một bộ component chung gồm: PageHeader, NavigationItem, Primary/Secondary/DangerButton, SearchBar, FilterChip, StatusBadge, PagedGrid, DetailHeader, MoneySummary, Timeline, Empty/ErrorPanel, ValidationSummary, ConfirmationDialog và ExportDialog. Các tên này là trách nhiệm thiết kế, Codex ánh xạ vào cấu trúc C# hiện có.

Nút thường cao 36 logical px, nút chính và input 40; header bảng 40, hàng bảng tiêu chuẩn 48 và hàng hai dòng 64 theo `design-tokens.json`. Biến thể gọn chỉ giảm khoảng trống sau khi kiểm tiếng Việt, bàn phím và DPI, không giảm cỡ chữ mặc định. Nút luôn có chữ khi ý nghĩa không hiển nhiên. Field có label thật; placeholder chỉ làm ví dụ. Error nằm cạnh field, có tổng lỗi khi form dài và đưa focus tới lỗi đầu tiên.

## 4. Shell, resize và điều hướng

MainForm giữ hành vi cửa sổ Windows: minimize/maximize/resize, focus và hệ thống bàn phím. Sidebar chuẩn **224**, thu gọn **64**; header khoảng **64**; status bar **24**. Các kích thước là logical pixel tại 96 DPI; dùng layout container, Dock/Anchor và scaling, không đặt tọa độ cứng cho toàn màn.

Breakpoint được tính theo **client width đã quy về logical 96 DPI**, gồm sidebar; không dùng số pixel vật lý màn hình:

| Client width | Hành vi |
|---|---|
| Từ 1440 | Có thể dùng bảng + detail 380–420 cạnh nhau nếu đủ chiều rộng cột ưu tiên. |
| 1120–1439 | Bảng ưu tiên toàn vùng; detail mở theo yêu cầu, không ép bảng hẹp cố định. |
| 900–1119 | Sidebar thu gọn; detail thành trang con đầy đủ với Quay lại và giữ ngữ cảnh danh sách. |
| Dưới 900 | Bố cục an toàn: form cuộn, grid cuộn ngang khi cần, action chính luôn tới được; đây không phải chế độ mobile. |

Header chứa breadcrumb/tên module, tìm trong phạm vi phù hợp và action của trang. Status bar hiển thị kết nối, trạng thái đồng bộ gần nhất nếu có, tài khoản/role; không đưa connection string vào UI. Thông báo mới có liên quan dẫn đến hồ sơ cụ thể, không tạo một chuông giả.

Chọn menu giữ filter, sort, trang, dòng đang chọn của module. Quay lại từ chi tiết không về đầu danh sách vô cớ. Một module mở trong vùng nội dung; dialog có owner cho thao tác ngắn, tránh nhiều cửa sổ độc lập chồng lên nhau. Phím tắt xuất hiện trên menu/tooltip và không cướp phím của ô quét hoặc editor.

## 5. Đăng nhập dùng chung — LG-01

Ở cửa sổ rộng, bố cục hai vùng: brand tối tiết chế và form sáng; form rộng khoảng 360–448 logical px (mẫu rộng dùng 448). Brand có logo/chữ BotZone, tên “Quản trị cửa hàng” và một câu định vị ngắn; không chạy slideshow/3D. Khi hẹp, phần brand co thành header để dành chỗ cho form.

Form gồm tên tài khoản/email theo cơ chế hiện có, mật khẩu, hiện/ẩn mật khẩu, “Ghi nhớ tài khoản” nếu được hỗ trợ, nút Đăng nhập. Ghi nhớ mặc định chỉ tên đăng nhập; không tự thêm lưu mật khẩu. Không yêu cầu chọn role trước khi xác thực. Hệ thống đọc role/quyền sau đăng nhập và mở đúng bàn làm việc.

Có Caps Lock, Tab order từ trên xuống, Enter gửi, focus rõ. Đang xác thực giữ kích thước nút và chặn gửi trùng; lỗi đăng nhập ở gần form. Mất kết nối API khác sai thông tin đăng nhập, có Thử lại. Không lộ nội dung exception/SQL. Đổi mật khẩu/khôi phục chỉ hiện nếu luồng đã tồn tại, không gắn nút chết.

Phiên hết hạn đưa về đăng nhập có giải thích và giữ nháp chưa gửi trong phạm vi an toàn. Sau xác thực lại kiểm quyền trước khi tiếp tục. Nút đổi role trong mockup chỉ được ghi “Xem thiết kế”, không trở thành cơ chế phân quyền thật.

## 6. Quyền và điều hướng theo vai trò

Menu là góc nhìn của role; API vẫn kiểm quyền. Ẩn chức năng hoàn toàn không thuộc nhiệm vụ, còn hành động tạm bị chặn do trạng thái thì giải thích lý do. Không suy luận Owner luôn được mọi quyền.

| Nghiệp vụ | Chủ cửa hàng | Xử lý đơn | Kho | Nội dung |
|---|---|---|---|---|
| Kinh doanh/lãi gộp | Toàn bộ | KPI đơn, không giá vốn | KPI kho | KPI nội dung |
| Nhân viên/quyền/cấu hình | Quản lý | Không | Không | Không |
| SKU/giá/trạng thái bán | Quản lý | Xem | Xem | Xem |
| Mô tả/ảnh/SEO VI–EN | Xem | Xem | Xem | Biên tập |
| Landing sửa/publish/restore | **Chỉ xem** | Không | Không | Thực hiện |
| Đơn/liên hệ khách | Giám sát/ngoại lệ | Thực hiện | Đủ để đóng gói | Không |
| Serial/xuất/kiểm định | Giám sát/ngoại lệ | Xem kết quả | Thực hiện | Không |
| Nhập/kiểm kê | Duyệt, xem giá vốn | Không | Theo phiếu | Không |
| Giá vốn/tồn ngoại lệ | Duyệt | Không | Đề nghị | Không |
| Hậu mãi/thu cũ/Care | Duyệt chính sách/tài chính | Tiếp nhận/điều phối | Nhận/kiểm tra | Nội dung giới thiệu |
| Xác nhận banking/COD demo | Có | Đúng trạng thái/số tiền | Không | Không |
| Hoàn tiền | Duyệt/thực hiện | Đề nghị/theo dõi | Kết quả kiểm tra | Không |
| Voucher/tỷ giá/điểm/hạng | Cấu hình | Tra cứu | Không | Dùng chiến dịch đã duyệt |
| Hóa đơn/lọc/xuất tài chính | Toàn bộ | Phục vụ xử lý đơn | Chỉ phiếu đóng gói | Không |
| Thống kê hóa đơn | Toàn bộ | Chỉ nhân viên hiện tại | Không | Không |
| Nhật ký | Toàn bộ | Nghiệp vụ được phép | Nghiệp vụ kho | Nghiệp vụ nội dung |

Kho có thể xem chi phí phiếu nhập được giao theo quyền riêng, không đồng nghĩa sửa giá vốn hoặc xem lãi gộp. Nếu quyền thay đổi trong phiên, cập nhật trước lần ghi/xuất tiếp theo.

## 7. Bốn bàn làm việc

### OW-01 — Chủ cửa hàng

Trang “Tổng quan cửa hàng” trả lời việc gì cần quyết định và cửa hàng đang vận hành ra sao. Header có kỳ, VND, thời điểm dữ liệu. Dòng chỉ số trong mẫu chốt gồm bốn mục: Tổng HĐ phát hành, Điều chỉnh giảm, Ròng phát sinh kỳ, Cần quyết định. Tiền đã thu và Lãi gộp hàng bán có báo cáo riêng từ cùng điều hướng; không thay thế ba chỉ số hóa đơn bằng các khái niệm khác. Mỗi mục có định nghĩa và drill-down; không dùng số viết tay.

Vùng chính là “Cần quyết định”: đề nghị hoàn, báo giá thu cũ vượt thẩm quyền, chênh lệch tồn/giá vốn, giao thất bại cần xử lý. Hàng có mã, người đề nghị, thời gian chờ, giá trị và lý do; mở chi tiết bằng một thao tác. Một biểu đồ kỳ và bảng mặt hàng cần chú ý hỗ trợ quyết định, không lấn hàng chờ.

Điều hướng nhóm: Tổng quan; Bán hàng; Kho & nhập hàng; Sản phẩm; Khuyến mãi & thành viên; Hậu mãi; Báo cáo; Nhân viên & quyền; Nhật ký; Cài đặt. Nội dung/landing chỉ có chế độ xem. Luồng duyệt: mở hồ sơ → đọc bằng chứng → xem tác động tiền/tồn → xác nhận → kết quả/audit → trở lại đúng hàng chờ.

### OR-01 — Nhân viên xử lý đơn

Trang đầu “Đơn cần xử lý” có các view theo trạng thái thực tế: Chờ xác nhận, Chờ thanh toán, Chờ kho, Đang giao, Có vấn đề. Cột ưu tiên gồm mã/giờ, khách, tóm tắt hàng, tổng, thanh toán, giao/xử lý, người phụ trách. Mã và tiền không bị cắt; cột ít dùng cho cấu hình.

Một dòng mở preview nhanh; Enter/nút Xem chi tiết vào bản đầy đủ. Panel chi tiết giữ mã, trạng thái, khách, hàng, tài chính theo quyền và timeline. Chỉ một CTA chính cho bước tiếp theo hợp lệ từ API. “Đã giao” và “Chưa đối soát COD” có thể cùng tồn tại; không gộp thành một trạng thái mơ hồ.

Tìm nhanh hỗ trợ mã, khách, điện thoại theo khả năng server; filter lưu thành view. Khi chọn nhiều, thanh action ghi đúng số dòng và phạm vi. Batch hỗn hợp báo phần đủ điều kiện, phần lỗi và cách tiếp tục; không thao tác lại giao dịch đã thành công.

Menu: Công việc/Đơn hàng; Hóa đơn; Khách hàng; Hậu mãi; Tra cứu sản phẩm/tồn; Báo cáo của tôi; Nhật ký. Hủy trước bàn giao khác xử lý giao thất bại sau bàn giao. Nhân viên đề nghị hoàn, không tự nhận thêm quyền duyệt.

### WH-01 — Nhân viên kho

Trang “Công việc kho” ưu tiên phiếu Xuất, Nhập, Kiểm định, Kiểm kê được giao. Một kho nên không có bộ chọn đa kho giả. Phiếu ghi mã nguồn, trạng thái, tiến độ món và bước tiếp theo; thông tin khách chỉ đủ cho nhiệm vụ.

Phiếu xuất có ô quét/nhập IMEI/serial dễ thấy, giữ focus sau lần quét hợp lệ. Bảng hiển thị SKU, số lượng yêu cầu/đã chuẩn bị, mã ghi nhận và vấn đề. Quét trùng/sai SKU/chưa khả dụng báo cạnh field và dòng, giữ mã để sửa. Phụ kiện dùng số lượng/lô, không ép nhập IMEI. Bước hoàn tất chỉ chạy command hiện có sau kiểm server.

Nhập hàng phân biệt dự kiến/đã nhận/còn thiếu và nhận một phần. Kiểm kê có Sổ sách/Thực đếm/Chênh lệch; chưa đếm không hiển thị thành 0. Lưu kiểm đếm khác đề nghị áp chênh lệch. Kho không tự duyệt ngoại lệ, hàng trả chưa kiểm tra không về tồn bán.

Menu: Công việc kho; Tồn & serial; Nhập hàng; Kiểm kê; Kiểm định hậu mãi; Lịch sử kho. Tra cứu serial hiển thị lịch sử và loại tài sản: hàng mới, hàng trả chờ kiểm tra, hàng thu cũ. Không trộn ba nguồn thành một số tồn sửa tay.

### CT-01 — Nhân viên nội dung

Trang “Việc nội dung” có view Cần hoàn thiện, Có thay đổi chưa xuất bản, Đã xuất bản. Bảng nêu đúng thiếu sót như “Thiếu mô tả EN”, “Chưa chọn poster”, “CTA chưa có đích”; bấm dẫn đến field liên quan. Không chấm một điểm chất lượng giả.

Menu: Việc nội dung; Nội dung sản phẩm; Landing page; Thư viện media; Lịch sử nội dung. Giá/tồn/thông tin kinh doanh là read-only theo BA. VI/EN là ngôn ngữ nội dung, không đổi UI admin.

Landing studio: thanh tên/trạng thái/ngôn ngữ cùng Lưu nháp, Xem trước, Xuất bản; cây section ở trái; thuộc tính section ở giữa; ngữ cảnh/thumbnail khi đủ rộng. Với laptop hẹp dùng tab thay ba cột ép nhỏ. Sắp xếp section bằng Lên/Xuống; drag chỉ bổ sung. CTA chọn sản phẩm/route; template quyết định cấu trúc trình bày.

Người biên tập chọn preset trước; Nâng cao chỉ mở nhóm thông số có giới hạn từ schema, có Khôi phục mặc định. Giữ model v003 và contract Duo; không biến CMS thành công cụ sửa shader. Preview mặc định mở renderer storefront trong trình duyệt, đúng bản nháp và quyền giới hạn theo API hiện có. WebView2 nếu dùng chỉ chứa preview/PDF.

Phân biệt Chưa lưu, Đang lưu, Đã lưu nháp, Có thay đổi chưa xuất bản, Đã xuất bản. Xuất bản nêu rõ trang/phạm vi ngôn ngữ; lỗi giữ draft và bản công khai hiện tại. Lịch sử/khôi phục theo BA, restore tạo phiên bản mới. Media đang dùng có danh sách tham chiếu, không xóa âm thầm. Nếu repo thiếu chức năng BA, ghi gap; không dựng nút giả hoặc tự thêm migration trong redesign.

## 8. Danh mục màn hình đầy đủ

“Ưu tiên” dưới đây là mức xuất hiện trong điều hướng/hàng chờ, **không phải cắt phạm vi triển khai**. A: vào thường xuyên; B: theo hồ sơ/ngữ cảnh; C: cấu hình/giám sát. Các màn có thể là UserControl, trang con hoặc dialog phù hợp.

| ID | Màn/nhóm màn | Role và mức ưu tiên |
|---|---|---|
| LG-01/02 | Đăng nhập; phiên hết hạn/đăng nhập lại | Cả bốn · A/B |
| SH-01 | Shell, điều hướng, tìm, tài khoản/kết nối | Cả bốn · A |
| OW-01/02 | Tổng quan; hồ sơ cần chủ duyệt | Owner · A |
| OR-01/02 | Danh sách đơn; chi tiết/timeline | Order · A; Owner · B |
| OR-03 | Thanh toán demo/COD/banking/payment attempts | Order/Owner · B |
| OR-04 | Giao nhận, vận đơn, thất bại/gửi lại | Order/Owner · B; Kho theo nhiệm vụ |
| IN-01/02 | Danh sách hóa đơn; chi tiết/chứng từ giảm | Owner/Order đúng scope · A/B |
| IN-03/04 | Bộ lọc nâng cao; xuất XLSX/PDF | Owner/Order đúng scope · B |
| WH-01/02 | Công việc kho; phiếu xuất/đóng gói | Kho · A; Owner giám sát |
| WH-03/04 | Tồn/sổ tồn; serial/IMEI và lịch sử | Kho · A; Owner/Order tra cứu theo quyền |
| WH-05/06 | Kiểm kê; đề nghị/duyệt điều chỉnh | Kho đề nghị, Owner duyệt · B |
| PU-01/02 | Nhà cung cấp; đơn mua/phiếu nhập | Owner quản lý, Kho theo phiếu · B |
| PU-03 | Nhận đủ/một phần, serial/lot/giá vốn | Kho theo quyền; Owner · B |
| CA-01/02 | Model/nhóm/thuộc tính; SKU/giá/trạng thái | Owner quản lý; role khác xem · B |
| CA-03 | Tương thích phụ kiện | Owner quản lý; role khác xem · B |
| CU-01/02 | Khách hàng; chi tiết đơn/điểm/credit/hậu mãi | Order/Owner · A/B |
| PR-01/02 | Voucher và phiên bản; lượt dùng/điều kiện | Owner quản lý, Order tra cứu · B |
| PR-03 | Chính sách điểm/hạng và sổ phát sinh | Owner; Order tra cứu · B/C |
| AS-01/02 | Hàng chờ hậu mãi; trả hàng từng món/bằng chứng | Order/Owner; Kho kiểm tra · A/B |
| AS-03 | Đề nghị, duyệt, thực hiện/theo dõi hoàn | Theo ma trận quyền · B |
| TI-01/02 | Thu cũ; tiếp nhận/kiểm định/checklist | Order/Owner/Kho theo bước · B |
| TI-03/04 | Báo giá phiên bản/phản hồi; sổ credit/gửi trả | Theo ma trận quyền · B |
| WC-01/02 | Gói/hợp đồng Care; tiếp nhận/xử lý/trả máy | Owner/Order/Kho theo bước · B |
| WC-03 | Sửa/đổi serial/hoàn theo hồ sơ | Theo ma trận quyền · B |
| CT-01/02 | Việc nội dung; biên tập sản phẩm VI–EN/SEO | Content · A |
| CT-03/04 | Landing studio; preview/publish/history/restore | Content thực hiện; Owner chỉ xem · A/B |
| CT-05 | Media ảnh/3D, vị trí sử dụng | Content · A/B |
| RP-01/02/03 | Hóa đơn theo kỳ/nhân viên/sản phẩm | Owner; Order chỉ phần mình · A/B |
| RP-04/05 | Thu/hoàn; tồn/giá vốn/lãi gộp | Owner; Kho chỉ báo cáo được phép · B |
| RP-06 | Ưu đãi, top SKU, thu cũ, Care | Owner, scope theo BA · B |
| SY-01/02 | Nhân viên/quyền; cấu hình/chính sách/tỷ giá | Owner · C |
| SY-03/04 | Nhật ký; công cụ demo được cấp quyền | Scope theo BA · B/C |

## 9. Hóa đơn và thống kê: thiết kế để kiểm chứng được

IN-01 mặc định issued, bảng read-only, tổng toàn bộ tập lọc và số kết quả tách khỏi số dòng trang. Chi tiết giữ dòng sản phẩm/SKU, Care/giao, giảm giá, nguồn thanh toán, người phụ trách/phát hành, chứng từ giảm và timeline; có điều hướng sang đơn/hậu mãi theo quyền.

Bộ lọc gồm Trường–So sánh–Giá trị; AND là “Tất cả”, OR là “Ít nhất một”, NOT là “Phủ định nhóm”. Có ngoặc/thụt lề và mô tả dễ đọc; tối đa 4 cấp/30 điều kiện theo BA. Toán tử >, <, =, <> có nhãn chữ; ngày chọn bằng date picker, nhân viên/sản phẩm bằng picker ID. Scope quyền luôn ở ngoài biểu thức người dùng. Không cho gõ SQL tự do.

RP-01/02/03 có bảng thống kê và tổng, chọn tháng/quý/năm kèm năm/phạm vi nhiều năm; biểu đồ chỉ bổ sung. Chọn ô số mở chứng từ/dòng nguồn. Trạng thái “Không có giao dịch” hiển thị 0 đúng kỳ, không tạo cảm giác báo cáo lỗi.

| Nhãn | Ý nghĩa phải giữ |
|---|---|
| Tổng HĐ phát hành | SUM hóa đơn issued theo issued_at, mỗi hóa đơn một lần |
| Điều chỉnh giảm | Chứng từ giảm issued theo ngày riêng của chứng từ |
| Ròng phát sinh kỳ | Phát hành trong kỳ trừ điều chỉnh phát sinh trong kỳ |
| Tiền đã thu/đã hoàn | Giao dịch hoàn tất theo thời điểm thu/hoàn; tách tiền demo và credit |
| Lãi gộp hàng bán | Theo công thức/giá vốn BA; không gọi là lợi nhuận ròng |

Thu cũ là nguồn thanh toán, không giảm tổng HĐ. Nhân viên thống kê là responsible_staff_id, không phải người thao tác phát hành. “Hóa đơn chứa sản phẩm X” khác “Thống kê riêng dòng sản phẩm X”; UI phải dùng nhãn riêng. Giữ cả nhân viên inactive trong lịch sử.

Xuất có ba lệnh: hóa đơn này, danh sách theo bộ lọc, báo cáo đang xem. Dialog nêu phạm vi, count, tổng, thời điểm; danh sách mặc định toàn bộ kết quả hợp lệ. Vượt hạn mức cấu hình phải báo, không cắt âm thầm. Xuất Excel/PDF cùng lượt lấy cùng dataset theo API; WinForms dùng SaveFileDialog. Kết quả cho Mở file/Mở thư mục, không báo thành công trước khi lưu xong.

## 10. Luồng hậu mãi và chính sách

Trả hàng hiển thị ngày giao, ngày yêu cầu, hạn 14 ngày, lỗi/giao sai, món và lượng được trả còn lại. Thu cũ phân biệt ước tính, báo giá phiên bản, khách chấp nhận và credit đã cấp; món không serial dùng mã tiếp nhận. Care hiển thị bảo hành chuẩn và 12 tháng gia hạn, ngày hiệu lực và phương án sửa/đổi/hoàn đúng thẩm quyền. Không thêm gói rơi/vỡ.

Mỗi hồ sơ dùng detail header, checklist theo nhóm hàng, bằng chứng, timeline và “Bước tiếp theo”. Tài chính tách điều chỉnh nghĩa vụ khỏi hoàn tiền thực tế. Nút duyệt hiển thị số tiền/tác động và nguồn hoàn; giữ audit/lý do. Khuyến mãi thể hiện phiên bản, quota giữ/đã dùng, hiệu lực; chỉ báo “Đã áp dụng” sau server xác nhận. Điểm, credit và tiền là ba sổ riêng.

## 11. Trạng thái, lỗi và thao tác bàn phím

Mọi màn có loading, empty, no-results, error, success, permission-denied và dirty-state khi phù hợp. Refresh giữ layout, kết quả cũ có thời điểm; không khóa cả app vì một bảng đang tải. Lỗi mạng có Thử lại; request cũ không ghi đè lựa chọn mới. 401 yêu cầu xác thực; 403 giải thích quyền; 409 nêu bản ghi thay đổi và cách tải lại, không ghi đè âm thầm.

Form đang sửa hỏi Lưu/Không lưu/Ở lại khi rời. Lưu lỗi giữ dữ liệu nhập. Double-click/retry không nhân giao dịch; chặn nút UI bổ sung cho idempotency server. Xác nhận chỉ cho hành động có tác động đáng kể, ghi tên/mã/số tiền cụ thể.

Tab order theo mắt đọc; focus hiện rõ; Enter mở dòng hoặc submit đúng ngữ cảnh; Escape đóng panel/dialog an toàn. Tooltips không chứa thông tin duy nhất. Grid dùng được bàn phím, control có AccessibleName/label phù hợp, trạng thái không chỉ dựa vào màu.

## 12. Chuyển thiết kế thành WinForms

Codex bắt buộc đọc và áp dụng **UI/UX Pro Max** cho design system, hierarchy, contrast, form/error/focus. Skill không có stack WinForms riêng: dùng hướng dẫn theo kết quả UX và tài liệu Microsoft; không gọi API WPF/WinUI như thể là WinForms. Chọn thư viện theo nền tảng/license/dependency đã xác minh trong repo, không nâng .NET hoặc thay control suite chỉ để đổi màu.

Ưu tiên shared UserControl, TableLayoutPanel/FlowLayoutPanel, SplitContainer, DataGridView/BindingSource và presenter/application client hiện có. Event handler không đặt SQL/công thức nghiệp vụ. I/O async/cancellable, cập nhật UI đúng thread, dispose event/resource đúng vòng đời. Custom painting chỉ ở thành phần nhỏ có kiểm accessibility và DPI, không tự vẽ toàn bộ form.

Mỗi màn cần map control → action → service/API → permission. Tạo gap report khi thiếu endpoint, quyền hoặc trạng thái; không âm thầm thay logic đang ổn. Chỉ dùng dữ liệu minh họa trong prototype, đánh dấu rõ; production đọc dữ liệu thật.

## 13. Nghiệm thu và bàn giao

Kiểm trên Windows: 1366×768 scale100% và125%; 1920×1080 scale125% và150%; resize qua các breakpoint và work area thực. Không cắt action/tổng/chữ Việt; form dài cuộn được, grid có thể đọc đủ cột. Build trên Linux không thay bằng chứng runtime Windows.

Đăng nhập đủ bốn role; kiểm quyền vượt cả nút/shortcut/API, đặc biệt owner không publish và content không đọc hóa đơn. Thực hiện các task: tìm/xử lý đơn; gán serial; nhận một phần; kiểm kê chênh lệch; hậu mãi từng món; lọc AND/OR/NOT; drill-down báo cáo; xuất file khớp tổng; lưu/preview/publish draft theo quyền. Kiểm API chậm/mất kết nối/xung đột và dữ liệu chưa lưu.

Bàn giao ảnh trước–sau cùng viewport/DPI, danh sách màn đã đổi theo ID, component dùng lại, kết quả thao tác thật, lỗi còn lại và lệnh chạy. Đánh giá bằng tính rõ ràng, nhất quán và hoàn thành công việc; không tuyên bố “đẹp nhất”, “hoàn hảo” hoặc giảm thời gian khi chưa đo. Màn chưa kiểm phải ghi chưa kiểm, không dựa vào mockup để đánh dấu WinForms đã đạt.
