# BotZone Admin — Bản đồ ID màn hình và mẫu trực quan

Phiên bản 1.0 · 17/11/2026. Áp dụng cho **ứng dụng WinForms native trên laptop và PC Windows**. Không có admin mobile; khung mobile trong CMS chỉ là preview website bán hàng.

## 1. Cách đọc ba loại ID

- **ID nghiệp vụ** như `OR-01`, `CT-03`: danh mục màn/nhóm chức năng trong `01_REDESIGN_SPEC.md`, dùng để theo dõi phạm vi triển khai và nghiệm thu.
- **ID blueprint/visual** như `ORD-QUEUE`, `CNT-EDITOR`: tên mẫu bố cục trong `02_SCREEN_BLUEPRINTS.md`. Một số ID chỉ có đặc tả bằng chữ, chưa có ảnh riêng.
- **ID bằng chứng** như `EV-03`: nhóm ảnh/log trước–sau trong `04_ACCEPTANCE_CHECKLIST.md`; không phải màn hình bổ sung.

**14 mockup trong `visuals/screens.json` là các pattern thiết kế đại diện, không phải giới hạn phạm vi.** Toàn bộ module và ID ở `01_REDESIGN_SPEC.md` vẫn phải được đối chiếu và redesign. Màn chưa có ảnh riêng dùng pattern phù hợp, đặc tả BA và component chung; không được bỏ màn, tạo nút giả hoặc ghi đã nghiệm thu chỉ vì có ảnh mẫu.

ID là nhãn tham chiếu thiết kế, không bắt buộc đổi tên class/Form hiện có. Codex thêm cột tên Form/UserControl/command thực tế vào tracking và giữ convention repo. Một chức năng có nhiều ID tham chiếu ở đây vẫn chỉ cần một implementation phù hợp.

## 2. Danh sách chính xác 14 mẫu có ảnh

Mỗi ID bên dưới có `visuals/<ID>.png` và `visuals/<ID>.svg`; metadata/title/notes/hotspots nằm trong `visuals/screens.json`. Các ảnh là thiết kế đề xuất, không phải screenshot executable Windows.

| Visual ID | ID nghiệp vụ đối chiếu | Phần được minh họa | Phần không được suy là đã có ảnh/nghiệm thu |
|---|---|---|---|
| `LOGIN` | `LG-01`; biến thể trạng thái `LG-02` | Đăng nhập chung | Xác thực/phiên thực, mọi lỗi và bốn role vẫn cần kiểm riêng |
| `OWN-HOME` | `OW-01`; hàng chờ thuộc `OW-02` | Tổng quan và lối vào quyết định | Chi tiết mọi loại đề nghị/phê duyệt chưa có ảnh riêng |
| `ORD-QUEUE` | `OR-01` | Hàng chờ đơn và preview chi tiết | Toàn bộ biến thể filter/batch/trạng thái không nằm trong một ảnh |
| `ORD-DETAIL` | `OR-02`; vùng liên quan `OR-03`, `OR-04` | Chi tiết đơn, tiền/giao nhận và bước kế tiếp | Không đại diện đầy đủ mọi dialog đối soát, hoàn hoặc giao thất bại |
| `WH-WORK` | `WH-01`; vùng thao tác của `WH-02` | Công việc kho và quét/chuẩn bị hàng | Các luồng tồn/serial/kiểm định khác vẫn trong scope |
| `WH-RECEIPT` | `PU-03`; liên quan `PU-02` | Nhận hàng/đối chiếu theo phiếu | Kiểm kê `WH-05` chỉ học pattern cột so sánh, không đồng nhất nghiệp vụ |
| `CNT-HOME` | `CT-01` | Hàng chờ nội dung | Cách tính trạng thái phải lấy dữ liệu/schema thật |
| `CNT-EDITOR` | `CT-03`; lối vào hành động `CT-04` | Landing studio theo section | Preview/publish/history/restore phải triển khai và kiểm riêng theo BA |
| `CNT-PRODUCT` | `CT-02`; vùng chọn media liên quan `CT-05` | Biên tập sản phẩm VI–EN | Chưa phải toàn bộ thư viện media và quản lý tham chiếu |
| `INV-LIST` | `IN-01` | Danh sách hóa đơn, filter/tổng/phạm vi | Không thay ảnh chi tiết hóa đơn hoặc file xuất thật |
| `INV-FILTER` | `IN-03` | Bộ lọc biểu thức native | Không chứng minh AST/permission/query server đã đúng |
| `REP-INVOICE` | `RP-01`, `RP-02`, `RP-03` | Họ màn thống kê theo kỳ/nhân viên/sản phẩm | Mỗi tab/phạm vi và drill-down vẫn cần dữ liệu thật để nghiệm thu |
| `LAPTOP-COMPACT` | Biến thể `SH-01`, `OR-01` | Bố cục laptop gọn 1068×584 logical | Không phải module/role mới; không thay kiểm DPI Windows |
| `SHARED-STATES` | Trạng thái dùng chung của toàn inventory | Field/focus/lỗi/empty/permission/conflict | Không phải trang production phải thêm vào menu |

