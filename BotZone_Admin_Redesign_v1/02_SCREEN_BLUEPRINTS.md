# BotZone — Đặc tả bố cục và tương tác màn hình quản trị

Phiên bản redesign 1.0 · 17/11/2026 · Dùng cùng BA v1.1 WinForms, hệ thiết kế và prompt triển khai trong bộ bàn giao.

**Phạm vi:** ứng dụng native C# WinForms chạy trên laptop/PC Windows. Admin dùng tiếng Việt và VND. VI/EN là ngôn ngữ nội dung storefront; mobile chỉ là chế độ xem trước storefront. Các hình 1440 × 900 là mẫu thiết kế, không phải bằng chứng ứng dụng đang chạy.

## 0. Quy tắc khi chuyển thiết kế thành code

- Đây là redesign giao diện cho nghiệp vụ hiện có. Codex ánh xạ field, command, permission và trạng thái vào DTO/API thật; không tự thêm workflow, công thức hay migration chỉ để làm đúng một ảnh minh họa.
- Khi BA mô tả chức năng nhưng repo chưa có, ghi vào gap report. Không đặt nút thành công giả hoặc số dashboard viết tay trong bản production.
- Mọi thao tác ảnh hưởng dữ liệu phải phản ánh kết quả server. UI không tự đổi trạng thái để vượt bước kiểm tra, thanh toán, tồn, phiên bản hoặc quyền.
- Không có quyền: ẩn module/action không liên quan. Có quyền nhưng đang thiếu điều kiện: giữ action ở đúng vị trí, vô hiệu hóa và nêu lý do cạnh action hoặc trong vùng “Bước tiếp theo”.
- Giá và tồn thuộc SKU/nghiệp vụ; nội dung không tự sửa giá. Hóa đơn đã phát hành giữ snapshot, không đưa nút sửa tiền trực tiếp. Chủ cửa hàng không sửa/xuất bản/khôi phục landing.

## 1. Khung chung và ngôn ngữ thị giác

### 1.1 Canvas chuẩn cho mẫu 1440 × 900

| Vùng | Kích thước định hướng ở 100% scale | Nội dung |
|---|---:|---|
| Title bar Windows | Khoảng 32 px | Tên ứng dụng, nút thu nhỏ/phóng to/đóng theo hành vi Windows |
| Sidebar | 224–232 px | Brand BotZone, điều hướng role, hồ sơ người dùng dưới cùng |
| Header nội dung | 64–72 px | Breadcrumb ngắn, tìm điều hướng Ctrl+K, thông tin tài khoản/phạm vi |
| Vùng nội dung | Phần còn lại, padding 24–28 px | Tiêu đề trang, toolbar, dữ liệu và panel chi tiết |
| Thanh trạng thái | 24–28 px | Kết nối API, thời điểm cập nhật, chế độ demo nếu có |

Các số trên là nhịp bố cục để dựng mẫu, không là tọa độ cứng của WinForms. Dùng Dock/Anchor/TableLayoutPanel/SplitContainer và AutoScale theo cấu hình dự án. Ngưỡng thu gọn theo vùng client còn lại sau DPI, không theo kích thước màn hình vật lý đơn thuần.

Ở laptop 1366 × 768: sidebar có thể gọn còn 200–216 px; giảm padding trước khi giảm chữ. Drawer chi tiết chuyển thành trang đầy đủ khi bảng còn quá hẹp. Không ép đầy đủ ba panel của editor vào một cửa sổ nhỏ. Bảng rộng có thanh cuộn riêng và nút chọn cột; action chính luôn truy cập được.

### 1.2 Token dùng xuyên suốt

| Vai trò | Token |
|---|---|
| Nền workspace | `#FAFAF9` |
| Bề mặt bảng/form | `#FFFFFF` |
| Sidebar | `#19191C` |
| Chữ chính | `#202126` |
| Chữ phụ | `#63656D` |
| Accent wine | `#6F233F` |
| Accent nhạt / selected nhẹ | `#F5EBEF` |
| Border | `#E2E3E7` |
| Font | Segoe UI; fallback Windows hỗ trợ tiếng Việt |

Màu wine dành cho action chính, selection và chi tiết thương hiệu; không phủ nền mọi ô. Trạng thái thành công/cảnh báo/lỗi có chữ đi kèm, không dựa riêng vào màu. Nền sáng, phân cách mảnh, góc bo nhỏ, shadow rất ít. Không dùng gradient, icon emoji hoặc một dãy card KPI nhiều màu.

Font ở WinForms dùng đơn vị point theo cơ chế native; đối chiếu mẫu thị giác trước khi áp số. Gợi ý body 10–10,5 pt, label 9,5–10 pt, heading trang 19–22 pt. Bảng hàng cao khoảng 38–44 px logical, header 36–40 px; mode compact chỉ khi vẫn đọc/điều khiển được ở DPI mục tiêu. Tiền căn phải, mã dùng độ rộng ổn định, thời gian định dạng nhất quán.

### 1.3 Cấu trúc một trang

1. Tiêu đề + một dòng giải thích phạm vi/đơn vị.
2. Một action chính; action phụ ở cạnh hoặc menu “Thao tác khác”.
3. Bộ lọc/tab công việc.
4. Nội dung chính: bảng/form, không dashboard trang trí.
5. Tổng, phân trang hoặc vùng tác động trước khi xác nhận.

