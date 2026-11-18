# BotZone Admin redesign — tracking và bằng chứng

Cập nhật 18/11/2026. Nguồn: `BotZone_BA_v1.1_WinForms.md`, trọn bộ bàn giao tại `BotZone_Admin_Redesign_v1/`, code WinForms/API hiện hành và yêu cầu motion bổ sung. Bản đồ dưới bao phủ **55/55 ID**; 14 PNG thiết kế chỉ là mẫu tham chiếu. Các mục chưa có endpoint hoặc workflow ở baseline được ghi rõ, không coi thay theme là đã thực hiện nghiệp vụ thiếu.

**Bản Release vòng 4 đã kiểm trực tiếp và mở sẵn trên desktop của Khang.** Sidebar đã đổi sang sáng theo yêu cầu mới nhất của Khang. Lượt shell trước sửa cuối grid có **208 PASS, 38 PNG `CopyFromScreen`**. Trên đúng Release hiện tại, component đạt **91 PASS/16 PNG**, grid combo đạt **22 PASS/8 PNG**; shell cuối đạt **43 PASS/7 PNG**; executable thật đã đăng nhập và chụp lại trên desktop Default. Vòng 3 giữ **96 kiểm tra chức năng/layout PASS, 80 PNG `DrawToBitmap`**, nhưng tuyên bố nghiệm thu thị giác trước đây được rút lại: ảnh bitmap đã bỏ sót góc nút trắng/viền bẩn trên desktop thật. Bằng chứng các vòng trước, bảng 55 màn hình và các gap API được giữ nguyên; không dùng chúng thay cho kiểm hiển thị trực tiếp hoặc chứng nhận các workflow ghi nghiệp vụ chưa thực hiện.

## Hiện trạng và bảo toàn

- Workspace không có metadata Git (`git status`: not a git repository); không reset, commit, push hoặc xóa tài liệu cũ. `role.md` hiện rỗng.
- Solution `BotZone.slnx`; admin `src/BotZone.Admin.WinForms/BotZone.Admin.WinForms.csproj`, `net10.0-windows`, WinExe x64, framework dependent. Native WinForms, không toolkit ngoài; chỉ tham chiếu Contracts. DPI bootstrap `PerMonitorV2`, forms `AutoScaleMode.Dpi`.
- Admin → `ApiClient` (HttpClient/CookieContainer trong bộ nhớ, session + CSRF) → API ASP.NET Core → service/EF Core/PostgreSQL. Giữ storefront, schema, migrations, state machine, command key và công thức tại backend.
- Quyền lấy từ `Actor` đã xác thực, API kiểm lại từng request. Owner xem/preview landing; chỉ content ghi/publish/restore. Content không invoice/giá/tồn; warehouse không xuất invoice tài chính. Giữ policy runtime đối với supplier/purchase cho warehouse; không tự siết/mở quyền theo ảnh.

## Baseline native trước sửa

Lệnh đã chạy thành công:

```powershell
dotnet src/BotZone.Admin.WinForms/bin/Debug/net10.0-windows/BotZone.Admin.WinForms.dll --smoke --api http://127.0.0.1:5080 --output artifacts/winforms/admin-redesign-before
```

Kết quả: **exit 0, PASS**, 15:20–15:20 ngày 17/11/2026 giờ Việt Nam. Windows NT 10.0.26300.0 x64; DPI thực **120 (125%)**. Kích thước cửa sổ yêu cầu và thực tế 1366×768, 1920×1080. Đây là kích thước window, không bằng chứng thay độ phân giải vật lý monitor.

[smoke.json](artifacts/winforms/admin-redesign-before/smoke.json) chứa login/session/CSRF, typed list 8 hóa đơn tổng 97.130.000 VND, chi tiết 3 dòng, 21 module, 6 loại báo cáo vận hành, XLSX/PDF chung export snapshot và logout. Có **26 PNG native** cùng [XLSX](artifacts/winforms/admin-redesign-before/invoices.xlsx) và [PDF](artifacts/winforms/admin-redesign-before/invoices.pdf). Export snapshot `875daf89-6f14-4db6-8322-041a6dfb1d40`: 8 hóa đơn / 97.130.000 VND. Test không sửa nghiệp vụ; có tạo/thu hồi phiên và export qua API. Không chạy `--roles-only` cũ vì mode đó có lưu landing.

Ảnh baseline là `DrawToBitmap` từ Form/control thực trong Windows message loop; không phải HTML/mockup. Chưa kiểm manual Narrator, keyboard toàn luồng hoặc DPI 100%/150% ở baseline. Nội dung ảnh được lưu local.

## Inventory màn hình

Vai trò: **O** chủ cửa hàng, **Đ** xử lý đơn, **K** kho, **N** nội dung. Trạng thái cột cuối ở thời điểm lập inventory: **B** có UI/API baseline; **G** có khoảng thiếu baseline; **M** cần mẫu mới trên API đang có. B không có nghĩa redesign/nghiệm thu đã xong. Shared component áp dụng: shell/header/toolbar/grid/status/button/busy/focus/motion; các form chi tiết/input dùng shared form/tabs/validation.