Các mẫu còn lại có canvas 1440×900. Kích thước ảnh không được dùng làm minimum size cố định cho app. Hành vi co giãn tuân logical client width và work area trong đặc tả.

## 3. Map toàn bộ inventory, không cắt module

Quy ước cột cuối: **Ảnh** = có mẫu trực tiếp hoặc một phần được ghi rõ; **Chữ** = blueprint mô tả nhưng không có ảnh riêng; **Pattern** = dùng cấu trúc chung, vẫn phải dựng đầy đủ màn nghiệp vụ.

| ID trong 01/04 | Chức năng phải giữ | Blueprint / visual tham chiếu | Mức phủ và lưu ý |
|---|---|---|---|
| `LG-01` | Đăng nhập | `LOGIN` | Ảnh trực tiếp |
| `LG-02` | Phiên hết hạn/đăng nhập lại | `LOGIN`, `SHARED-STATES` | Biến thể trạng thái; không tạo auth mới |
| `SH-01` | Shell, điều hướng, tài khoản, kết nối | Tất cả mẫu; `LAPTOP-COMPACT` | Pattern dùng chung; role/menu theo quyền |
| `OW-01` | Tổng quan cửa hàng | `OWN-HOME` | Ảnh trực tiếp |
| `OW-02` | Hàng chờ/chi tiết cần chủ duyệt | `OWN-HOME`; pattern detail của `ORD-DETAIL` | Ảnh hàng chờ; detail theo từng loại hồ sơ, không sao chép command đơn hàng |
| `OR-01` | Danh sách/hàng chờ đơn | `ORD-QUEUE`, `LAPTOP-COMPACT` | Ảnh trực tiếp và biến thể hẹp |
| `OR-02` | Chi tiết đơn/timeline | `ORD-DETAIL` | Ảnh trực tiếp |
| `OR-03` | Thanh toán/đối soát banking, COD, attempts | `ORD-DETAIL`; §16 của 02 | Ảnh một phần + chữ; giữ trạng thái/giao dịch riêng |
| `OR-04` | Giao nhận, vận đơn, thất bại/gửi lại | `ORD-DETAIL`; §16 của 02 | Pattern detail/timeline; không gộp với payment |
| `IN-01` | Danh sách hóa đơn | `INV-LIST` | Ảnh trực tiếp |
| `IN-02` | Chi tiết hóa đơn/chứng từ giảm | `INV-DETAIL` trong §13 của 02 | Chữ; **không có `INV-DETAIL` trong screens.json** |
| `IN-03` | Bộ lọc nâng cao | `INV-FILTER`; §12 của 02 | Ảnh trực tiếp |
| `IN-04` | Xuất hóa đơn/danh sách/báo cáo | Mục Xuất trong §12 của 02 | Chữ; cần dialog, SaveFileDialog và file thực |
| `WH-01` | Công việc kho | `WH-WORK` | Ảnh trực tiếp |
| `WH-02` | Phiếu xuất, quét, đóng gói | `WH-PICK` trong §7 của 02; `WH-WORK` | Chữ + vùng thao tác mẫu; **không có ảnh riêng `WH-PICK`** |
| `WH-03` | Tồn/sổ tồn | §16 của 02; pattern bảng `WH-WORK` | Pattern; trạng thái tồn và đơn vị theo BA |
| `WH-04` | Serial/IMEI/lịch sử thiết bị | §16 của 02; pattern bảng/detail | Pattern; không dùng danh sách serial minh họa làm dữ liệu thật |
| `WH-05` | Kiểm kê | `WH-COUNT` trong §8 của 02 | Chữ; **không có ảnh riêng `WH-COUNT`**; cột Sổ sách/Thực đếm/Chênh lệch |
| `WH-06` | Đề nghị/duyệt điều chỉnh | `WH-COUNT`, `OWN-HOME`; pattern detail | Pattern; kho đề nghị, owner duyệt ngoại lệ |
| `PU-01` | Nhà cung cấp | §16 của 02; pattern list/detail | Pattern; vẫn thuộc scope |
| `PU-02` | Đơn mua/phiếu nhập | §16 của 02; `WH-RECEIPT` | Pattern list/detail và ảnh phần nhận |
| `PU-03` | Nhận đủ/một phần, mã/lô/giá vốn | `WH-RECEIPT` | Ảnh trực tiếp; giá vốn theo quyền |
| `CA-01` | Nhóm/model/thuộc tính | §16 của 02; pattern list/form | Pattern; không đồng nhất với editor mô tả |
| `CA-02` | SKU/giá/trạng thái bán | §16 của 02; pattern list/detail | Pattern; owner quản lý, content read-only |
| `CA-03` | Tương thích phụ kiện | §16 của 02; pattern picker/list | Pattern; giữ quan hệ catalogue hiện có |
| `CU-01` | Danh sách khách | §16 của 02; pattern list/detail | Pattern theo quyền khách hàng |
| `CU-02` | Khách, đơn, điểm, credit, hậu mãi | §16 của 02; pattern detail/tabs/timeline | Pattern; điểm/credit/tiền không gộp |
| `PR-01` | Voucher/phiên bản | §16 của 02; pattern list/form | Pattern theo quyền |
| `PR-02` | Điều kiện, hiệu lực, lượt dùng | §16 của 02; pattern detail/list | Pattern; quota đang giữ và đã dùng tách rõ |
| `PR-03` | Chính sách điểm/hạng và sổ phát sinh | §16 của 02; pattern form/ledger | Pattern; không chỉnh lịch sử đã ghi |
| `AS-01` | Hàng chờ hậu mãi | §16 của 02; pattern `ORD-QUEUE` | Pattern hàng chờ, không dùng state machine đơn cho hậu mãi |
| `AS-02` | Trả hàng từng món/bằng chứng | §16 của 02; pattern detail/checklist | Pattern; thời hạn/điều kiện theo BA |
| `AS-03` | Đề nghị/duyệt/thực hiện hoàn | §16 của 02; pattern detail/confirmation | Pattern; credit note khác refund |
| `TI-01` | Hàng chờ thu cũ | §16 của 02; pattern hàng chờ | Pattern; toàn bộ nhóm sản phẩm theo BA |
| `TI-02` | Tiếp nhận/kiểm định thu cũ | §16 của 02; pattern checklist/detail | Pattern; serial hoặc mã nội bộ đúng nhóm |
| `TI-03` | Báo giá phiên bản/phản hồi | §16 của 02; pattern detail/timeline | Pattern; ước tính khác báo giá được chấp nhận |
| `TI-04` | Sổ credit/gửi trả | §16 của 02; pattern ledger/timeline | Pattern; credit chỉ cấp theo điều kiện BA |
| `WC-01` | Gói/hợp đồng Care | §16 của 02; pattern list/detail | Pattern; thời hạn bảo hành chuẩn + gia hạn |
| `WC-02` | Tiếp nhận/xử lý/trả máy | §16 của 02; pattern case detail | Pattern theo role/bước |
| `WC-03` | Sửa/đổi serial/hoàn trong hồ sơ | §16 của 02; pattern action/timeline | Pattern; không tạo doanh thu bán mới khi đổi bảo hành |
| `CT-01` | Việc nội dung | `CNT-HOME` | Ảnh trực tiếp |
| `CT-02` | Nội dung sản phẩm VI–EN/SEO | `CNT-PRODUCT` | Ảnh trực tiếp |
| `CT-03` | Landing studio | `CNT-EDITOR` | Ảnh trực tiếp |
| `CT-04` | Preview/publish/history/restore | `CNT-EDITOR`; §10 của 02 | Ảnh lối vào + chữ; các trạng thái/version phải kiểm riêng |
| `CT-05` | Media ảnh/3D và vị trí sử dụng | `CNT-PRODUCT`, `CNT-EDITOR`; §16 của 02 | Ảnh vùng chọn + pattern thư viện; không bỏ quản lý tham chiếu |
| `RP-01` | Hóa đơn theo thời gian | `REP-INVOICE` | Họ ảnh báo cáo; đúng kỳ/phạm vi |
| `RP-02` | Hóa đơn theo nhân viên | `REP-INVOICE` | Họ ảnh báo cáo; responsible_staff_id và scope thật |
| `RP-03` | Hóa đơn theo sản phẩm | `REP-INVOICE` | Họ ảnh báo cáo; cấp dòng hàng/Model/SKU đúng nghĩa |
| `RP-04` | Tiền thu/hoàn | Pattern bảng/chart/filter `REP-INVOICE` | Pattern; **không dùng công thức hóa đơn thay dòng tiền** |
| `RP-05` | Tồn/giá vốn/lãi gộp | §16 của 02; pattern báo cáo | Pattern; quyền và công thức riêng BA |
| `RP-06` | Top SKU, ưu đãi, thu cũ, Care | §16 của 02; pattern báo cáo | Pattern; từng chỉ tiêu có nguồn/phạm vi riêng |
| `SY-01` | Nhân viên/phân quyền | `SYS-SETTINGS` trong §15 của 02 | Chữ; không có ảnh riêng trong screens.json |
| `SY-02` | Cấu hình/chính sách/tỷ giá | `SYS-SETTINGS` trong §15 của 02 | Chữ; không đổi snapshot giao dịch cũ |
| `SY-03` | Nhật ký | `SYS-AUDIT` trong §15 của 02 | Chữ; scope nhật ký theo role |
| `SY-04` | Công cụ demo được cấp quyền | §15 của 02; pattern form/action | Pattern; chỉ xuất hiện/hoạt động đúng môi trường và quyền |

