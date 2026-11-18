# BotZone Admin — Checklist nghiệm thu redesign

Áp dụng cho **WinForms native trên laptop/PC Windows**. Đây là checklist để Codex điền sau khi triển khai; các ô trống không có nghĩa đã đạt. Mốc đối chiếu: BA v1.1, `01_REDESIGN_SPEC.md`, hành vi/API trước khi sửa và các yêu cầu mới đã được người dùng chốt.

Mỗi mục ghi kết quả **Đạt / Chưa đạt / Chưa kiểm / Không áp dụng**, đường dẫn bằng chứng và vấn đề còn lại. “Không áp dụng” phải có lý do dựa trên repo/phạm vi, không dùng để bỏ module BA. Một bằng chứng có thể chứng minh nhiều mục; không cần tạo bài test mới chỉ để lặp lại implementation.

## A. Giao diện, bố cục và phạm vi

- [ ] **UX-01 — Native và đúng phạm vi.** Login, shell, bảng, form và CMS là control WinForms; WebView2 nếu có chỉ dành preview/PDF. Admin chạy trên Windows laptop/PC; không xây mobile admin hoặc một admin web khác. Ghi tên project/framework thực tế đã kiểm tra.
- [ ] **UX-02 — Hệ thiết kế nhất quán.** Token màu, Segoe UI, spacing, radius, trạng thái chọn/focus và component dùng lại khớp đặc tả. Chữ Việt có dấu đọc rõ; tiền/mã không bị cắt; màu trạng thái luôn kèm chữ hoặc icon có nghĩa.
- [ ] **UX-03 — Bao phủ toàn bộ màn.** Lập bảng map ID trong screen inventory → form/control/route nội bộ thực tế. Mọi module BA có đường vào dùng được theo role; không để màn trắng, nút chết hoặc số giả thay dữ liệu. Chức năng thiếu từ baseline được ghi thành gap riêng, không âm thầm coi là đã làm.
- [ ] **UX-04 — Bảng và form có thứ bậc rõ.** Cột ưu tiên, tìm/lọc, phân trang, tổng và CTA chính nhìn thấy; một dòng mở được chi tiết mà không chỉ phụ thuộc double-click. Quay lại giữ filter, sort, trang và lựa chọn hợp lý; hành động ít dùng không che nhiệm vụ chính.
- [ ] **UX-05 — DPI trên Windows.** Quan sát tại 1366×768 scale100% và125%, 1920×1080 scale125% và150% trong work area thực. Không cắt tiêu đề, field, nút chính hoặc tổng; dialog dài cuộn được. Ghi cả độ phân giải, scale và client size, không chỉ kích thước ảnh.
- [ ] **UX-06 — Resize theo diện tích thật.** Kiểm client width logical ≥1440, 1120–1439, 900–1119 và dưới900: split detail chỉ khi đủ chỗ, detail theo yêu cầu/trang con và sidebar gọn đúng thiết kế. Dưới900 vẫn tới được hành động; cuộn ngang của grid không làm mất dữ liệu.

## B. Luồng công việc của từng vai trò

- [ ] **UX-07 — Chủ cửa hàng.** Từ tổng quan mở một chỉ số tới tập dữ liệu tương ứng; mở một ngoại lệ cần duyệt, đọc bằng chứng và tác động tiền/tồn, thực hiện hành động được phép rồi trở lại hàng chờ. Chỉ số có tên/phạm vi/thời điểm dữ liệu; không duyệt mù từ card.
- [ ] **UX-08 — Nhân viên xử lý đơn.** Tìm một đơn trong view công việc, mở chi tiết, hoàn thành bước tiếp theo hợp lệ và theo dõi kho/giao/hóa đơn. Thanh toán và fulfillment hiển thị riêng; COD đã giao nhưng chưa đối soát không bị mô tả là đã thu tiền.
- [ ] **UX-09 — Kho xuất hàng.** Mở phiếu được giao, quét/nhập đúng serial/IMEI, xử lý một mã sai/trùng và một phụ kiện theo số lượng/lô. UI chỉ rõ dòng lỗi, không mất thông tin đã nhập; thao tác hoàn tất nối đúng command hiện có.
- [ ] **UX-10 — Nhập hàng và kiểm kê.** Nhận một phần theo phiếu, nhìn rõ còn thiếu; kiểm đếm một món có chênh lệch, phân biệt chưa đếm với0, lưu và đề nghị duyệt theo quyền. Không thay việc lập phiếu bằng sửa trực tiếp số tồn/giá vốn trên grid.
- [ ] **UX-11 — Nhân viên nội dung.** Từ việc cần hoàn thiện mở đúng sản phẩm/locale/field; sửa và lưu nháp, xem đúng bản preview, xuất bản và xem lịch sử theo quyền. Lưu nháp khác xuất bản; chuyển VI/EN/rời form không làm mất sửa đổi âm thầm. Preview mobile chỉ mô phỏng storefront.
- [ ] **UX-12 — Hậu mãi và các module hỗ trợ.** Đi qua một hồ sơ trả hàng từng món, một hồ sơ thu cũ và một hồ sơ Care theo các bước role được phép; kiểm lối truy cập supplier, promotion và loyalty. Nhãn thời hạn, báo giá phiên bản, credit/điểm/tiền và bước tiếp theo giữ đúng BA, không thêm hoặc bỏ điều kiện nghiệp vụ.