| ID | Role | Form / module / action thực tế | Pattern đối chiếu | Baseline, việc cần giữ và gap |
|---|---|---|---|---|
| LG-01 | O Đ K N | `LoginForm` → `ApiClient.LoginAsync` | LOGIN | B: email/mật khẩu/API URL, login thật; thêm feedback/busy/keyboard/hiện mật khẩu |
| LG-02 | O Đ K N | `ApiClient.SessionAsync`, `SessionExpired`, Main recovery | LOGIN, SHARED-STATES | G ở baseline; vòng 3 có re-auth trong shell giữ page/draft khi cùng actor và role. Dialog con quay về shell để đăng nhập lại; không tự gửi lại lệnh |
| SH-01 | O Đ K N | `MainForm`, 23 module menu và logout | Shell, LAPTOP-COMPACT | B: server actor; baseline dispose page khi đổi module nên mất ngữ cảnh; cần cache/dirty/lifecycle |
| OW-01 | O | `OverviewView`, overview + reports API | OWN-HOME | M: overview chỉ orders/lowStock/drafts; dùng báo cáo thật cho KPI, không số minh họa |
| OW-02 | O | returns/refunds/trade_ins/stock_adjustments + `NativeCommandForm` | OWN-HOME/detail | G: các action duyệt có thật, chưa có query hàng chờ thống nhất đầy đủ |
| OR-01 | O Đ K | `OrdersView` → `GET /api/orders` | ORD-QUEUE | B/G: 200 đơn gần nhất; chưa server search/paging/settlement/customer list |
| OR-02 | O Đ K | `OrderDetailForm` → `GET /api/orders/{id}` | ORD-DETAIL | B: dòng/units/attempts/receipts/shipments/address; giữ scope kho và quay lại list |
| OR-03 | O Đ | orders + payments; `start_payment`, `record_payment` | ORD-DETAIL/payment | B: COD và giao nhận độc lập, confirmation tiền, pending key không tự retry |
| OR-04 | O Đ K | OrderDetail; `shipment_event`, `handoff` | ORD-DETAIL/timeline | B/G: chuyển giao demo có thật; không có UI riêng finalize_failed_delivery ở baseline |
| IN-01 | O Đ | `InvoicesView`, `InvoiceQuery`, BindingSource | INV-LIST | B: list/search/sort/page/count/tổng toàn tập, issued/draft |
| IN-02 | O Đ | `InvoiceDetailForm` | INV-DETAIL chữ | B: snapshot/dòng/nguồn thu/chứng từ giảm/timeline; chỉ đọc |
| IN-03 | O Đ | `FilterDialog` | INV-FILTER | B: whitelist typed rule/group/not/null/product; giữ giới hạn depth/condition |
| IN-04 | O Đ | invoice/report export → SaveFileDialog | INV-LIST/export | B: detail/list/report XLSX/PDF cùng dataset; không tổng trang thay toàn tập |
| WH-01 | K O | overview + orders/purchases/returns/warranty | WH-WORK | M/G: có các danh sách thật, chưa endpoint phiếu được giao tổng hợp |
| WH-02 | K | OrderDetail + `handoff` | WH-PICK chữ | B/G: xuất transaction có thật; baseline không có command gán serial thủ công trước handoff |
| WH-03 | K O | `ModuleView(inventory)`, operations inventory | WH-WORK/list | B/G: tồn lô/giữ/quarantine thật; thiếu sổ stock movement detail API |
| WH-04 | K O | lookups.inventoryUnits; order detail.units | Serial/list/detail | G: lookup serial có thật; chưa endpoint lịch sử riêng từng serial |
| WH-05 | K O | inventory/stock_adjustments + command form | WH-COUNT chữ | G: chỉ quantityDelta/lý do; chưa workflow draft thực đếm/đếm chưa xong |
| WH-06 | K đề nghị, O duyệt | `request_stock_adjustment`, `approve_stock_adjustment` | WH-COUNT/OWN-HOME | B: role, lý do, chọn thiết bị đúng lô; server kiểm trùng/âm |
| PU-01 | O K | suppliers; `save_supplier` | List/form | B: tạo/sửa theo policy runtime, archive bằng active |
| PU-02 | O K | purchases; `create_purchase`, `approve_purchase` | List/detail/WH-RECEIPT | B: PO lines qua lookup; action đúng quyền |
| PU-03 | K | `receive_goods`, `ReceiptLines` | WH-RECEIPT | B/G: nhận một phần/serial có thật; lookup chỉ ordered, thiếu đã nhận/còn thiếu để hiển thị |
| CA-01 | O, còn lại xem | categories/catalogue; `save_category`, `create_product` | Catalogue/list/form | B/G: tạo nhóm/model; chưa editor đầy đủ attribute-definition/specs |
| CA-02 | O sửa, Đ K N xem | catalogue; `create_variant`, `update_variant`, `update_product_status` | SKU/list/form | B: giữ giá/care/warranty/tracking và trạng thái kinh doanh |
| CA-03 | O sửa | `set_compatibility`, product MultiLookup | Picker/list | B: target ID quan hệ hiện có, không tự tạo SKU |
| CU-01 | O Đ | `ModuleView(customers)` | List | B: search/paging, số dư/điểm/hạng do API |
| CU-02 | O Đ | `CustomerDetailForm`, `CustomerReadService` | Detail/tabs | B: profile, orders, addresses, credit, loyalty, returns, trade-ins, claims; limit lịch sử 200 |
| PR-01 | O sửa, Đ N xem | promotions; `create_promotion` | List/form | B: tạo mã/version; bảo toàn rule cũ |
| PR-02 | O Đ N theo quyền | promotions/operations promotions | Detail/list | G: list baseline không trả versions/targets/redemptions đầy đủ |
| PR-03 | O cấu hình, Đ tra cứu | settings commerce + customer loyalty tabs | Settings/ledger | B/G: points rules và ledger thật; chưa màn cấu hình tier riêng |
| AS-01 | O Đ K | returns/refunds/trade_ins/warranty | Case queue | B: từng module phân quyền, không dùng order state cho case |
| AS-02 | Đ tiếp nhận, K kiểm, O duyệt | `approve_return_intake`, `inspect_return`, `approve_return`, `resolve_return` | Case detail/checklist | B/G: action thật; generic list/detail thiếu return item/evidence projection đầy đủ |
| AS-03 | O, Đ theo dõi | refunds; `execute_refund`; order cancelled refund | Confirmation/finance | B: key hiện có, hoàn đúng nguồn do service, credit note khác refund |
| TI-01 | O Đ K | `ModuleView(trade_ins)` | Case queue | B: request search/paging |
| TI-02 | K | `receive_trade_in`, `inspect_trade_in` | Checklist/detail | B/G: item lookup thật; baseline gửi evidenceIds rỗng, chưa picker bằng chứng |
| TI-03 | O Đ | `quote_trade_in`, `approve_trade_in_quote` | Quote/version/detail | B: quote lines/version/expiry, approval theo quyền |
| TI-04 | O Đ K theo bước | `finalize_trade_in`, customer credit ledger | Ledger/timeline | B/G: cấp credit thật; chưa UI staff gửi trả thu cũ riêng |
| WC-01 | O Đ K theo quyền | warranty list; customer claims; SKU Care fee | Care list/detail | G: chưa màn danh sách mọi entitlement/plan version native riêng |
| WC-02 | Đ K | `process_warranty`: receive/inspect/repair/repaired/close | Case detail | B: giữ notes/state/role; data fixture chưa có claims ở baseline |
| WC-03 | O K | `process_warranty`: unrepairable/approve_replace/replace/refund/reject | Action/confirmation | B: cùng SKU, lot hoặc serial, không doanh thu mới; mutation chưa chạy đợt này |
| CT-01 | N | `ContentHomeView`, admin landing draft + public landing; mở catalogue | CNT-HOME | M/G ở baseline; đã có backlog landing thật. Thiếu admin translation projection nên không giả count thiếu VI/EN sản phẩm |
| CT-02 | N sửa, O Đ K xem | catalogue → `update_translation` | CNT-PRODUCT | B/G: save/publish translation cùng command; chưa preload mô tả/SEO/draft EN từ admin API |
| CT-03 | N sửa, O xem | `LandingView`, TreeView/native tabs | CNT-EDITOR | B: schema hiện có, VI/EN/media/preset/range, thứ tự section; giữ cấu hình mở rộng |
| CT-04 | N ghi, O preview/xem | `save_landing`, `preview_landing`, `publish_landing`, `restore_landing` | CNT-EDITOR/history | B: revision/idempotency/grant, publish riêng save; vòng 2 giữ dirty/busy và lỗi mở đúng nhóm |
| CT-05 | N upload, O xem | media + `MediaMetadataForm`, upload API | Media/list/editor | B/G: upload validate/source/license; chưa API trả references/gallery quản lý đầy đủ |
| RP-01 | O Đ scope | `ReportsView` period | REP-INVOICE | B: tháng/quý/năm/multi-year/kỳ rỗng, định nghĩa metric và drill-down |
| RP-02 | O Đ scope | `ReportsView` staff | REP-INVOICE | B/G: responsible_staff scope; lookup baseline chỉ staff active dù lịch sử report vẫn server |
| RP-03 | O Đ scope | `ReportsView` product/model/SKU | REP-INVOICE | B: tổng dòng riêng với filter invoice contains; drill-down/export |
| RP-04 | O Đ scope | ReportsView metric receipts/refunds | Report/table | B: ngày receipt/refund, không dùng tổng hóa đơn thay thu/hoàn |
| RP-05 | O, K inventory theo quyền | `OperationalReportsView` inventory/margin | Operations report | B: definition/readAt, service tính giá vốn/lãi gộp |
| RP-06 | O, role khác theo policy | operations top_products/promotions/trade_in/warranty | Operations report | B: 6 loại báo cáo đã load; từng definition/phạm vi giữ tại API |
| SY-01 | O | staff → `update_staff` | SYS-SETTINGS chữ | B: khóa/role/reason, server revoke session; không tạo role switcher |
| SY-02 | O | settings → `update_settings` | SYS-SETTINGS chữ | B/G: UI tỷ giá/commerce; store/tradeIn tồn tại nhưng chưa editor riêng |
| SY-03 | O Đ K N scope | audit ModuleView | SYS-AUDIT chữ | B/G: list actor/action/time/reason; chưa before/after detail trong projection |
| SY-04 | Đ O demo payment; công cụ CLI riêng | payment/fulfillment demo + scripts local | Action/confirmation | B/G: không có reset/restore DB trong UI hiện tại; không thêm nút phá dữ liệu |

## Rà mật độ vòng 2 — lớp mặc định và chi tiết theo yêu cầu

55/55 ID đã được đối chiếu lớp mặc định và thông tin mở khi cần. Ký hiệu bằng chứng: **N** = full native published 35 PASS; **R** = bốn vai trò native published 56 PASS; **F** = finance native 48 PASS, 1 chưa xác minh; **S** = source đã rà theo ownership, chưa chạy giao dịch ghi. N/R/F chỉ chứng minh kiểm tra được nêu, không thay cho mutation, fixture hoặc endpoint còn thiếu. Các đường dẫn JSON và ảnh nằm trong phần bằng chứng vòng 2.