## 4. Map nhóm bằng chứng nghiệm thu

Ảnh thiết kế là điểm đối chiếu thẩm mỹ; cột bằng chứng của file04 vẫn phải điền ảnh/log **từ app thực tế** sau triển khai.

| Evidence ID trong 04 | Numeric ID | Visual/blueprint để đối chiếu |
|---|---|---|
| `EV-01` | `LG-01`, `LG-02` | `LOGIN`, `SHARED-STATES` |
| `EV-02` | `OW-01`, `OW-02` | `OWN-HOME` + detail theo loại đề nghị |
| `EV-03` | `OR-01`, `OR-02`, phần `OR-03` | `ORD-QUEUE`, `ORD-DETAIL`, `LAPTOP-COMPACT` |
| `EV-04` | `WH-01`, `WH-02` | `WH-WORK`, blueprint `WH-PICK` |
| `EV-05` | `PU-03`, `WH-05`, phần `WH-06` | `WH-RECEIPT`, blueprint `WH-COUNT` |
| `EV-06` | `CT-01`, `CT-02` | `CNT-HOME`, `CNT-PRODUCT` |
| `EV-07` | `CT-03`, `CT-04` | `CNT-EDITOR` + trạng thái/version thực |
| `EV-08` | `IN-01`, `IN-02` | `INV-LIST`, blueprint `INV-DETAIL` |
| `EV-09` | `IN-03` | `INV-FILTER` |
| `EV-10` | `RP-01`, `RP-02`, `RP-03` | `REP-INVOICE` + từng tab dữ liệu thực |
| `EV-11` | `IN-04` | Mục Xuất trong blueprint `INV-LIST`; SaveFileDialog và file thật |
| `EV-12` | `SH-01`, `SY-01`, các action vượt quyền | Shell trong bốn role; blueprint `SYS-SETTINGS`; kết quả API |