Breadcrumb chỉ khi có cấp sâu thật. Back giữ tab, bộ lọc, trang và dòng đã chọn. Chỉ một dialog có owner tại một thời điểm; không mở chuỗi cửa sổ rời khiến người dùng không biết quay về đâu.

## 2. Điều hướng và trang vào của bốn role

| Role | Trang vào | Nhóm điều hướng chính |
|---|---|---|
| Chủ cửa hàng | `OWN-HOME` | Tổng quan; Bán hàng (đơn, hóa đơn, tiền thu/hoàn); Sản phẩm; Kho & nhập hàng; Hậu mãi; Ưu đãi & thành viên; Báo cáo; Nội dung — chỉ xem; Hệ thống |
| Nhân viên xử lý đơn | `ORD-QUEUE` | Công việc; Đơn hàng; Hóa đơn; Khách hàng; Hậu mãi; Tra cứu sản phẩm/tồn; Báo cáo của tôi; Nhật ký của tôi |
| Nhân viên kho | `WH-WORK` | Công việc kho; Tồn & serial; Nhập hàng; Kiểm kê; Kiểm định hậu mãi; Lịch sử kho |
| Nhân viên nội dung | `CNT-HOME` | Việc nội dung; Nội dung sản phẩm; Landing page; Thư viện media; Lịch sử nội dung |

Nhóm được mở/đóng, tối đa hai cấp. Badge chỉ đếm việc có nghĩa với role, lấy từ dữ liệu thật. Những tính năng dùng chung theo quyền vẫn dùng cùng component và cách gọi; không sao chép bốn bộ giao diện độc lập.

## 3. `LOGIN` — Đăng nhập quản trị

**Mục đích:** vào ứng dụng nhanh, biết rõ đang đăng nhập BotZone và không nhầm role.

**Bố cục mẫu:** vùng thương hiệu tối khoảng 40–44% chiều rộng; vùng form sáng còn lại. Form rộng khoảng 360–448 px, căn theo chiều dọc trong work area. Vùng thương hiệu chỉ có logo chữ BotZone, câu ngắn “Mọi vận hành. Một nơi làm việc.” và một nét đồ họa tiết chế. Không đưa ảnh điện thoại khổng lồ hoặc animation 3D lên form đăng nhập.

**Thứ tự form:**

1. Eyebrow “BOTZONE ADMIN”; heading “Đăng nhập quản trị”.
2. Mô tả “Sử dụng tài khoản nhân viên được cấp.”
3. Label “Tên đăng nhập” hoặc “Email” đúng cơ chế auth hiện có; textbox.
4. Label “Mật khẩu”; password textbox + nút hiện/ẩn có accessible name.
5. “Ghi nhớ tên đăng nhập” nếu có lưu local; không tự lưu mật khẩu.
6. Vùng lỗi form, thường không chiếm chỗ khi không có lỗi nhưng không làm form nhảy mạnh.
7. Action chính full-width: “Đăng nhập”.
8. Hỗ trợ/quên mật khẩu chỉ khi luồng hiện có hỗ trợ; thông tin build và trạng thái kết nối nhỏ ở chân form.

**Tương tác:** focus vào trường đầu phù hợp; Tab đúng thứ tự; Enter gửi một lần; hiện “Đang đăng nhập…” trong lúc chờ. Mật khẩu giữ ký tự người dùng nhập khi lỗi mạng, không lộ text ngoài lựa chọn của họ. Caps Lock chỉ báo khi đang focus password.

**Không có bộ chọn role trong app thật.** Server xác thực và trả role/quyền; app mở trang vào tương ứng. Bộ chọn role của mock nếu có nằm ngoài khung ứng dụng và ghi “Xem thiết kế theo vai trò”.

**Lỗi:** sai thông tin → thông báo chung dễ hiểu gần form; mất kết nối → “Chưa kết nối được dịch vụ BotZone. Thử lại.”; khóa/hết hiệu lực → nội dung đúng phản hồi auth. Không hiện stack trace, database connection string hoặc raw token.

**Vì sao:** một luồng đăng nhập chung giảm lựa chọn thừa và tránh cảm giác người dùng có thể tự chọn quyền.

## 4. `OWN-HOME` — Tổng quan cửa hàng

**Header:** “Tổng quan cửa hàng”; dòng phụ “Tháng này · VND · Cập nhật …”. Action chính “Xem báo cáo”; bộ chọn kỳ đặt gần tiêu đề.

**Thứ bậc:**

1. Dải bốn chỉ số theo mẫu chốt: Tổng HĐ phát hành; Điều chỉnh giảm; Ròng phát sinh kỳ; Cần quyết định. Mỗi số dẫn đến đúng danh sách/báo cáo và có định nghĩa ngắn. Tiền đã thu và Lãi gộp hàng bán mở ở báo cáo riêng, không trộn tổng hóa đơn với tiền thu.
2. Vùng trung tâm lớn nhất “Cần quyết định”: bảng ngoại lệ có nút Xem ở từng hàng.
3. Bên cạnh hoặc dưới: một biểu đồ giá trị hóa đơn theo kỳ + bảng “Cần chú ý” tồn thấp/đơn giao lỗi.
4. Không đặt bảng hoạt động dài hoặc nhật ký kỹ thuật lên trước các việc cần làm.