| ID | Lớp 1: cần nhìn ngay | Lớp 2: mở khi cần | Owner / nghiệm thu vòng 2 |
|---|---|---|---|
| `LG-01` | Email, mật khẩu, đăng nhập, lỗi/busy | Cấu hình địa chỉ dịch vụ | N/R · đăng nhập, role/session và busy đã kiểm |
| `LG-02` | Banner hết phiên, đăng nhập lại cùng tài khoản, giữ draft | Dialog con quay về shell; thay role phải làm mới cache | V3 shared · live API giữ draft cùng actor/role PASS; đổi role xóa cache đã rà source; không tự retry |
| `SH-01` | Nhóm đang dùng, module hiện tại, tài khoản, trạng thái | Các module cùng nhóm qua mở nhóm/menu | R · menu compact đúng role, nhóm wide mở/thu, cache/latest PASS |
| `OW-01` | KPI chính và hàng chờ trong kỳ hiện tại | KPI còn lại, đổi kỳ, định nghĩa và drill-down | R · owner home/compact đã kiểm; S · KPI/kỳ/chi tiết đã rà |
| `OW-02` | Loại hồ sơ, trạng thái, việc cần duyệt | Hồ sơ/chứng từ và xác nhận theo từng loại | N/S · list và form hiện có đã rà; chưa có queue duyệt thống nhất |
| `OR-01` | Hàng chờ, tìm mã, trạng thái, scope 200 đơn | Hồ sơ đơn và các bộ lọc ít dùng | N/R · list, scope 200 và giữ filter/selection PASS |
| `OR-02` | Mã đơn, trạng thái, dòng hàng, bước tiếp theo | Thu tiền, giao nhận, địa chỉ, lịch sử từng nhóm | R · 3 nhóm, 5 cột; kho 2 nhóm/3 cột; Back giữ context |
| `OR-03` | Số tiền/trạng thái đối soát khi xử lý thanh toán | Các lần thử và chứng từ thu/hoàn | R/S · mở/đóng xác nhận không submit; giao dịch tiền chưa chạy |
| `OR-04` | Trạng thái giao và bước hợp lệ | Timeline, thông tin giao thất bại/gửi lại | R/S · nhóm giao nhận và dữ liệu phụ; giữ gap finalize_failed_delivery |
| `IN-01` | List chính, tìm, tổng/scope, lọc đang áp dụng | Advanced filter, cột, lưu view, export | N/F · 5 cột, query/tổng/filter/selection/compact PASS |
| `IN-02` | Mã hóa đơn, trạng thái, tổng và dòng hàng | Thu/hoàn, chứng từ giảm, snapshot, timeline theo đích riêng | N/F · 3 nhóm, 4 cột; phân bổ/chứng từ/snapshot truy cập được |
| `IN-03` | Điều kiện đang soạn và lỗi tại điều kiện | Nhóm AND/OR/NOT, null/product, tùy chọn thêm | F · AST và tổng query giữ nguyên; lỗi/nhóm đã rà |
| `IN-04` | Phạm vi xuất, định dạng, tiến độ/kết quả | Chi tiết snapshot, tải từng file/lưu native | N/F · snapshot/XLSX/PDF API PASS; SaveFileDialog chưa xác minh |
| `WH-01` | Hàng chờ kho và việc ưu tiên | Chuyển tồn, nhập, trả/Care theo công việc | R · home kho/compact PASS; queue giao việc tổng hợp vẫn là gap |
| `WH-02` | Phiếu, hàng/serial cần kiểm, bàn giao | Địa chỉ/phân bổ và điều kiện ngoại lệ | R/S · detail kho đã kiểm; chưa thêm gán serial thủ công |
| `WH-03` | Sản phẩm, khả dụng/giữ/quarantine, scope | Lô và chi tiết nguồn tồn theo dữ liệu có | N/F · inventory list/report PASS; gap sổ tồn giữ nguyên |
| `WH-04` | Serial/IMEI, tình trạng và vị trí hiện có | Lịch sử riêng chỉ khi API có | S · lookup hiện có giữ nguyên; gap endpoint lịch sử serial |
| `WH-05` | Phiếu/lô, số lượng đang đối chiếu | Chi tiết dòng và lý do chênh lệch | N/S · list/form điều chỉnh đã rà; chưa có draft kiểm kê |
| `WH-06` | Đề nghị, lượng chênh, trạng thái duyệt | Lý do/chứng cứ và xác nhận quyền owner | N/S · form/quyền/payload giữ nguyên; chưa submit điều chỉnh |
| `PU-01` | Nhà cung cấp, liên hệ chính, hoạt động | Form sửa/tạo và dữ liệu phụ | N/S · list/preset/record/form đã rà; chưa ghi supplier |
| `PU-02` | Mã PO, nhà cung cấp, trạng thái | Dòng mua, duyệt, mở nhận hàng | N/S · list/PO/form đã rà; chưa ghi hoặc duyệt PO |
| `PU-03` | SKU, số nhận, scanner, lỗi trùng/thiếu | Chi tiết lô/serial/giá vốn theo quyền | R · scanner/duplicate/focus PASS; API thiếu đã nhận/còn thiếu |
| `CA-01` | Nhóm/model và trạng thái | Form thuộc tính/model riêng khi có | N/R/S · model 2 nhóm, validation PASS; gap attribute editor |
| `CA-02` | SKU, giá/trạng thái theo quyền | Sửa đúng variant, warranty/Care tùy chọn | R · đúng variant_id, picker cột/refresh/selection PASS; chưa ghi |
| `CA-03` | Model cần cấu hình | Picker phụ kiện/tương thích riêng | S · picker/target/contract giữ nguyên; chưa ghi compatibility |
| `CU-01` | Tên/liên hệ, search/scope | Hồ sơ khách hàng | N/F · list và mở profile PASS; scope API giữ nguyên |
| `CU-02` | Nhận diện khách và tổng quan cần thiết | Đơn, địa chỉ, điểm, credit, hậu mãi từng nhóm | F · 3 nhóm, đủ count/credit/loyalty/returns/trade-in/claims |
| `PR-01` | Mã ưu đãi, hiệu lực, trạng thái | Tạo/sửa phiên bản riêng | N/S · list và form 3 nhóm đã rà; chưa tạo promotion |
| `PR-02` | Tình trạng và điều kiện chính | Targets/lượt dùng/version khi API có | N/S · record/preset đã rà; gap versions/targets/redemptions |
| `PR-03` | Chính sách đang áp dụng | Cấu hình và ledger điểm/hạng riêng | F/S · ledger đọc PASS, commerce 2 nhóm; gap tier editor |
| `AS-01` | Hàng chờ theo loại, trạng thái và bước xử lý | Mở từng case | N/S · list theo từng loại đã load; source quyền/state giữ nguyên |
| `AS-02` | Món nhận, tình trạng và việc kiểm định | Bằng chứng, lý do, quyết định theo bước | N/S · form theo bước đã rà; gap item/evidence projection |
| `AS-03` | Số tiền/nguồn hoàn, trạng thái, xác nhận | Chứng từ giảm, lịch sử hoàn | R/F/S · xác nhận mở không submit; credit note/refund giữ riêng |
| `TI-01` | Yêu cầu thu cũ và bước đang chờ | Hồ sơ từng thiết bị | N/S · list/record đã load; giữ search/paging |
| `TI-02` | Thiết bị, checklist cần nhập, lỗi | Bằng chứng và thông tin tiếp nhận phụ | S · checklist/form giữ contract; gap evidence picker |
| `TI-03` | Báo giá hiện hành, hạn và phản hồi | Chi tiết dòng/version trước | N/S · form quote/version/expiry đã rà; chưa submit báo giá |
| `TI-04` | Credit cấp/giữ theo điều kiện và trạng thái | Ledger và gửi trả khi có workflow | F/S · credit ledger đọc PASS; gap workflow gửi trả |
| `WC-01` | Thiết bị/quyền lợi và trạng thái case hiện có | Hợp đồng/plan version khi API có | N/S · list hiện có load; gap entitlement/plan list riêng |
| `WC-02` | Bước xử lý và dữ liệu cần nhập ở bước đó | Lịch sử tiếp nhận/sửa/trả | N/S · empty state PASS, form theo bước đã rà; fixture claims rỗng |
| `WC-03` | Quyết định xử lý, serial/tiền liên quan | Lý do, bằng chứng và xác nhận ngoại lệ | S · form/role/điều kiện giữ nguyên; chưa mutation thay/hoàn |
| `CT-01` | 3 số thật, queue 3 cột, scope nháp/công khai | Detail từng khối; metadata xuất bản/tài nguyên qua menu | R · queue 3 cột, detail, đúng section và cache PASS |
| `CT-02` | Ngôn ngữ/nội dung đang sửa, lưu nháp | SEO/metadata và xuất bản riêng | N/S · catalogue/form nháp-publish đã rà; gap preload translation |
| `CT-03` | Nội dung VI mặc định; 3 tab cấp 1 | EN trong Nội dung; ảnh/liên kết, motion, advanced chọn từng nhóm | R · 3 tab, mặc định VI, nhóm phụ/compact 215 px PASS |
| `CT-04` | Lưu nháp, xuất bản riêng; dirty/busy/error | Preview menu; lịch sử/restore trong tab riêng | R · lỗi EN/advanced, dirty/cache/quyền PASS; API draft r3 không đổi |
| `CT-05` | Media và trạng thái cần dùng | Metadata/license/upload; references khi có API | N/S · empty/list/upload metadata đã rà; gap media references |
| `RP-01` | Metric/kỳ/scope hiện hành và kết quả | Đổi kỳ, định nghĩa, export/drill-down | N/F · kỳ, metric, 3 tab, cột chính/extra filters PASS |
| `RP-02` | Kết quả theo nhân viên trong scope thật | Bộ lọc và chi tiết nhân viên | F · tab staff/scope/tổng PASS; gap lookup staff inactive giữ nguyên |
| `RP-03` | Kết quả theo sản phẩm/SKU | Model/variant, bộ lọc và invoice drill-down | F · tab product/extra filters/export PASS; tổng dòng giữ riêng |
| `RP-04` | Thu/hoàn và kỳ của chứng từ tương ứng | Chi tiết receipt/refund và định nghĩa | F · cột thu/hoàn và nguồn metric PASS; không đổi loại tổng |
| `RP-05` | Loại báo cáo, scope và số chính | Chi tiết tồn/giá vốn/lãi theo quyền | N/F · inventory/margin load, 5 cột chính và scope PASS |
| `RP-06` | Kết quả của một báo cáo đang chọn | Đổi loại, giải thích và chi tiết nhóm | N/F · đủ 6 loại operations; warranty empty state PASS |
| `SY-01` | Nhân viên, vai trò/trạng thái | Form thay quyền/khóa + lý do | N/S · list/form/role/reason đã rà; chưa ghi đổi quyền |
| `SY-02` | Nhóm cấu hình đang chọn | Form từng thiết lập và xác nhận | N/R/S · list/403 theo role PASS, form nhóm; editor thiếu giữ gap |
| `SY-03` | Ai, hành động, thời gian và scope | Lý do/chi tiết bản ghi khi có | N/S · list/preset/record đã rà; gap before/after |
| `SY-04` | Tác vụ demo hợp lệ đang chọn | Tham số/xác nhận riêng | S · action/xác nhận giữ contract; chưa chạy demo/reset DB |