## C. Quyền truy cập

- [ ] **UX-13 — Quyền xuyên suốt.** Đăng nhập cả bốn tài khoản role, xác nhận trang đầu/menu/action đúng ma trận. Thử một action vượt quyền qua đường gọi hiện có và xác nhận server từ chối; ẩn menu không được dùng thay kiểm quyền. Đổi quyền trong phiên được nhận biết trước lần ghi/xuất tiếp theo.
- [ ] **UX-14 — Owner và landing.** Chủ cửa hàng xem được nội dung theo BA nhưng không sửa, publish hoặc restore landing bằng nút, shortcut hay API. Tài khoản content thực hiện được các thao tác này trong phạm vi hợp lệ.
- [ ] **UX-15 — Nội dung và tài chính.** Nhân viên nội dung không xem/xuất hóa đơn tài chính, sửa giá/tồn hoặc thực hiện hoàn tiền. Form biên tập chỉ ghi các field được phép; dữ liệu read-only không bị gửi ngược như thay đổi hợp lệ.
- [ ] **UX-16 — Phạm vi nhân viên xử lý đơn.** Báo cáo chỉ thuộc responsible_staff_id của nhân viên đang đăng nhập; filter OR/NOT hoặc chọn ID khác không vượt scope. Không lộ giá vốn/lãi gộp. Hóa đơn phục vụ xử lý đơn vẫn truy cập đúng quyền BA.
- [ ] **UX-17 — Phạm vi nhân viên kho.** Kho chỉ xem chứng từ/thông tin khách cần nhiệm vụ; không mở danh sách/xuất hóa đơn tài chính. Xem chi phí phiếu nhập được giao, nếu được cấp, không trở thành quyền sửa giá vốn hay tự duyệt điều chỉnh ngoại lệ.

## D. Hóa đơn, database và dữ liệu không bị thay đổi sai

- [ ] **UX-18 — Chứng từ và số tiền giữ nguyên nghĩa.** Đối chiếu một hóa đơn issued trước–sau: snapshot/dòng/tổng không bị sửa bởi redesign. Tổng HĐ phát hành, điều chỉnh giảm, ròng phát sinh kỳ, tiền thu/hoàn và lãi gộp có nhãn riêng; store credit không bị coi là giảm giá hoặc tiền thu mới.
- [ ] **UX-19 — Bộ lọc nâng cao.** Thực hiện >, <, =, <>, AND, OR, NOT và nhóm lồng trong giới hạn BA; kiểm giá trị trống, ngày và “không chứa sản phẩm”. Mô tả tiếng Việt khớp biểu thức gửi API; input theo kiểu dữ liệu, không ghép SQL tự do hay áp scope chỉ ở client.
- [ ] **UX-20 — Danh sách, tổng và phân trang.** Với tập dữ liệu vượt một trang, count/tổng toàn bộ kết quả không bằng nhầm tổng trang. Sort/filter dùng query contract hiện có, giữ thứ tự ổn định; đổi trang không thay phạm vi. Một hóa đơn nhiều dòng không bị cộng tổng nhiều lần.
- [ ] **UX-21 — Ba báo cáo và drill-down.** Kiểm thời gian tháng/quý/năm qua nhiều năm, nhân viên–quý/năm, sản phẩm–tháng/quý/năm; ô thống kê mở đúng chứng từ/dòng nguồn. Giữ kỳ0, nhân viên inactive trong lịch sử và phân biệt lọc hóa đơn có sản phẩm với cộng riêng dòng sản phẩm.
- [ ] **UX-22 — XLSX/PDF thực và đúng phạm vi.** Xuất một hóa đơn, danh sách theo filter và báo cáo; count/tổng/điều kiện/thời điểm khớp dataset xuất. Excel/PDF cùng lượt dùng cùng dataset theo API; không chỉ xuất trang đang xem hoặc cắt âm thầm. Mở file có chữ Việt, serial giữ dạng text; SaveFileDialog/lưu file được quan sát trên Windows.
- [ ] **UX-23 — Không hồi quy giao dịch.** Chạy lại các kiểm thử có sẵn liên quan nghiệp vụ bị chạm: khuyến mãi DB, tiền/tồn/serial/credit/điểm, phát hành/điều chỉnh/hoàn. UI vẫn gọi service/API hiện có; không tạo migration/backend khác hoặc tính lại công thức trong event handler. Chỉ bổ sung test để xử lý rủi ro cụ thể phát hiện được.