`SHARED-STATES` và `LAPTOP-COMPACT` còn đối chiếu các mục keyboard/error/DPI của checklist, không tạo thêm module production. Module khác vẫn được ghi vào coverage tracking; bằng chứng shared component có thể tái sử dụng nếu chứng minh đúng hành vi, nhưng không thay kiểm riêng action nghiệp vụ bị tác động.

## 5. Quy tắc chuyển mockup thành code

1. Lấy phạm vi/quyền/nghiệp vụ từ BA và yêu cầu người dùng; lấy danh mục numeric từ file01. Đọc blueprint file02, đối chiếu ảnh qua bảng này, rồi map vào code thật.
2. Hotspot chỉ điều hướng minh họa. Ví dụ `LOGIN` dẫn tới `OWN-HOME` trong prototype **không có nghĩa mọi tài khoản đăng nhập vào role Owner**. Tương tự, lối vào ngoại lệ dùng ảnh chi tiết mẫu không đồng nghĩa đổi loại hồ sơ hoặc command.
3. Không suy ra endpoint, enum, dữ liệu thật hoặc migration từ con số trong ảnh. Không đưa role switcher, dữ liệu mẫu hoặc nút điều hướng prototype thành cơ chế nghiệp vụ.
4. Các alias chỉ có chữ như `INV-DETAIL`, `WH-PICK`, `WH-COUNT`, `SYS-SETTINGS`, `SYS-AUDIT` là yêu cầu bố cục hợp lệ dù chưa có PNG riêng; **không tạo đường dẫn ảnh không tồn tại**.
5. Nghiệm thu trên laptop/PC Windows theo file04; mockup có màu/spacing đúng chưa chứng minh quyền, DPI, keyboard, dữ liệu hoặc runtime đã đạt. Báo riêng phần chưa kiểm và gap có thật, không cắt scope theo số lượng14ảnh.