Nguyên tắc kiểm lại: thông tin ở lớp 2 phải truy cập được bằng native keyboard/menu; dữ liệu cần xác nhận không bị giấu trong thao tác ghi. Lỗi ở field đang ẩn phải mở đúng nhóm và focus field. Nội dung/giá/tổng không bị sửa để giảm chữ. Search skill về progressive disclosure không trả kết quả đủ khớp desktop sau hai truy vấn; các quyết định phân lớp ở vòng này dựa yêu cầu cụ thể của người dùng và contract hiện có, không áp máy móc kết quả web.

## Bảo toàn action qua các vòng

Mọi action qua `ApiClient.CommandAsync` → `CommandService.Execute` → permission/transaction/idempotency/audit hiện có. Redesign không chạy các action này chỉ để tạo ảnh.

| Nhóm | Commands / read contract |
|---|---|
| Xác thực | login/session/logout + anti-forgery; không lưu cookie/mật khẩu |
| Catalogue | save_category, create_product, create_variant, set_compatibility, update_product_status, update_variant, update_translation |
| Kho / mua | save_supplier, create_purchase, approve_purchase, receive_goods, request_stock_adjustment, approve_stock_adjustment, handoff |
| Đơn / giao / thu | confirm_order, cancel_order, shipment_event, start_payment, record_payment, execute_refund |
| Trả hàng | approve_return_intake, inspect_return, approve_return, resolve_return |
| Thu cũ | receive_trade_in, inspect_trade_in, quote_trade_in, approve_trade_in_quote, finalize_trade_in |
| Care | process_warranty (receive, inspect, repair, repaired, unrepairable, replace, approve_replace, refund, reject, close) |
| Ưu đãi / hệ thống | create_promotion, update_staff, update_settings; audit đọc theo role |
| Nội dung | save_landing, preview_landing, publish_landing, restore_landing; media upload + source/license |
| Hóa đơn / báo cáo | invoices/search, invoices/{id}, reports/invoices, invoices/exports, export download; operations inventory/margin/top_products/promotions/trade_in/warranty |

## Thực hiện trên code hiện có

- `MainForm` giữ từng page theo module, xác thực lại session khi chuyển, hủy lượt điều hướng cũ và giữ context; overview theo vai trò. `Ui`, `UiComponents` và `Motion` cung cấp native header, field, grid, tabs, focus và busy dùng chung. Không có WebView hoặc HTML thay WinForms.
- `ContentHomeView` có ba thẻ số từ **draft landing thật**: tổng khối, thiếu title/subtitle VI/EN/poster/CTA và section khác bản công khai. Vòng 2 giữ queue ba cột; mở detail khối mới đọc từng ngôn ngữ, metadata/tài nguyên qua menu. Biên tập từ detail mở đúng section, quay lại giữ dòng chọn. Không gán số thiếu bản dịch sản phẩm khi API admin chưa có dữ liệu đó.
- `LandingView` vòng 2 chỉ có ba tab cấp 1 **Nội dung / Hiển thị / Lịch sử**. Nội dung mặc định VI, EN qua selector; Hiển thị chỉ hiện một nhóm ảnh-liên kết, chuyển động hoặc nâng cao. Schema/cấu hình mở rộng, thứ tự/thêm/xóa vẫn giữ. Validation tự mở đúng tab/locale/nhóm và focus field, không giấu lỗi sau khi chuyển lớp. Ctrl+S chỉ lưu nháp; save, preview, publish/restore riêng, giữ revision/key và xác nhận phạm vi. Owner đọc/preview, content biên tập. Dirty-close giữ/bỏ/lưu rồi tiếp tục; bỏ thay đổi giữ section đang chọn. Write đang chờ không bị hiểu là hủy khi rời page, không tự retry.
- Operations giữ **34/34 ActionSpec** so với source backup; thêm command selector/context, lỗi tại trường/dòng, scanner serial local, bảng đơn 200 gần nhất ghi rõ phạm vi. Order detail nằm trong workspace và quay lại giữ tìm kiếm; SKU action dùng `variant_id` đúng dòng chọn. API không có received-before/remaining của PO thì không tạo số hiển thị giả.
- Hóa đơn/báo cáo giữ query typed, tổng toàn tập, sort/page/filter, source snapshot/export và scope staff; các kiểm tra tài chính bổ sung do ownership invoice/report chạy riêng.

## Motion và lifecycle — yêu cầu nghiệm thu bổ sung