## E. Bàn phím, lỗi và bất đồng bộ

- [ ] **UX-24 — Điều khiển bằng bàn phím.** Hoàn thành đăng nhập, chọn menu, tìm đơn/hóa đơn, mở chi tiết, sửa form và đóng dialog bằng Tab/Shift+Tab/Enter/Escape/phím tắt phù hợp. Focus luôn nhìn thấy, label/accessibility name đúng; ô quét và editor không bị shortcut toàn cục chiếm nhầm.
- [ ] **UX-25 — Validation và dữ liệu chưa lưu.** Field lỗi có thông báo tại chỗ và focus phù hợp; rời form dirty có Lưu/Không lưu/Ở lại. Lưu thất bại giữ dữ liệu nhập; nhãn “Đã lưu/Đã xuất bản/Đã hoàn tất” chỉ xuất hiện sau phản hồi thật.
- [ ] **UX-26 — Lỗi kết nối, phiên và xung đột.** Quan sát mất kết nối,401,403,409 trong luồng liên quan. Thông báo đúng nguyên nhân, có hành động tiếp tục phù hợp và không lộ bí mật/SQL. Xung đột không ghi đè âm thầm; dữ liệu cũ hoặc nháp còn lại được gắn trạng thái rõ.
- [ ] **UX-27 — UI không treo và kết quả đúng thứ tự.** Khi API chậm hoặc đổi filter liên tục, cửa sổ vẫn resize/điều hướng được trong giới hạn hợp lý; request cũ không ghi đè request mới. Loading đúng vùng, timeout/cancellation được xử lý; không dùng `.Wait()`/`.Result` chặn UI thread.
- [ ] **UX-28 — Gửi lặp và batch.** Double-click/retry không ghi hai giao dịch; button đang gửi có phản hồi. Batch có count/phạm vi rõ, báo thành công/thất bại theo dòng và retry đúng phần lỗi. Không lấy thông báo thành công chung để che kết quả từng phần.

## F. Bằng chứng và khả năng bàn giao

- [ ] **UX-29 — Runtime Windows thật.** Có bằng chứng mở executable trên Windows, đăng nhập bốn role, dùng control native và lưu file. Ghi OS/runtime/client size/DPI. Build hoặc mockup HTML chỉ là bằng chứng loại tương ứng, không thay runtime WinForms.
- [ ] **UX-30 — Lệnh kiểm tra có kết quả.** Lưu command/exit code/output cho build và các gate có sẵn liên quan; phân biệt lỗi có từ baseline với lỗi do thay đổi. Không bịa tên solution/test hoặc đánh dấu pass khi chưa chạy. Lỗi ngoài phạm vi được ghi rõ tác động, không tự thêm gate không liên quan.
- [ ] **UX-31 — Ảnh trước–sau có thể so sánh.** Có ảnh cùng màn/role, dữ liệu tương đương, viewport và DPI; những trạng thái chính trong bảng bằng chứng dưới đây được phủ. Ảnh có tên/ID rõ và thông tin môi trường, che dữ liệu nhạy cảm nếu xuất hiện.
- [ ] **UX-32 — Bàn giao đúng thực tế.** Có screen mapping, file/component đã đổi, lệnh chạy, kết quả32mục, ảnh/log và gap còn lại. Ghi rõ mục chưa kiểm, không dùng ảnh thiết kế làm bằng chứng đã triển khai. Không khẳng định giảm phần trăm thao tác/thời gian hoặc “hoàn hảo” nếu chưa đo.

## Bảng bằng chứng trước–sau

