# Prompt triển khai BotZone Admin Redesign v1

Bạn tiếp tục dự án BotZone trong workspace hiện tại. Nghiệp vụ coi như tạm ổn; nhiệm vụ này là redesign toàn bộ giao diện quản trị C# Windows Forms để đẹp, rõ ràng và dễ làm việc hằng ngày.

Phạm vi gồm đăng nhập, shell và toàn bộ màn/module của bốn role: Chủ cửa hàng, Nhân viên xử lý đơn, Nhân viên kho, Nhân viên nội dung. Thiết bị mục tiêu là laptop và PC Windows. Thực hiện đến khi có kết quả native chạy được để tôi xem; không dừng ở kế hoạch, mockup hoặc bốn dashboard trang chủ.

## 1. Đọc dự án và bộ redesign

Tôi đặt bộ redesign trong `docs/design/admin-redesign-v1/`. Nếu chưa thấy, tìm theo tên `05_PROMPT_CODEX.md`, `03_WINFORMS_IMPLEMENTATION.md` hoặc README của bộ redesign trong workspace và các đường dẫn tôi đã cung cấp; không tự tạo một bản hướng dẫn khác để thay thế. Nếu thực sự thiếu, báo đúng file cần tôi bổ sung sau khi đã kiểm hiện trạng hữu ích.

Đọc đầy đủ spec, `02_SCREEN_BLUEPRINTS.md`, prototype/ảnh tham chiếu, implementation guide, tiêu chí nghiệm thu và nghiên cứu trong gói. Đọc `AGENTS.md`, BA hiện hành, tài liệu chuyển WinForms, `.sln`, `.csproj`, dependency, form base, navigation, authorization và các service liên quan. Kiểm `git status`; giữ thay đổi đang có của tôi và session khác.

Chạy app hiện tại nếu môi trường hỗ trợ để chụp baseline đăng nhập và các role. Lập `UI_REDESIGN_TRACKING.md`, liệt kê mọi form/màn/action theo cả BA và code: role sử dụng, luồng chính, vấn đề, component mới, ảnh trước/sau, kết quả kiểm. Không bỏ module chỉ vì nó không xuất hiện trong vài ảnh minh họa.

## 2. Bắt buộc dùng UI/UX Pro Max

Phải đọc và áp dụng skill `ui-ux-pro-max` thực sự, không chỉ ghi tên skill trong phần kết luận. Nếu chưa có, tìm trong các thư mục skill được phép; cài từ nguồn chính thức khi công cụ cho phép. Nếu bị chặn, báo blocker cụ thể, không tuyên bố đã dùng.

Thực hiện phân tích product/role/platform, chạy `--design-system` với query tập trung như `retail operations desktop tables`, rồi các tìm kiếm `--domain ux` cho keyboard/focus, validation, bảng/lọc và loading. Đọc kết quả, chọn khuyến nghị phù hợp, ghi ngắn gọn áp dụng ở đâu.

Skill hiện không có stack WinForms: không giả định WPF/WinUI là WinForms và không chép hướng dẫn CSS/ARIA/mobile vào app native. Dùng design-system/UX cùng tài liệu Microsoft WinForms. Giữ master của bộ redesign; không để output tự sinh thay màu, bố cục hoặc mục tiêu đã chốt. Rà UX trên màn đã triển khai và sửa các lỗi nhìn thấy.

## 3. Giữ kiến trúc và nghiệp vụ

Storefront là website; admin là Windows Forms. Hệ thống chạy local, API/database theo repo hiện tại; không dùng Supabase/Vercel, không tạo admin web, backend hoặc bộ migration thứ hai. BA định hướng .NET 10 nhưng `.csproj` thực tế quyết định compatibility; không nâng framework chỉ để redesign.

Giữ service, DTO, validation, transaction, phép tính, trạng thái, permission và dữ liệu. UI mới gọi nghiệp vụ hiện có. Không seed số liệu trong prototype vào DB, không thay số thật bằng dữ liệu giả, không cắt chức năng để màn đẹp hơn. Nếu phát hiện lỗi nghiệp vụ liên quan, nêu rõ và xử lý đúng phạm vi.

WebView2 chỉ dùng cho preview landing/PDF theo BA, không nhúng toàn bộ HTML prototype làm admin. Không sửa storefront hoặc model 3D ngoài phần preview thực sự cần cho CMS.

## 4. Hệ thiết kế và component

Hướng thị giác: nền nội dung `#FAFAF9`, surface `#FFFFFF`, sidebar graphite `#19191C`, primary burgundy `#6F233F`; Segoe UI; radius khoảng 6–8; spacing nhất quán; bảng dễ đọc. Dùng tokens chung, không rải màu/kích thước trên từng form. Body khoảng 10.5–11 pt và scale đúng DPI; trạng thái luôn có chữ, không chỉ màu.