Tokens chung: feedback 80–120 ms; module/tab/panel 120–180 ms, ease-out. Loading chỉ chạy khi có tác vụ thật. Nhấn phản hồi ngay, request không chờ transition; không ép thời gian loading tối thiểu. Dừng/thay transition cũ khi tương tác nhanh. Không dùng Form.Opacity cho cả app, Thread.Sleep/.Wait/.Result/Application.DoEvents để tạo animation.

| Hạng mục | Bằng chứng cần thu | Trạng thái |
|---|---|---|
| Nút/nav/tab/checkbox focus, hover, pressed, selected, disabled | Native keyboard và chuột; layout không dịch | Component đúng Release cuối 91 PASS/16 ảnh, grid combo 22 PASS/8 ảnh desktop thật; shell cuối đạt **43 PASS/7 PNG**; executable thật đã đăng nhập và chụp lại trên desktop Default. PASS chức năng/bounds và PNG `DrawToBitmap` không thay cho kiểm góc/repaint |
| Busy login/load/save/filter/page/export/publish | Nhãn đúng task + spinner vùng tương ứng; không khóa shell | Shared busy PASS suppression, nested scope và giữ focus; lệnh save/publish thật chưa mutation test |
| Đổi module/filter liên tục | Request cũ không ghi đè; không timer treo/control disposed | PASS controlled old/new result và rapid orders→overview / invoice query, cache module; xem role smoke |
| Đóng form/rời khi đọc/ghi | Read cancel/drop late; ghi không hiểu là server hủy, key giữ đúng | Read deactivate/dispose PASS; dirty modal giữ input/bỏ thay đổi/cache PASS. Nhánh lưu qua server và mất mạng trong lúc ghi chưa mutation test |
| Reduced motion Windows | Bỏ transition nhưng giữ focus/chữ/progress, không sửa setting Windows | PASS override test, check SPI_GETCLIENTAREAANIMATION không đổi; khôi phục override trong finally. Chưa manual OS toggle |
| Bản chạy hoặc video | Executable native thật xem được chuyển động | Vòng 4 có 38 PNG desktop của hash `40810E…`; bản cuối `5B9BF0…` đã qua component/grid/shell; có ảnh cửa sổ executable đang chạy trên Default |

## Vòng 4 — sidebar sáng, control và bảng; đã kiểm Release cuối

Phản hồi từ cửa sổ đang dùng là bằng chứng lỗi thật: nút dùng chung có góc trắng hoặc viền bẩn dù ảnh harness trước đó trông sạch; nhóm sidebar và module con giống cùng một cấp. Ảnh desktop trước sửa được giữ tại [user-window-before.png](artifacts/winforms/live-ui-v4/user-window-before.png). Không coi trạng thái GUI visible, build thành công hay số PNG là nghiệm thu thẩm mỹ.

`BzButton` đã tắt `Opaque`, bật `SupportsTransparentBackColor` và dùng nền trong suốt để vùng ngoài đường bo được vẽ lại đúng theo parent; màu primary chỉ nằm trong đường bo. Nút giữ focus/busy và kích thước khi đổi trạng thái. Lượt kiểm cuối đã xác nhận normal/hover/pressed/keyboard focus/disabled/busy/selected, resize và che/hiện lại trên desktop thật của đúng binary.

Sidebar sáng theo yêu cầu mới nhất: `NavSurface #FFFFFF`, chữ chính tối, nhãn nhóm `NavMuted #706C73`, nhấn burgundy. `NavigationLevel` và `Expanded` tách nhóm Semibold/chevron với module con thụt thêm 26 logical px, bỏ icon lặp; chỉ module đang chọn có nền/vạch nhấn ở chế độ rộng. Thu gọn giữ icon nhóm/menu và dấu chọn mục hiện tại. Ctrl+K, lọc quyền, cache page và dữ liệu giữ nguyên.

Dropdown thường và combo trong bảng dùng chung `ComboPopupChrome`: popup native giữ chọn/cuộn/Enter/Escape, có góc bo và viền `Ui.Line`; `BzGridComboBoxColumn` giữ `DisplayMember`/`ValueMember` và raw ID. Lượt grid v3 xử lý focus vẽ hai lần, dòng mới, shared row/getter, padding mặc định và thứ tự đo độ rộng popup sau native handler. Date picker giữ segment ngày native để nhập/sửa bằng bàn phím khi focus, đồng thời vẽ nút lịch cùng style. `Ui.Bar` căn nút theo đáy hàng input; label thường/nav/combo hiển thị nguyên dấu `&`, còn caption của `Ui.Field` giữ mnemonic. Tổng quan compact được thu gọn chiều cao phần trên để thấy đủ ba dòng dữ liệu.

`ModuleView` dùng đúng field API và tối đa sáu cột mặc định: trả hàng có tham chiếu/đơn/state/lý do/ngày, hoàn tiền dùng `completed_at`, thu cũ có cách tiếp nhận, tồn kho có mã lô, audit có người và hồ sơ liên quan. UUID chỉ rút gọn khi hiển thị, đầy đủ trong tooltip/detail; selection và dữ liệu lệnh vẫn dùng giá trị gốc. Enum đã biết được Việt hóa, free text giữ nguyên. Overlay “Chưa có hồ sơ” hoặc “Không tìm thấy kết quả” chỉ hiện sau lượt đọc thành công rỗng; ẩn khi tải/lỗi/có dữ liệu, lỗi vẫn nằm ở Status. Không tạo hồ sơ giả để lấp màn hình.

| Bằng chứng vòng 4 | Kết quả đã có | Phạm vi / binary |
|---|---|---|
| [Full shell sáng](artifacts/winforms/live-ui-v4/full-shell-light-review/result.json) | **208 PASS, 38 PNG `CopyFromScreen`** | 23 module owner; desktop Default, DPI thực 120; HWND/foreground/occlusion có guard. DLL `40810E21F4FAA42ABA640B809DB080A9A54FD7C126C3D4556B6697AB6D196BBB`, trước sửa cuối grid |
| [Cột/ID/trạng thái rỗng](artifacts/winforms/live-ui-v4/operation-columns/result.json) | **70 PASS** | Fixture từ GET thật của 17 module; raw ID/selection/detail, enum, empty→loading→error→data. Không network trong harness, không ảnh nghiệm thu |
| [Component Release cuối](artifacts/winforms/live-ui-v4/after-final-light/result.json) | **91 PASS, 16 PNG `CopyFromScreen`** | Đúng DLL `5B9BF00E…`; trạng thái nút/focus/busy và repaint |
| [Grid combo Release cuối v3](artifacts/winforms/live-gridcombo-v4/final-release-v3/result.json) | **22 PASS, 8 PNG `CopyFromScreen`** | Đúng DLL `5B9BF00E…`; popup/paint/binding/keyboard |
| [Shell ưu tiên / dropdown / date của Release cuối](artifacts/winforms/live-ui-v4/full-shell-final/result.json) | **43 PASS, 7 PNG CopyFromScreen** | DLL `5B9BF00E69BAC03CF1B1F87DF67DD0893BDBF3FCAC6645592DD5DE3AEE841AB9`; [manifest vòng 4](artifacts/admin/build-manifest.json) ghi 42 source/assets |

Kết quả cuối phải khớp hash Release đang chạy; không lấy 208 PASS/38 ảnh của bản trước sửa grid làm PASS cho bản mới. PNG `DrawToBitmap` chỉ hỗ trợ kiểm bố cục. Các lượt bị từ chối foreground/desktop được giữ là chưa xác minh; một lượt crash thuộc bootstrap của helper kiểm thử riêng, không phải crash khởi động của ứng dụng BotZone, và không được tính PASS. Khang đã cho phép kiểm trực tiếp desktop; các harness dùng cửa sổ riêng, không tự xác nhận lưu/bỏ dữ liệu. Các lượt kiểm chỉ đọc API/nhập local; có auth login/logout, không lưu giao dịch nghiệp vụ. DPI vật lý khác 120, Narrator toàn luồng và các trường hợp mutation chưa chạy vẫn chưa nghiệm thu.