Bảng là nhóm bằng chứng để tránh chụp trùng. Có thể cần nhiều ảnh cho một nhóm nếu trạng thái không thể thấy trong một ảnh. Tên file minh họa; Codex ghi đường dẫn thật. Không ghi mật khẩu/token vào ảnh hoặc log.

| Evidence ID | Screen ID / trạng thái | Ảnh trước | Ảnh sau | Môi trường và thao tác xác nhận | Kết quả |
|---|---|---|---|---|---|
| EV-01 | LG-01: mặc định, lỗi kết nối/đăng nhập | Chưa ghi | Chưa ghi | OS, size, DPI; Tab/Enter, hiện mật khẩu | Chưa kiểm |
| EV-02 | OW-01/02: tổng quan → ngoại lệ | Chưa ghi | Chưa ghi | Role Owner; chỉ số → danh sách → hồ sơ | Chưa kiểm |
| EV-03 | OR-01/02: list/detail, trạng thái COD | Chưa ghi | Chưa ghi | Role Order; mở/quay lại giữ filter | Chưa kiểm |
| EV-04 | WH-01/02: xuất theo serial/số lượng | Chưa ghi | Chưa ghi | Role Kho; mã đúng/sai/trùng | Chưa kiểm |
| EV-05 | PU-03/WH-05: nhập một phần, kiểm kê | Chưa ghi | Chưa ghi | Role Kho/Owner theo bước; chênh lệch | Chưa kiểm |
| EV-06 | CT-01/02: hàng chờ, sửa VI–EN | Chưa ghi | Chưa ghi | Role Content; thiếu field → sửa/lưu | Chưa kiểm |
| EV-07 | CT-03/04: nháp/preview/publish | Chưa ghi | Chưa ghi | Role Content; Owner chỉ xem | Chưa kiểm |
| EV-08 | IN-01/02: danh sách/chi tiết issued | Chưa ghi | Chưa ghi | Filter/count/tổng; snapshot read-only | Chưa kiểm |
| EV-09 | IN-03: nhóm AND/OR/NOT | Chưa ghi | Chưa ghi | Mô tả filter và request/kết quả theo scope | Chưa kiểm |
| EV-10 | RP-01/02/03: kỳ/nhân viên/sản phẩm | Chưa ghi | Chưa ghi | Số liệu đối chiếu, drill-down nguồn | Chưa kiểm |
| EV-11 | IN-04: xuất và file đã mở | Chưa ghi | Chưa ghi | SaveFileDialog Windows; count/tổng/tiếng Việt | Chưa kiểm |
| EV-12 | SH-01/SY-01 và action vượt quyền | Chưa ghi | Chưa ghi | Bốn role; kết quả server từ chối và UI | Chưa kiểm |

Với module còn lại trong inventory, ghi trạng thái đã rà soát và ảnh khi có thay đổi riêng đáng kể; có thể dùng cùng bằng chứng shared component nếu bố cục tương đương. Không dùng ảnh duy nhất của dashboard để nghiệm thu toàn bộ admin.

## Nhật ký kiểm tra cần điền

| Nhóm | Dữ liệu cần ghi |
|---|---|
| Baseline | Commit/working tree trước sửa; solution/project thực tế; phiên bản .NET; lỗi có sẵn; ảnh trước. |
| Build/gate | Câu lệnh nguyên văn, thư mục chạy, thời điểm, exit code, log và phạm vi. |
| Windows runtime | Phiên bản Windows/.NET, cách chạy, resolution, scale, client size, role/tài khoản demo. |
| Dữ liệu | Bộ seed/fixture hoặc hồ sơ thử; mã chứng từ liên quan; kỳ/filter; kết quả mong đợi theo BA hoặc baseline. |
| Kết quả task | Bước thực hiện, kết quả thật, ảnh/log, Đạt/Chưa đạt/Chưa kiểm và issue còn lại. |
| File xuất | Export ID nếu có, count/tổng/điều kiện, đường dẫn, định dạng, cách mở kiểm; không lộ file ngoài quyền. |

**Cách xác định “Đạt”:** kết quả quan sát khớp yêu cầu cụ thể của mục, dữ liệu/API và quyền BA không sai, có bằng chứng đúng loại. Chỉ thay màu, build thành công hoặc ảnh prototype đẹp chưa đủ để kết luận luồng nghiệp vụ và runtime đạt. Chỉ số cải thiện UX nếu muốn báo phải có cùng task, dữ liệu và điều kiện đo trước–sau; không bắt buộc đặt mục tiêu phần trăm tùy ý.