**Bảng Cần quyết định:** Loại việc | Mã hồ sơ | Nội dung ngắn | Giá trị/chênh lệch | Người đề nghị | Chờ từ | Hành động Xem. Mặc định sắp theo ưu tiên/thời gian theo dữ liệu hiện có; không tự tạo “AI priority”.

**Luồng:** chọn ngoại lệ → xem hồ sơ/bằng chứng → summary số tiền, nguồn, món hoặc chênh lệch → quyết định theo quyền → kết quả thật + audit → trở về đúng dòng. Không duyệt hoàn tiền hoặc điều chỉnh tồn chỉ bằng checkbox tại dashboard.

**Quyền đặc biệt:** lối vào Nội dung/Landing cho xem bản nháp/công khai/lịch sử theo BA; không xuất hiện action sửa/publish/restore.

**Vì sao:** chủ cửa hàng cần nhận diện sai lệch và quyết định, không phải mở app để ngắm biểu đồ.

## 5. `ORD-QUEUE` — Đơn cần xử lý

**Header:** “Đơn cần xử lý”; dòng phụ phạm vi người phụ trách. Action chính phụ thuộc tab: “Mở đơn đã chọn”; không tạo nút “Tạo đơn” nếu BA/API chưa có luồng tạo đơn tại quầy.

**Tab công việc:** Tất cả | Chờ xác nhận | Chờ thanh toán | Chờ kho | Đang giao | Có vấn đề. Tab là saved query tương ứng trạng thái backend, không thêm enum mới. Nếu chọn “Của tôi”, hiển thị chip cố định để người dùng biết scope.

**Toolbar:** tìm mã đơn/khách/SĐT → ngày tạo → thanh toán → xử lý/giao → bộ lọc thêm → xóa lọc. Tìm nhanh debounce/cancel request cũ, có nút hoặc Enter theo pattern hiện có.

**Bảng ưu tiên:** Mã + giờ | Khách | Mặt hàng tóm tắt | Tổng VND | Thanh toán | Giao/xử lý | Phụ trách. Cột phụ có thể chọn: phương thức, địa chỉ rút gọn, lần cập nhật, cảnh báo. Tiền căn phải; hai trạng thái thanh toán/giao luôn tách nhau.

**Layout 1440:** bảng chiếm khoảng 2/3 vùng nội dung; panel xem nhanh khoảng 340–380 px ở bên phải khi có dòng chọn. Khi vùng còn lại quá hẹp, panel đóng và dùng màn chi tiết. Một click chọn; Enter hoặc nút có nhãn mở đầy đủ; double-click chỉ là cách phụ.

**Panel xem nhanh:** mã + badge → khách/liên hệ → 2–3 dòng hàng → tổng → “Bước tiếp theo” → action hợp lệ → timeline rút gọn. Ví dụ COD chưa xác nhận thì action xác nhận; banking chờ kiểm thì action xác nhận giao dịch demo theo điều kiện.

**Batch:** chỉ hiện khi có command thực hỗ trợ. Thanh ghi “Đã chọn 5 đơn trên trang này”, scope và action cụ thể. Không âm thầm áp toàn bộ kết quả. Đơn không hợp lệ nêu lý do; không báo batch thành công toàn bộ khi có dòng lỗi.

**Vì sao:** bảng hàng chờ + xem nhanh giúp xử lý liên tục, không mở/đóng nhiều form.

## 6. `ORD-DETAIL` — Chi tiết đơn hàng

**Header cố định:** Quay lại → mã đơn → thời điểm → các badge độc lập. Bên phải một action bước tiếp theo; action ít dùng nằm trong “Thao tác khác”.

**Cấu trúc:**

- Cột chính: sản phẩm/dịch vụ; thông tin giao nhận; tiến trình thực hiện.
- Cột hỗ trợ: khách và địa chỉ; thanh toán; người phụ trách; việc đang chặn.
- Tab dưới: Timeline | Thanh toán | Giao nhận | Hóa đơn & hậu mãi; chỉ hiện phần role được đọc.

**Bảng hàng:** Sản phẩm/SKU | Biến thể | SL | Đơn giá | Giảm | Thành tiền | Serial/trạng thái gán nếu có. Care/giao có cách trình bày phù hợp hợp đồng dữ liệu; không tính tiền lần nữa ở UI.

**Khối thanh toán:** tổng đơn, voucher/điểm theo snapshot, phần credit thu cũ, phần tiền cần thu, đã thu/còn chờ, phương thức và mã tham chiếu. Payment attempt thất bại nằm trong lịch sử, không cộng vào đã thu.

**Các action theo ngữ cảnh:** xác nhận đơn; ghi nhận banking/COD demo; chuyển xử lý hợp lệ; đề nghị hoàn; tạo/đi tới hồ sơ hậu mãi; xem hóa đơn. API quyết định điều kiện. Nhân viên xử lý đơn không tự gán serial/xuất kho hoặc duyệt hoàn tiền.

**Hành động tác động lớn:** dialog có mã đơn, số tiền/món bị tác động, nguồn thanh toán và lý do bắt buộc khi BA quy định. Nút chính gọi đúng hành động như “Xác nhận thu 5.000.000 ₫”, không “OK”.

**Hóa đơn:** trước bàn giao có thể xem nháp theo BA; phát hành gắn command bàn giao/xuất kho, không thêm nút phát hành tùy ý trên đơn. Issued hiển thị liên kết tới `INV-DETAIL`.