Executable cuối đã được mở trên WinSta0\Default, đăng nhập owner qua màn Login bình thường, maximized và xác minh PID 42128 / HWND 4526362 / DPI 120. [Ảnh cửa sổ đang chạy](artifacts/winforms/live-ui-v4/published-app-live.png), [xác nhận đăng nhập](artifacts/winforms/live-ui-v4/published-login.json), [runtime](artifacts/admin/running-admin.json). Bản cũ trên desktop người dùng đã thoát trước lượt mở này. 42 source/assets khớp manifest; API và storefront vẫn chạy. Đây là kết quả kiểm kỹ thuật/ảnh thật cho phạm vi đã ghi, Khang nghiệm thu trải nghiệm khi sử dụng.

## Bằng chứng vòng 3 — tinh chỉnh thao tác và phục hồi phiên

Tại thời điểm vòng 3, source đã publish Release **0 warning / 0 error**. Hai lượt kiểm chức năng/layout từ executable tại `admin-refinement-final` và `admin-refinement-roles-final` hoàn tất **exit 0**, tổng **96 PASS / 80 PNG `DrawToBitmap`**, DPI thực **120 (125%)**. Đường dẫn publish dùng chung: [BotZone.Admin.WinForms.exe](artifacts/admin/win-x64/BotZone.Admin.WinForms.exe), Windows x64, framework dependent, cần giữ toàn bộ thư mục publish. [Manifest vòng 3 lưu riêng](artifacts/admin/build-manifest-round3.json) ghi 41 source/assets; kiểm lúc đó xác nhận 0 hash mismatch và DLL khớp. [GUI vòng 3](artifacts/admin/running-admin-round3.json) khi đó xác nhận Login visible, PID **47588**, HWND **7212980**; việc visible không xác nhận góc nút hoặc repaint đúng. API `5080/health` và web `3000/vi` trả HTTP 200 lúc bàn giao vòng 3. Đường dẫn publish hiện chứa vòng 4; các hash/PASS vòng 3 chỉ là lịch sử.

| Bộ kiểm tra vòng 3 | Bằng chứng | Kết quả / phạm vi |
|---|---|---|
| Full published | [admin-refinement-final/smoke.json](artifacts/winforms/admin-refinement-final/smoke.json) | **35 PASS, 26 PNG, exit 0**, kết thúc `2026-11-17T16:38:53Z` |
| Bốn role published | [admin-refinement-roles-final/smoke.json](artifacts/winforms/admin-refinement-roles-final/smoke.json) | **61 PASS, 54 PNG, exit 0**, kết thúc `2026-11-17T16:39:07Z` |
| Shared / session / calendar | [result.json](artifacts/winforms/admin-refinement-shared-v3/result.json) | **28 PASS, 6 PNG**, DPI 120; calendar, login validation/scroll containment, HTTP race và recovery |
| Finance refinement | [finance-refinement.json](artifacts/winforms/admin-refinement-finance-v3/after/finance-refinement.json) | **40 PASS, 19 PNG**, DPI 120; compact, chi tiết/phân bổ, filter, chart/keyboard, export |
| Finance regression | [finance-native.json](artifacts/winforms/admin-refinement-finance-v3/regression/finance-native.json) | **48 PASS, 1 chưa xác minh, 16 PNG**; SaveFileDialog vẫn chưa xác minh hoàn tất |
| Content views | [run-5/result.json](artifacts/winforms/admin-refinement-content-v3/run-5/result.json) | **28 PASS, 12 PNG**, DPI 120; validation, context, history, preview race và nội dung dài |
| Footer picker | [result.json](artifacts/winforms/admin-refinement-operation-footer-v3/result.json) | **61 geometry/behavior assertions, 2 PNG**, DPI 120; 440×380 và 680×540, nút chọn/hủy không bị che |

**LG-01/LG-02/SH-01:** URL dịch vụ không hợp lệ trong phần cấu hình đang thu được mở ra, focus đúng field và nằm trong khung cuộn. Calendar có chọn nhanh tháng/năm, F4, phím mũi tên/PageDown, giữ ngày hợp lệ cuối tháng/năm nhuận, giữ giờ và Escape không ghi giá trị chưa chọn. Khi API hết phiên, shell hiển thị recovery cho tài khoản hiện tại; hủy vẫn giữ draft, mật khẩu rỗng giữ dialog, đăng nhập lại **cùng actor và role** giữ đúng page và dữ liệu local. Kiểm tra live API xác nhận phiên mới hợp lệ và draft server chưa bị ghi. Ba race dùng HTTP handler có kiểm soát xác nhận 401 cũ không làm hết phiên mới, session-null cũ không xóa actor mới, và không đóng recovery giữa login với CSRF cuối. Không tự refresh/replay command sau đăng nhập lại. Dialog con không mở re-auth riêng: quay về shell để tiếp tục. Nhánh thay actor/role xóa cache được rà source, không giả là mutation quyền đã chạy.

**IN/CU/RP:** dữ liệu phụ vẫn truy cập đủ ở compact: tám trường chứng từ hóa đơn, phân bổ theo món từ DTO và tiền API, nguồn thu cùng phân bổ lồng nhau, sáu trường phụ của khách và địa chỉ đường phố. Picker cột nằm đúng dialog cha, không cho áp dụng bảng không có cột; Enter/Escape có tác dụng rõ. Filter cho đọc/cuộn/copy biểu thức dài trong dialog riêng, giữ editor; lựa chọn không còn trong lookup được giữ, số VND sai giữ dialog và focus field. Biểu đồ giữ đủ 36 kỳ, Home/End cuộn và Enter drill-down đúng kỳ. Export giữ query đã áp dụng; Enter tạo hai định dạng rồi bỏ default action để tránh tạo lặp. Tải file API đã kiểm; thao tác hoàn tất Windows SaveFileDialog vẫn là skip có lý do, không cộng vào PASS.

**OR/PU/CA và form dùng chung:** picker dòng có tìm kiếm/giữ lựa chọn, lỗi nhập tại dòng, xóa dòng giữ đúng payload; form tiếp tục mở nhóm lỗi và giữ input. Footer picker giữ nút chọn/hủy trên một hàng ở 440×380, kể cả tìm kiếm không có kết quả; kiểm cả bounds của ancestor và hit-test tâm nút. Scope 200 đơn, thiếu received-before/remaining và các projection khác không đổi. ActionSpec/DTO/quyền nghiệp vụ giữ contract hiện có; các test không submit giao dịch nghiệp vụ.

**CT-01/03/04:** lỗi gắn với section, không chuyển nhầm sang khối khác; quay lại vẫn thấy lỗi đó. Error row rỗng không chiếm chỗ. Client mở đúng nhóm cho thiếu ảnh/model, preset mở/gập với model điện thoại và model v003 không khớp sản phẩm theo ràng buộc service hiện có. Refresh giữ section/ngôn ngữ/version; history có người xuất bản/thời gian và scope 100 bản mới nhất, restore chỉ bật khi content chọn phiên bản. Preview đang chờ chặn các ghi/refresh xung đột, giữ input và dùng revision đã yêu cầu trong thông báo. Queue giữ dòng qua refresh/filter, đếm riêng title/subtitle mỗi ngôn ngữ; detail dài có thể chọn/cuộn đến cuối và Escape về đúng dòng. [README content](artifacts/winforms/admin-refinement-content-v3/README.md) phân biệt snapshot GET thật, input local và phản hồi preview giả trong handler: **không tạo preview grant/browser**, không save/publish/restore thật. Draft/history live trước–sau **DeepEquals, r3**.

Ảnh tiêu biểu: [calendar tháng/năm](artifacts/winforms/admin-refinement-shared-v3/calendar-month-year.png), [khôi phục phiên giữ draft](artifacts/winforms/admin-refinement-shared-v3/session-restored-draft.png), [biểu đồ cuộn cuối](artifacts/winforms/admin-refinement-finance-v3/after/report-chart-end-1000.png), [filter dài](artifacts/winforms/admin-refinement-finance-v3/after/filter-review-dialog.png), [picker compact](artifacts/winforms/admin-refinement-operation-footer-v3/item-picker-440x380.png), [history bằng bàn phím](artifacts/winforms/admin-refinement-content-v3/run-5/history-keyboard.png), [editor compact](artifacts/winforms/admin-refinement-content-v3/run-5/compact-content.png).