Ưu tiên sự gọn gàng, phân cấp chữ, đường phân cách nhẹ và khoảng trống hợp lý. Tránh gradient lòe loẹt, card lớn vô ích, shadow dày, emoji làm icon và dashboard toàn biểu đồ. Mỗi màn có một hành động chính rõ; secondary/bulk action đúng ngữ cảnh.

Giữ toolkit hiện có nếu ổn. Nếu chưa có, chọn AntdUI stable tương thích cho shell/forms và DataGridView style đồng bộ cho bảng khi phù hợp. Không thêm dependency trả phí/trial, không trộn nhiều toolkit, không đổi package hàng loạt. Kiểm keyboard, DPI và tiếng Việt trước khi nhân rộng.

Xây hoặc tái sử dụng component chung: login, shell, navigation, page header, toolbar, saved views, grid, status tag, filter builder, detail pane, edit form, empty/error/loading state và confirmation. Không biến mọi control thành Panel tự vẽ; giữ semantic và hành vi Windows. Giữ resize, maximize, minimize, Alt+F4 và Snap.

## 5. Đăng nhập và shell

Đăng nhập sang trọng nhưng tập trung vào tác vụ: thương hiệu tiết chế, field có label, nút hiện mật khẩu có nhãn, Enter submit, Tab hợp lý, loading và lỗi cụ thể. Không hiển thị trạng thái kết nối giả. Không lưu mật khẩu tùy tiện hoặc bổ sung quy trình xác thực ngoài nghiệp vụ.

Role do account sau xác thực quyết định. Không đưa role switcher của prototype vào bản chạy như cách đổi quyền. Vào đúng workspace của role, điều hướng thống nhất, menu dễ quét, đang chọn rõ, user/role nhìn thấy và logout thuận tiện.

Tìm nhanh và phím tắt chỉ thấy dữ liệu/lệnh được phép. Giữ filter, sort, trang, dòng chọn và vị trí cuộn khi quay về từ chi tiết. Drawer dùng xem nhanh; form dài dùng trang chi tiết trong shell, tránh modal chồng modal.

## 6. Thiết kế theo công việc của bốn role

**Chủ cửa hàng:** ưu tiên tình hình bán hàng và vấn đề cần quyết định, có đường mở dữ liệu chi tiết. Thống kê tiền hóa đơn theo tháng/quý/năm qua nhiều năm; theo nhân viên và quý/năm; theo sản phẩm và tháng/quý/năm. Bộ lọc, định nghĩa chỉ số và đơn vị rõ. Biểu đồ hỗ trợ bảng và drill-down; không thay cách tính hiện có. Chủ cửa hàng xem tổng thể CMS/landing nhưng không tự được cấp quyền sửa hoặc xuất bản.

**Nhân viên xử lý đơn:** vào hàng đợi cần xử lý, saved views theo trạng thái/việc được giao; nhìn mã đơn, khách, thanh toán, giao hàng và việc tiếp theo. Tách trạng thái thanh toán khỏi fulfillment nếu backend đã tách. Chi tiết gom thông tin đủ để xử lý mà ít chuyển màn. Xác nhận/bulk actions tuân state machine và permission; không xác nhận thanh toán hoặc đổi trạng thái ngoài điều kiện nghiệp vụ.

**Nhân viên kho:** ưu tiên việc nhập/xuất/kiểm đang chờ, tồn và cảnh báo cần xử lý. Luồng nhận hàng, chọn SKU/biến thể, serial/IMEI, kiểm đếm, điều chỉnh theo chức năng đã có. Ô quét/nhập mã giữ focus hợp lý; kết quả trùng/sai/missing rõ, không mất dữ liệu đang nhập. Giữ truy vết chứng từ và điều kiện xác nhận kho.

**Nhân viên nội dung:** ưu tiên nội dung thiếu, bản nháp và việc chuẩn bị xuất bản. Tổ chức sản phẩm/nội dung, media, banner, landing thành tác vụ dễ hiểu. Editor native có danh sách section, thuộc tính và preview; sửa văn bản, ảnh, thứ tự theo schema hiện có. Hiện trạng thái nháp/đã xuất bản, validation trước publish và đường quay lại. Nội dung được sửa/xuất landing theo quyền; không được sửa tiền, thanh toán hay tồn kho.

Đây là định hướng luồng, không phải quyền mới. Map từng thao tác vào policy thật; menu, context menu, tìm nhanh, export và API đều phải giữ authorization. Đừng chỉ giấu nút rồi xem là đã kiểm quyền.

## 7. Bao phủ mọi module và trạng thái

Đối chiếu inventory để redesign cả sản phẩm/biến thể, danh mục, khách hàng, đơn, hóa đơn, thanh toán, giao nhận, đổi trả/bảo hành/thu cũ, tồn kho/chứng từ/serial, khuyến mãi, thống kê, nội dung/media/landing, tài khoản/phân quyền/cấu hình và lịch sử nếu những module đó tồn tại trong phạm vi BA/code. Không tự bổ sung nghiệp vụ mới vào module chưa có.