**Vì sao:** thông tin cần kiểm và action hợp lệ cùng một chỗ, đồng thời không hòa lẫn đơn, giao hàng và dòng tiền.

## 7. `WH-WORK` — Công việc kho

**Header:** “Công việc kho”; dòng phụ “Một kho BotZone · … phiếu cần thực hiện”. Action chính “Mở phiếu đã chọn”. Vùng quét/tìm mã phiếu luôn dễ thấy.

**Tab:** Xuất hàng | Nhập hàng | Kiểm định | Kiểm kê. Không có selector đa kho giả.

**Bảng:** Mã phiếu | Loại / đơn nguồn | Sản phẩm hoặc số dòng | Tiến độ | Việc tiếp theo | Thời gian | Người thực hiện. Không hiển thị doanh thu/lợi nhuận hoặc danh sách hóa đơn tài chính.

**Vùng phụ:** tồn cần chú ý và kiểm định đang chờ theo nhiệm vụ. Không thay bàn làm việc bằng hàng chục KPI tổng kho.

**Mở phiếu xuất (`WH-PICK`, biến thể chi tiết):** mã phiếu + đơn nguồn → vùng quét IMEI/serial → bảng SKU cần lấy/đã lấy → lỗi theo dòng → đóng gói → bàn giao theo command hiện có.

**Bảng picking:** SKU / Tên | Yêu cầu | Đã ghi nhận | Serial/lô | Tình trạng. Phụ kiện dùng số lượng/lô, không ép IMEI. Scanner keyboard-wedge nhập vào ô đang focus; Enter chỉ xử lý mã quét, không tự kích hoạt command bàn giao toàn phiếu.

**Footer phiếu:** tổng tiến độ + lỗi còn lại + một action chính theo bước. Trước bàn giao nêu số món, đơn nguồn, điều kiện thanh toán/COD đã được server xác nhận; command thực hiện nguyên transaction hiện có, không tự tách issue invoice khỏi kho.

**Vì sao:** kho cần biết phiếu nào làm trước và sai mã nào, không cần xem số tiền của toàn cửa hàng.

## 8. `WH-RECEIPT` — Nhận hàng theo phiếu nhập

**Header:** mã phiếu + nhà cung cấp + trạng thái + đơn mua nguồn. Action chính “Xác nhận nhận hàng” khi có dữ liệu hợp lệ; “Lưu tiến độ” nếu API hỗ trợ draft.

**Khối đối chiếu:** ngày nhận, chứng từ nhà cung cấp, người nhận theo tài khoản, ghi chú/bằng chứng. Thông tin có sẵn điền tự động, không bắt nhập lặp mã nhà cung cấp.

**Bảng:** SKU / Tên | Đặt | Đã nhận trước | Nhận lần này | Còn thiếu | Serial/lô | Kết quả. Đơn giá/giá vốn chỉ hiển thị khi quyền phiếu nhập cho phép; đây không phải quyền sửa giá vốn toàn cục.

**Vùng quét/nhập:** chọn dòng SKU → quét serial hoặc nhập SL → phản hồi tại dòng. Mã trùng, sai SKU, serial đã có hoặc SL vượt còn phải báo ngay. Dòng chưa nhập khác với nhận 0.

**Xác nhận:** summary số lượng nhận, số còn thiếu, món hỏng/chờ kiểm và tác động tồn. Sau thành công vẫn hiện các dòng chưa nhận; không tự đóng phiếu đủ khi mới nhận một phần.

**Kiểm kê (`WH-COUNT`, cùng pattern):** SKU | Sổ sách | Thực đếm | Chênh lệch | Lý do. “Chưa đếm” hiển thị riêng. Lưu kết quả đếm và đề nghị điều chỉnh khác nhau; kho đề nghị, owner duyệt ngoại lệ. Không sửa trực tiếp tồn master.

**Vì sao:** planned/actual/còn thiếu minh bạch và không đánh đồng nhập dở với tồn đã tăng.

## 9. `CNT-HOME` — Việc nội dung

**Header:** “Nội dung”; mô tả “Hoàn thiện sản phẩm và trang đang hiển thị trên BotZone”. Action chính “Mở landing page”.

**Tab/hàng đợi:** Cần hoàn thiện | Có thay đổi chưa xuất bản | Bản nháp | Đã xuất bản. Counts lấy từ dữ liệu thật; không dùng điểm chất lượng phần trăm tự nghĩ ra.

**Bảng:** Nội dung | Loại | VI | EN | Vấn đề cần xử lý | Trạng thái | Cập nhật. Cột VI/EN dùng nhãn như Đủ/Thiếu mô tả, không chỉ chấm màu. Click lỗi đưa đến đúng field của editor.

**Khối phụ:** landing đang chạy + phiên bản + thời điểm + người xuất bản; bản nháp gần đây; media lỗi/tham chiếu không hợp lệ nếu có dữ liệu. Không có doanh thu, hoàn tiền hoặc tồn chỉnh sửa trên trang này.

**Vì sao:** nội dung có một backlog rõ ràng, việc sửa VI/EN không phải lục từng sản phẩm.

## 10. `CNT-EDITOR` — Landing editor