XLSX/PDF của full published dùng chung snapshot **`2987e18c-f4e2-40b6-b891-f406d0fee4fe`**, **8 hóa đơn / 97.130.000 VND**: [XLSX](artifacts/winforms/admin-refinement-final/invoices.xlsx), [PDF](artifacts/winforms/admin-refinement-final/invoices.pdf). Các tổng theo sản phẩm vẫn theo metric riêng của báo cáo, không thay bằng tổng hóa đơn.

Giới hạn giữ nguyên: không suy kiểm tra input local thành giao dịch ghi; chưa chạy physical DPI 100%/150%, Narrator toàn luồng hoặc mọi điều kiện offline/conflict. Các bằng chứng fail trung gian được giữ, gồm lỗi GDI khi tháo view, selection event sớm và kỳ vọng cũ cho nút restore; source/test tương ứng đã sửa và chạy lại. Vòng 3 không thay contract backend, schema, migration hoặc storefront ngoài phần logo đã có.

## Bằng chứng vòng 2 — lịch sử trước lượt tinh chỉnh

Build/publish vòng 2: **0 warning / 0 error**. [Build manifest vòng 2](artifacts/admin/build-manifest-round2.json) lưu hash executable, DLL và **39 source/assets**; [release verification vòng 2](artifacts/admin/release-verification-round2.json) ghi **0 source hash mismatch**, DLL khớp, 91 published checks và 75 PNG. [GUI vòng 2](artifacts/admin/running-admin-round2.json) ghi cửa sổ đăng nhập PID **38932**, HWND **264140**, visible lúc bàn giao đó. Các archive này là lịch sử; executable dùng chung đã được publish tiếp ở vòng 3.

| Bộ kiểm tra | Bằng chứng | Kết quả và phạm vi |
|---|---|---|
| N — full từ published executable | [admin-density-final/smoke.json](artifacts/winforms/admin-density-final/smoke.json) | **35 PASS, 26 PNG, exit 0**; kết thúc `2026-11-17T10:03:59Z`; login, invoice/query/detail, 21 module, 6 loại operations, export, logout |
| R — bốn role từ published executable | [admin-density-roles-final/smoke.json](artifacts/winforms/admin-density-roles-final/smoke.json) | **56 PASS, 49 PNG, exit 0**; kết thúc `2026-11-17T10:04:15Z`; home, quyền, navigation, disclosure, lifecycle, input local và compact |
| F — finance native bổ sung | [finance-native.json](artifacts/winforms/admin-redesign-finance-v2/finance-native.json) | **48 PASS, 1 chưa xác minh**; invoice/detail/customer, dữ liệu phụ, AST, query, report, export API, latest/cache/dispose |
| Controls native | [result.json](artifacts/winforms/admin-disclosure-controls/result.json) | **9 PASS**; calendar range, chọn ngày giữ giờ, Escape, dispose, popup combo và bo góc |
| Web brand regression | [results.json](artifacts/web/brand-redesign/results.json) | **16 kết quả đạt**, `errors: []`, `writes: []`; logo/focus/không tràn ngang ở 360/768/1366, bốn asset HTTP 200 |

Hai bộ published chạy Windows NT 10.0.26300 x64, DPI thực **120 (125%)**. Window 1366×768, 1920×1080 và compact 1000×720 được resize trên monitor hiện tại; không giả lập hoặc thay thiết lập DPI. Shell đo actual columns **[80,1268]** ở 1366 và **[280,1622]** ở 1920, không force-layout trong harness. Menu nhóm báo cáo compact đúng quyền, chọn module rồi đóng an toàn; nhóm wide mở/thu đúng control. Content không có nhóm báo cáo.

Invoice mặc định năm cột; detail ba nhóm và bốn cột hàng hóa, cột phụ/phân bổ/chứng từ/snapshot vẫn truy cập được. Customer ba nhóm giữ đầy đủ các history count đã trả từ API. Report giữ metric/kỳ/scope, bộ lọc phụ mở riêng; tổng sản phẩm **94.800.000 VND** không bị đổi thành tổng hóa đơn **97.130.000 VND**. XLSX/PDF full dùng chung snapshot **`681dc78b-c1a3-407b-bc06-bac557c6f251`**, **8 hóa đơn / 97.130.000 VND**: [XLSX](artifacts/winforms/admin-density-final/invoices.xlsx), [PDF](artifacts/winforms/admin-density-final/invoices.pdf). Tải qua API đã PASS; hoàn tất lưu qua **Windows SaveFileDialog chưa xác minh** vì Windows từ chối foreground trong lượt trước, automation không gửi Enter vào cửa sổ khác.

Operations dùng ba nhóm chi tiết đơn (kho hai), mặc định năm cột hàng hóa (kho ba); giữ tìm kiếm khi Back/đổi module. Catalogue đúng `variant_id`; cột Care được mở qua picker rồi refresh vẫn giữ cột và dòng. Record detail mặc định tối đa sáu scalar, mẫu test có hai nhóm vẫn truy cập đủ **12 scalar** nguồn. Validation của form tự mở nhóm lỗi và giữ input; scanner trùng giữ nội dung/focus. [Audit operation](artifacts/winforms/operation-density-audit/README.md) ghi source/contract và giới hạn; **34/34 ActionSpec**, context, DTO, roles được đối chiếu giữ nguyên.

Content queue còn ba cột; test mở dòng thứ hai → detail → đúng section `duo-story` → quay về giữ dòng chọn. Landing chỉ ba tab cấp 1, mặc định VI; EN và advanced mở đúng nhóm khi có lỗi, không bỏ mất dữ liệu phụ. Ở compact 1000×720, selector khối/ngôn ngữ/menu chung một hàng, vùng editor **215 px**, trường đầu và nút save/publish nhìn thấy đầy đủ; có kiểm tra clipping theo parent và hit-test tại tâm nút. Dirty modal **Ở lại** giữ input, **Bỏ thay đổi** phục hồi nội dung và page cache; không bấm lưu qua API. Draft/history trước và sau **DeepEquals, revision r3**. Owner chỉ đọc; ba vai trò còn lại không vào settings và API trả **403**.

Lifecycle kiểm kết quả cũ/mới đảo thứ tự, deactivate/dispose lúc đọc, điều hướng/filter nhanh, busy lồng nhau giữ focus/bounds và chặn click lặp. Reduced-motion override được reset trong finally; `SPI_GETCLIENTAREAANIMATION` trước/sau không đổi. Tất cả kiểm tra role chỉ dùng read/input local, không submit business command; đăng nhập/đăng xuất có tạo/thu hồi session thật.

| Nhóm ảnh vòng 2 | Bằng chứng native hiện tại |
|---|---|
| Login / shell / home | [Login](artifacts/winforms/admin-density-final/login.png), [owner](artifacts/winforms/admin-density-roles-final/owner-home.png), [kho](artifacts/winforms/admin-density-roles-final/warehouse_staff-home.png), [menu compact](artifacts/winforms/admin-density-roles-final/owner-navigation-reports-menu.png) |
| Hóa đơn / khách hàng / báo cáo | [Invoice detail](artifacts/winforms/admin-density-final/invoice-detail.png), [thanh toán](artifacts/winforms/admin-redesign-finance-v2/invoice-payment-detail.png), [hồ sơ khách](artifacts/winforms/admin-redesign-finance-v2/customer-profile.png), [bộ lọc phụ](artifacts/winforms/admin-redesign-finance-v2/report-extra-filters.png) |
| Đơn / record / form | [Chi tiết đơn](artifacts/winforms/admin-density-roles-final/owner-order-detail-embedded.png), [record tóm tắt](artifacts/winforms/admin-density-roles-final/owner-record-summary.png), [thông tin phụ](artifacts/winforms/admin-density-roles-final/owner-record-secondary-fields.png), [lỗi đúng nhóm](artifacts/winforms/admin-density-roles-final/owner-product-form-error-section-no-submit.png), [scanner](artifacts/winforms/admin-density-roles-final/warehouse-receipt-input-only.png) |
| Nội dung / editor / dirty | [Home](artifacts/winforms/admin-density-roles-final/content_staff-home.png), [khối](artifacts/winforms/admin-density-roles-final/content-section-detail.png), [editor](artifacts/winforms/admin-density-roles-final/content_staff-landing.png), [compact](artifacts/winforms/admin-density-roles-final/content-landing-compact.png), [lỗi EN](artifacts/winforms/admin-density-roles-final/content-landing-english-error.png), [lỗi nâng cao](artifacts/winforms/admin-density-roles-final/content-landing-advanced-error.png), [giữ nháp](artifacts/winforms/admin-density-roles-final/content-landing-dirty-keep.png), [bỏ nháp](artifacts/winforms/admin-density-roles-final/content-landing-dirty-discard.png) |