Mỗi màn cần trạng thái có dữ liệu, rỗng, không có kết quả lọc, đang tải, lỗi, chỉ đọc, không có quyền và thay đổi chưa lưu phù hợp. Lỗi giữ input và cho retry; không đóng form khiến mất công nhập. Action nguy hiểm có nội dung xác nhận đúng đối tượng, không hỏi xác nhận cho mọi click.

Hóa đơn phải có danh sách dễ tìm, tổng tiền đúng phạm vi và mở chi tiết thuận tiện. Bộ lọc nâng cao hỗ trợ các toán tử đang có `>`, `<`, `=`, `<>`, nhóm AND/OR/NOT bằng giao diện dễ hiểu, có xóa điều kiện/reset và mô tả bộ lọc. Dùng query builder/service hiện có, không nối SQL từ text người dùng. Xuất Excel/PDF giữ đúng dữ liệu, quyền, phạm vi lọc và tổng; chọn dòng để xuất phải thể hiện rõ.

## 8. Hiệu năng, DPI và accessibility

Load/save/report/export không khóa UI. Dùng async đúng target, cập nhật control trên UI thread; hủy hoặc bỏ kết quả truy vấn cũ khi đổi filter/tab. Chặn gửi trùng đúng thao tác, không khóa cả app. Không thêm virtual mode hoặc viết lại data layer nếu chưa có vấn đề dữ liệu thật. DataGridView dùng shared styles, autosize hợp lý và paging hiện có; tránh control/object cho từng cell.

Thiết kế chỉ dành laptop/PC Windows. Dùng Dock/Anchor/layout panel thay tọa độ tuyệt đối. Khi hẹp, thu sidebar, đổi pane thành trang chi tiết, cho form cuộn đúng vùng; không thu chữ đến mức khó đọc. Tôn trọng work area, taskbar và chrome. Nút lưu/huỷ/xác nhận không được nằm ngoài màn.

Kiểm baseline độ phân giải vật lý/Windows scale: 1366×768 ở 100% và 125%; 1920×1080 ở 125% và 150%. Kiểm thêm 1920×1080 ở 100% và 2560×1440 ở 150%, 200% khi phần cứng cho phép hoặc có rủi ro DPI liên quan; không tự biến cấu hình không có thành blocker. 1366×768/125% chỉ khoảng 1093×614 logical trước chrome/taskbar, nên không ép minimum size vượt diện tích sử dụng. Kiểm cấu hình DPI trước khi sửa, tránh scale hai lần hoặc manifest/bootstrap mâu thuẫn.

Focus phải nhìn thấy; Tab/Shift+Tab/Enter/Escape hoạt động; label, AccessibleName/Role và mnemonic đầy đủ. Kiểm tiếng Việt có dấu, text lớn và Narrator trên các thành phần chính. Phím tắt có nhãn hướng dẫn, không cướp phím khi nhập mã hoặc văn bản. Không tuyên bố đạt accessibility chỉ vì đã đặt thuộc tính.

## 9. Triển khai, kiểm thử và bàn giao

Chia việc thành các phần có ownership rõ nếu dùng multi-agent: shared tokens/components, từng nhóm module, review/nghiệm thu. Thống nhất component trước, tránh agent cùng sửa shell hoặc file designer. Giữ working tree; branch/commit chỉ theo quy định repo, không coi đó là điều kiện tự thêm để bắt đầu.

Làm login + shell + một màn danh sách/chi tiết để xác minh hướng; sau đó triển khai toàn inventory. Chạy build và kiểm tra sẵn có; thêm kiểm thử có ý nghĩa cho binding, quyền, filter, tổng/export hoặc lifecycle bị tác động, không viết test chỉ sao chép implementation. So lại từng action/service trước và sau để tránh mất chức năng.

Mở executable Windows thật, dùng bốn account/role phù hợp, đi các luồng chính và chụp ảnh trước/sau. Kiểm đặc biệt: chủ chỉ xem CMS; nội dung không sửa tiền/kho; hóa đơn/tổng/export; mất kết nối; thao tác nhanh; DPI và keyboard. Build pass hoặc ảnh HTML không thay thế bằng chứng native.

Nếu môi trường chỉ có Linux/không chạy được app Windows, hoàn thiện phần có thể kiểm, ghi chính xác runtime/DPI/ảnh native chưa xác minh; không tạo ảnh giả rồi báo đã nghiệm thu. Tự sửa vấn đề quan sát được trước khi bàn giao, không đẩy việc rà lỗi hiển nhiên cho tôi.

Kết thúc bằng: danh sách module đã redesign theo role, ảnh native trước/sau, lệnh build/chạy, vị trí executable, kết quả kiểm và phần còn chưa xác minh. Cập nhật tracking để session sau tiếp tục đúng chỗ. Mục tiêu cuối là một ứng dụng WinForms đồng bộ, đẹp và dễ dùng trong các tác vụ thật, đầy đủ mọi màn đang có.