**Header:** “Landing page” / tên bản / trạng thái; bên phải **Lưu nháp**, **Xem trước**, **Xuất bản**. “Xuất bản” là action chính khi đủ điều kiện; không tạo auto-publish sau save. Owner nhìn cùng cấu trúc nhưng ở mode chỉ đọc, không có các command ghi.

**Thanh ngữ cảnh:** Bản nháp / Đang công khai; version và cập nhật; VI / EN; menu Lịch sử. Ngôn ngữ nội dung luôn nhìn thấy ngay cạnh trường đang sửa.

**Layout rộng:**

1. Trái 220–250 px: cây section theo thứ tự; thêm section hợp schema; lên/xuống; ẩn/hiện.
2. Giữa chiếm phần chính: tab Nội dung | Media | Chuyển động | Nâng cao.
3. Phải khoảng 300–340 px khi đủ chỗ: preview context, thumbnail/poster, phiên bản model, validation checklist ngắn.

Ở laptop/scale lớn, chuyển phần preview thành tab hoặc nút mở trình duyệt, giữ form biên tập rộng; không hiển thị một bản mobile admin.

**Tab Nội dung:** tên section nội bộ, tiêu đề VI/EN, mô tả, nhãn CTA, product/route picker, trạng thái hiển thị. Field số lượng/độ dài theo schema có counter chỉ khi hữu ích.

**Tab Media:** ảnh/model/poster, asset ID dưới nhãn thân thiện, kích thước/dung lượng, nguồn/quyền dùng theo dữ liệu, nút chọn từ thư viện. `.blend` là nguồn thiết kế, không phải upload runtime.

**Tab Chuyển động:** chọn preset đã duyệt, xem mô tả hành vi; default an toàn cho mobile storefront; không bắt người nội dung viết animation code.

**Tab Nâng cao:** góc máy, nghiêng, ánh sáng, tốc độ, góc mở Duo theo giới hạn schema/API đã có trong BA. Control có range, đơn vị, value và Khôi phục mặc định. Không mở editor shader/script hoặc tọa độ tự do ngoài contract.

**Với v003:** duy trì clip `Duo_OpenClose`, track scale STEP màn ngoài và kính tối; không thêm màn app/widget. Tên asset/phiên bản/poster hiển thị để tránh chọn model cache cũ. Model không tương thích section phải có lý do và cách chọn asset phù hợp.

**Preview:** dùng renderer storefront đang có, mở trình duyệt mặc định; WebView2 chỉ là vùng preview tùy chọn nếu đã được triển khai. Dùng preview grant theo BA; không đưa auth session vào URL. Nút Desktop/Mobile là kích thước xem storefront. Luôn ghi rõ preview bản nào, ngôn ngữ nào; bản nháp chưa hỗ trợ phải được ghi thiếu hụt, không giả là đã preview nháp.

**Publish:** validate VI/EN bắt buộc, CTA, poster, asset và preset. Panel báo lỗi có link đến field. Dialog trước xuất bản nêu bản sắp công khai, phạm vi/ngôn ngữ và thay đổi có dữ liệu; kết quả thành công sau API commit. Lịch sử/khôi phục là command riêng theo BA; khôi phục tạo version mới, không xóa lịch sử.

**Vì sao:** nhân viên hiểu mình đang chỉnh phần nào và bản nào sẽ xuất hiện trên web; sự linh hoạt được giới hạn bởi template đã thiết kế.

## 11. `CNT-PRODUCT` — Biên tập sản phẩm VI/EN

**Header:** tên sản phẩm + model/SKU liên quan + trạng thái nội dung. Action chính Lưu nháp hoặc Xuất bản đúng cơ chế hiện có; action phụ Xem trên website.

**Tab:** Nội dung | Media | Thông số | SEO. Tab VI/EN hoặc segmented selector đặt trong vùng nội dung, không nằm lẫn với setting ngôn ngữ admin.

**Field:** tên hiển thị, mô tả ngắn, mô tả dài, nhóm thông số theo schema, ảnh đại diện, thứ tự gallery, alt text, title/meta description nếu có. Dùng form có nhãn và editor vừa đủ; không biến việc nhập mô tả thành chỉnh HTML thủ công.

**Context panel:** ảnh đại diện, danh mục/model, SKU liên quan, giá bán và tình trạng kinh doanh chỉ đọc, tình trạng VI/EN. Không hiện giá vốn cho content. Field dùng chung ghi “Dùng chung VI/EN”; chuyển locale không tạo bản sao SKU/giá/tồn.

**Dirty guard:** nếu chuyển sản phẩm/ngôn ngữ/rời form trong khi chưa lưu, có “Lưu rồi tiếp tục / Bỏ thay đổi / Ở lại”. Không hiển thị modal này nếu không có thay đổi.

**Vì sao:** nội dung song ngữ và thông tin kinh doanh phân biệt rõ, tránh sửa nhầm dữ liệu giao dịch.

## 12. `INV-LIST` — Danh sách hóa đơn và lọc nâng cao

**Role:** owner toàn bộ; order staff đọc/xuất phục vụ xử lý đơn theo access scope. Kho chỉ chứng từ đóng gói theo nhiệm vụ; content không thấy module này.

**Header:** “Hóa đơn”; mô tả “Chứng từ đã phát hành · VND”. Action chính **Xuất danh sách**; phụ **Xem chi tiết**, **Lọc nâng cao**. Không có nút Xóa/Sửa hóa đơn issued.