Giữ cả evidence trung gian: v2-1 đạt 50 checks/40 PNG; v2-2 phát hiện lỗi dispose context menu; root đã sửa thứ tự clear/dispose bằng `BeginInvoke`; v2-3/v2-4 đạt 56 checks/49 PNG. Review compact sau InputSurface 52 px dẫn đến gộp selector/menu, tăng vùng nhập. Những ảnh trung gian không thay bằng chứng final ở trên. DPI vật lý 100%/150%, manual Narrator/keyboard toàn luồng, offline/conflict và giao dịch ghi thực chưa kiểm ở lượt này. Khoảng thiếu backend trong inventory vẫn giữ nguyên.

## Bằng chứng vòng 1 — trước lượt giảm mật độ

**Lịch sử bàn giao vòng 1:** [build-manifest-round1.json](artifacts/admin/build-manifest-round1.json) lưu hash source/binary của lượt đó; build/publish 0 warning / 0 error. [running-admin-round1.json](artifacts/admin/running-admin-round1.json) lưu xác nhận cửa sổ đăng nhập đã mở lúc bàn giao vòng 1. Executable tại đường dẫn publish dùng chung đã được cập nhật ở các vòng sau; hai archive này và các ảnh dưới đây chỉ là lịch sử đối chiếu.

Hai lượt sau chạy **trực tiếp từ published executable**, đều exit 0: [full native](artifacts/winforms/admin-redesign-final/smoke.json) PASS **35 checks, 26 PNG**; [bốn role native](artifacts/winforms/admin-redesign-roles-final/smoke.json) PASS **41 checks, 26 PNG**. Native Windows NT 10.0.26300 x64, DPI thực **120 (125%)**. Không đổi DPI hoặc animation preference của Windows. Các lượt baseline/v2–v8 được giữ để đối chiếu; dùng hai thư mục `-final` cho bằng chứng bản bàn giao.

Role smoke chạy `--smoke --redesign-only`, chỉ đọc và thao tác input trong controls, không submit business command. Có login/logout, inline-invalid draft tại client rồi trả text ban đầu, scanner input rồi dispose. Test dirty modal sửa title local, chọn **Ở lại** giữ nguyên input, mở lại chọn **Bỏ thay đổi**, chuyển overview rồi trở lại đúng page cache/title gốc; không nhấn nút lưu qua API. Draft landing và lịch sử trước/sau **DeepEquals**, revision vẫn **r3**. Owner chỉ đọc; ba role không được mở settings và API trả 403. Context danh sách/chi tiết/command đóng không submit, catalogue action đúng variant, scanner trùng giữ input/focus đã PASS.

[Finance native](artifacts/winforms/admin-redesign-finance/finance-native.json) ghi **16 PASS, 1 chưa xác minh**: filter AST, tổng query, ba tab báo cáo, giữ dòng chọn, grid compact, export giữ query đã áp dụng, tải XLSX/PDF từ API, cache page, latest-filter và đóng detail đang tải. Report theo sản phẩm có tổng dòng **94.800.000 VND** theo metric đó; không đổi nó thành tổng hóa đơn 97.130.000 VND. Mục chưa xác minh là hoàn tất thao tác lưu qua **Windows SaveFileDialog**: dialog/filename đã được nhận diện nhưng Windows từ chối foreground, nên automation không gửi Enter vào cửa sổ khác. Đây là giới hạn bằng chứng, không đánh dấu lưu native đã PASS.

Lượt đầu phát hiện lỗi thật: invoice load race, CTS dispose lặp và Orders CurrencyManager trong selection lúc dispose. Các agent đã sửa và lượt native tiếp theo qua. Không xóa evidence fail. Phép đo v3 phát hiện shell đổi style từ 80→280 px nhưng layout chưa tính lại khi resize. Source đã sửa; bản final **không force-layout trong harness**, xác nhận actual columns `[80,1268]` ở window 1366 và `[280,1622]` ở 1920, DPI 120.

Review ảnh đã bắt thêm header compact bị toolbar che và input bị co chiều rộng. Header dùng đo/layout native; input frame giữ width sau tạo child. Bản final có kiểm tra parent clipping **và hit-test tại tâm nút** để bắt cả sibling che; bốn role home và Landing compact đều PASS. Ở window 1000×720, Landing giữ vùng editor **239 px**, trường title và save/publish nhìn thấy đầy đủ. Đây là responsive window layout, chưa thay cho test vật lý ở DPI 100%/150%.

| Nhóm | Trước | Sau / trạng thái |
|---|---|---|
| EV-01 login | `admin-redesign-before/login.png` | `admin-redesign-final/login.png` |
| EV-02 owner work | API/module baseline; chưa ảnh overview riêng | `admin-redesign-roles-final/owner-home*.png`  |
| EV-03 orders | `admin-redesign-before/orders.png` | `admin-redesign-final/orders.png`; `admin-redesign-roles-final/*-order-detail-embedded.png`, `*-orders-return-context.png` |
| EV-04/05 kho/nhập | `inventory.png`, `purchases.png`, `stock_adjustments.png` trong baseline | Full final cùng tên; role final `warehouse_staff-home.png`, `warehouse-receipt-input-only.png`; chưa submit mutation |
| EV-06 nội dung sản phẩm | `admin-redesign-before/catalogue.png` | `admin-redesign-final/catalogue.png`; role final `content_staff-home.png` là backlog landing thật |
| EV-07 landing | `admin-redesign-before/landing.png` (owner read-only) | Role final `owner-landing.png`, `content_staff-landing.png`, `content-landing-inline-error.png`, `content-landing-compact.png`, `content-landing-dirty-keep.png`, `content-landing-dirty-discard.png` |
| EV-08 invoices/detail | `invoices-1366x768.png`, `invoices-1920x1080.png`, `invoice-detail.png` | Full final cùng tên; source/runtime query/detail PASS; resize shell PASS |
| EV-09 filter | `admin-redesign-before/native-filter.png` | `admin-redesign-final/native-filter.png` |
| EV-10 reports | `reports.png`, `operational_reports.png` | Full final cùng tên và 6 báo cáo operations PASS |
| EV-11 export | `invoices.xlsx`, `invoices.pdf`, snapshot JSON | Full final XLSX/PDF chung snapshot `46e74a09-fa8d-4a2c-b535-8afb1f94c13f`, 8 hóa đơn / 97.130.000 VND. Native SaveFileDialog chưa xác nhận hoàn tất: Windows không cấp foreground cho automation; harness dừng trước khi gửi keyboard vào sai cửa sổ |
| EV-12 role/system | owner staff/settings/audit ảnh baseline; bốn role trước đó xem docs/winforms.md | Full final `staff.png`, `settings.png`, `audit.png`; role final đủ bốn role homes, UI/API denied settings |

Không khẳng định đạt Narrator, mọi DPI, offline/conflict, permission hoặc mutation chưa chạy. Luồng thiếu baseline vẫn là gap cần dữ liệu/API/command rõ, không tạo backend/migration thứ hai để minh họa thiết kế.