**Toolbar:** tìm mã/khách → khoảng ngày phát hành → nhân viên theo quyền → thanh toán → tab Đã phát hành/Nháp → Lọc nâng cao. Saved view nếu hiện có lưu theo người dùng, không làm thay đổi scope quyền.

**Dải tổng:** “75 hóa đơn khớp bộ lọc · Tổng HĐ … ₫ · Điều chỉnh … ₫ · Ròng … ₫ · dữ liệu lúc …”. Đây là toàn tập lọc, tách với footer “Đang xem 1–20/75”. Nhãn demo chỉ là dữ liệu minh họa trong mock, production dùng API.

**Cột đầy đủ theo BA:** Mã | Ngày/giờ | Khách | Nhân viên phụ trách | Số món | Trước giảm | Giảm giá | Tổng HĐ | Điều chỉnh giảm | Ròng | Thanh toán. Mẫu 1440 ưu tiên Mã, Ngày, Khách, Nhân viên, Tổng HĐ, Ròng, Thanh toán; các cột còn lại vẫn xem được bằng chọn cột/cuộn grid/chi tiết. Freeze cột mã; không ép 11 cột chữ nhỏ không đọc được.

**Mở chi tiết:** chọn dòng + Enter/nút Xem; double-click là bổ sung. Footer cố định trong vùng nội dung: pageSize, tổng số, nút trang trước/sau. Không materialize toàn bộ DB để lọc bằng WinForms.

### Bộ lọc nâng cao native

Panel mở trong workspace hoặc dialog có owner, rộng đủ đặt các trường. Mỗi dòng: **Trường | Toán tử | Giá trị | Xóa**. Field enum/product/staff là picker; tiền là số VND; ngày là date picker.

Toán tử có nhãn: “Lớn hơn >”, “Nhỏ hơn <”, “Bằng =”, “Khác <>”; hỗ trợ các operator còn lại theo whitelist BA. Nhóm: **Tất cả (AND)** / **Ít nhất một (OR)**; công tắc **Phủ định nhóm (NOT)**. Nhóm có viền/thụt lề nhẹ; tối đa 4 cấp/30 điều kiện theo contract.

Dưới nhóm có mô tả tiếng Việt và action **Áp dụng bộ lọc**, phụ **Xóa điều kiện**. Điều kiện không hợp lệ hiển thị ngay tại dòng, không gửi query rồi báo lỗi SQL.

Ví dụ owner: Tổng > 10.000.000 ₫ AND (Nhân viên An OR Bình) AND NOT (Có sản phẩm iPhone 13). “Không chứa sản phẩm” nghĩa là hóa đơn không có dòng sản phẩm đó; UI không dùng so sánh “một dòng khác sản phẩm”. Scope quyền vẫn do server ghép ngoài biểu thức, không được vượt bằng OR/NOT.

### Xuất

Dialog nêu **toàn bộ kết quả lọc**, số hóa đơn, tổng, readAt, định dạng Excel/PDF. Giới hạn BA khởi tạo 10.000 hóa đơn; vượt thì yêu cầu thu hẹp, không cắt âm thầm. File lưu qua SaveFileDialog; hoàn tất cho Mở file/Mở thư mục. Không tự đổi thành CSV/HTML mang đuôi XLSX/PDF.

**Vì sao:** đáp ứng tiêu chí DB của giảng viên, đồng thời người không biết SQL vẫn đọc và kiểm tra được điều kiện.

## 13. `INV-DETAIL` — Chi tiết hóa đơn

Header mã + “Đã phát hành” + ngày giờ; action chính **Xuất hóa đơn**, phụ **Xem đơn nguồn**. Nội dung read-only.

Khối đầu: khách snapshot, nhân viên phụ trách, người thao tác phát hành, đơn nguồn. Bảng dòng: sản phẩm/SKU, SL, đơn giá, voucher, điểm, thành tiền; Care/giao theo dòng thực. Khối tổng phân biệt giá trị phát hành gốc, điều chỉnh giảm, ròng và trạng thái thu/hoàn tại thời điểm đọc.

Tab nguồn thanh toán/chứng từ giảm/timeline; liên kết tới return/refund theo quyền. Hóa đơn gốc giữ số gốc; không làm người xem tưởng số phát hành đã bị đổi sau trả hàng. Không cho đổi giá hiện tại của SKU để sửa chứng từ lịch sử.

## 14. `REP-INVOICE` — Thống kê hóa đơn

**Role:** owner toàn bộ; order staff chỉ thống kê gắn chính tài khoản, selector nhân viên cố định và hiển thị rõ “Dữ liệu của tôi”. Kho/content không có màn này.

**Header:** “Thống kê hóa đơn”; action chính **Xuất báo cáo**. Tab **Theo thời gian / Theo nhân viên / Theo sản phẩm**.

**Bộ chọn:** chỉ tiêu → độ hạt Tháng/Quý/Năm → năm hoặc khoảng nhiều năm → phạm vi lọc khác theo tab → **Xem báo cáo**. Theo nhân viên hỗ trợ quý/năm đúng rubric; theo sản phẩm có Model/SKU và kỳ. Giữ người inactive khi xem lịch sử.

**Thứ bậc:** phần định nghĩa chỉ tiêu/kỳ/VND/readAt → tóm tắt tổng → một chart đơn giản → bảng dữ liệu đầy đủ + tổng cuối. Chart không thay bảng. Kỳ không có dữ liệu hiển thị 0 đúng API.

**Cột bảng:**

- Theo thời gian: Kỳ | Số HĐ | Tổng phát hành | Điều chỉnh | Ròng phát sinh kỳ.
- Theo nhân viên: Nhân viên phụ trách | Kỳ | Số HĐ | Tổng phát hành | Điều chỉnh | Ròng.
- Theo sản phẩm: Model/SKU | Kỳ | SL bán | SL điều chỉnh nếu có | Giá trị dòng phát hành | Điều chỉnh | Ròng.

Chỉ hiển thị các cột API đã định nghĩa; tài liệu gap ghi phần thiếu thay vì tính công thức khác ở UI. Dùng `responsible_staff_id` cho báo cáo nhân viên; không quy doanh số cho kho chỉ vì kho phát hành khi bàn giao.

**Drill-down:** chọn ô → mở tập hóa đơn/dòng tạo ra con số → back giữ kỳ. Phân biệt bộ lọc “Hóa đơn chứa sản phẩm X” với “Sản phẩm cần thống kê X”; không cộng cả hóa đơn cho X.

**Định nghĩa tiền:** Tổng phát hành theo kỳ hóa đơn; điều chỉnh theo kỳ chứng từ giảm; tiền thu/hoàn theo giao dịch hoàn tất ở báo cáo riêng; lãi gộp là chỉ tiêu riêng theo công thức BA. Không đổi tên tất cả thành doanh thu. Ví dụ trả tháng sau không viết lại tổng phát hành tháng trước.

**Vì sao:** số liệu vừa đọc được vừa truy ra dữ liệu gốc để bảo vệ đồ án và giải quyết lệch tổng.

## 15. `SYS-SETTINGS` — Cài đặt, nhân viên và nhật ký

**Role:** chủ cửa hàng. Header “Cài đặt”; tab hoặc navigation cấp hai: Cửa hàng | Chính sách | Nhân viên & quyền | Chế độ demo. API/DB thông tin nhạy cảm không xuất ra form vận hành.

**Cài đặt cửa hàng:** nhãn/group theo nghiệp vụ; field rõ đơn vị, default/current; **Lưu thay đổi** chỉ bật khi dirty hợp lệ. Các thiết lập tác động giá/chính sách chỉ cho sửa theo versioning/quy tắc hiện có; không sửa snapshot cũ.

**Nhân viên:** bảng Tên | Tài khoản | Vai trò | Trạng thái | Lần cập nhật. Chi tiết tên/role/trạng thái, action khóa/mở theo API. Trước đổi role/khóa tài khoản tóm tắt đúng người và tác động phiên/quyền; không dùng checkbox chung thay cơ chế quyền server. Không đưa mật khẩu vào bảng.

**Nhật ký (`SYS-AUDIT`):** Thời gian | Người thao tác | Hành động | Đối tượng | Kết quả; filter theo kỳ/người/module; detail read-only gồm trước/sau/bối cảnh được phép. Các role khác chỉ có nhật ký trong phạm vi, không đổi sang toàn hệ thống qua filter.

**Công cụ demo:** gom trong vùng riêng, nhãn dễ hiểu; reset/restore không đặt cạnh Lưu thường dùng. Nếu command có sẵn, dialog phải nêu môi trường, DB/volume đích, dữ liệu bị ảnh hưởng và yêu cầu xác nhận cụ thể. Không tự chạy reset khi khởi động hoặc sau redesign.

**Vì sao:** cấu hình ít dùng được tách khỏi công việc mỗi ngày, nhưng tác động vẫn rõ trước khi thực hiện.

## 16. Bao phủ các module khác bằng pattern chung

Redesign phải đi qua mọi màn hiện có của bốn role, không dừng ở các mẫu hero. Những màn dưới dùng cùng shell, list/detail, field, state và permission đã quy định; không tạo nghiệp vụ mới.

| Module | Role và trọng tâm | Pattern / trường cần rõ |
|---|---|---|
| Catalogue/SKU | Owner sửa giá/trạng thái; các role khác xem theo quyền | Danh mục → model → SKU; giá hiện tại khác snapshot; tương thích phụ kiện; archive thay xóa lịch sử |
| Khách hàng | Owner/order staff | Tìm khách → liên hệ theo quyền → đơn/hậu mãi/điểm/credit; không gộp điểm và số dư thu cũ |
| Thanh toán/đối soát | Owner/order staff | Banking chờ xác nhận, COD chưa thu, receipt/attempt/refund tách; summary mã/tiền/nguồn trước xác nhận |
| Trả hàng/hoàn | Order tiếp nhận, kho kiểm tra, owner tài chính | Case header → món/serial → bằng chứng → kết quả kiểm định → đề nghị/duyệt/thực hiện theo quyền; credit note khác refund |
| Thu cũ | Order điều phối/báo giá, kho kiểm định, owner ngoại lệ | Theo từng món; ước tính/quote version/khách chấp nhận/credit tách; hàng cũ không cộng vào tồn mới |
| BotZone Care | Order hồ sơ, kho nhận/kiểm, owner ngoại lệ | Ngày bảo hành gốc + 12 tháng gia hạn, món/serial, sửa/đổi cùng SKU/hoàn theo BA; không thêm gói rơi vỡ |
| Voucher/điểm/hạng | Owner cấu hình; order tra cứu; content dùng chiến dịch duyệt | Điều kiện, hiệu lực, quota giữ/đã dùng, version; không sửa lịch sử redemption |
| Nhà cung cấp/đơn mua | Owner quản lý, kho thực hiện | Ordered/received/remaining; nhận một phần; giá vốn theo quyền; danh sách và chi tiết thống nhất |
| Tồn/serial | Kho thao tác, owner duyệt; order tra cứu | Khả dụng/đang giữ/chờ kiểm/đã xuất tách; tìm một serial xem lịch sử; không sửa tổng tồn tùy ý |
| Thư viện media | Content biên tập; owner giám sát | Thumbnail/tên/loại/dung lượng/đang dùng; không xóa media được bản công khai tham chiếu |

## 17. Trạng thái dùng chung

| Trạng thái | Giao diện và hành vi |
|---|---|
| Loading lần đầu | Giữ vùng dữ liệu ổn định; progress/placeholder đúng panel; toolbar không biến mất |
| Refresh có dữ liệu cũ | Giữ bảng và selection, hiển thị đang cập nhật và thời điểm dữ liệu; không nháy trống toàn trang |
| Chưa có dữ liệu | Giải thích ngắn + action tạo/tiếp nhận nếu có quyền và luồng tồn tại |
| Không khớp bộ lọc | Ghi “Không có kết quả phù hợp”; nút Xóa bộ lọc; giữ điều kiện để sửa |
| Validation | Thông báo cạnh field + focus đầu lỗi; giữ toàn bộ input hợp lệ |
| Mất kết nối/timeout | Giữ dữ liệu/input, nhãn lỗi dễ hiểu và Thử lại; không tự báo giao dịch thất bại nếu chưa biết kết quả commit |
| Đang gửi command | Chặn gửi trùng ở action đó; UI vẫn đáp ứng, server vẫn kiểm idempotency |
| Thành công | Cập nhật từ phản hồi thật; thông báo nhỏ cạnh ngữ cảnh; không modal “Thành công” cho mọi lần lưu |
| 401 | Hướng dẫn đăng nhập lại, giữ bản nháp an toàn theo cơ chế cho phép; không lưu secret lộ thiên |
| 403 | Báo quyền đã thay đổi/không được phép; cập nhật nav; không để role cũ tiếp tục ghi |
| 409/version conflict | Nêu dữ liệu đã thay đổi; tải lại/so sánh theo API; không ghi đè âm thầm |
| Không có quyền | Ẩn menu/action; khi truy cập trực tiếp vẫn báo từ chối từ API |
| Thiếu điều kiện | Disable action kèm “Còn 1 serial chưa gán” hoặc lý do server; dẫn tới chỗ xử lý |
| Đang xuất file | Progress phù hợp, chống bấm lặp; lỗi cho thử lại; kết quả lưu rõ đường dẫn |
| Rời form dirty | Lưu/Bỏ thay đổi/Ở lại; Escape không bỏ dữ liệu âm thầm |

Các toast chỉ hỗ trợ, không là nơi duy nhất hiển thị lỗi. Màu + chữ + icon nhất quán. Không đổi focus bất ngờ khi bảng refresh.

## 18. Bàn phím và nghiệm thu

| Phím | Tác dụng |
|---|---|
| Ctrl+K | Mở tìm điều hướng trong các module được phép; chọn kết quả rồi Enter, không thực hiện command tài chính |
| Ctrl+F | Focus tìm kiếm của màn danh sách hiện tại; không cướp hành vi tìm trong editor text đang có chức năng riêng |
| Ctrl+S | Lưu draft/form hiện tại nếu có quyền và hợp lệ; tuyệt đối không đồng nghĩa Xuất bản, Bàn giao, Hoàn tiền |
| F5 | Refresh màn/list; nếu dirty thì hỏi giữ/lưu/bỏ, không xóa input im lặng |
| Enter | Thao tác mặc định theo focus: đăng nhập, tìm/quét, mở dòng; không kích hoạt destructive command từ focus ngẫu nhiên |
| Esc | Đóng menu/popover, đóng drawer hoặc quay lại một cấp; dirty guard vẫn áp dụng |
| Tab/Shift+Tab | Thứ tự hợp lý, focus luôn nhìn thấy, không kẹt trong custom panel |

Phím tắt hiển thị trong tooltip/menu có nhãn. Không thêm phím một ký tự làm phát hành/hoàn/xóa. Các lệnh scanner trong kho có context riêng, không rơi vào AcceptButton của toàn form.

**Nghiệm thu tối thiểu:** LOGIN và trang vào cả 4 role; các luồng ORD-QUEUE → ORD-DETAIL, WH-WORK → WH-RECEIPT/WH-PICK, CNT-HOME → CNT-EDITOR/CNT-PRODUCT, INV-LIST → filter → INV-DETAIL → export, REP-INVOICE → drill-down; settings/quyền/nhật ký và toàn bộ module đang có đều dùng shared components. Kiểm ở 1366 × 768 scale 100%, 1920 × 1080 scale 125%/150%, resize trong work area và thao tác bàn phím. Không dùng screenshot HTML mẫu để thay bằng chứng runtime WinForms trên Windows.
