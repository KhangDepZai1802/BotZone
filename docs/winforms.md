# BotZone Admin — Windows Forms native

Ứng dụng `src/BotZone.Admin.WinForms` dùng .NET 10, Windows Forms, `BotZone.Contracts` và HTTP tới ASP.NET Core API. Không tham chiếu EF/Npgsql, không chứa kết nối PostgreSQL. Các form, sidebar, bảng dữ liệu, bộ lọc và CMS đều là control Windows native; preview landing mở trình duyệt ngoài.

## Chạy local

Yêu cầu Windows, SDK khóa ở `global.json` và API/seed local đã sẵn sàng. Không chạy seed/migration từ WinForms.

```powershell
./scripts/build-admin.ps1
dotnet run --project src/BotZone.Admin.WinForms
./scripts/build-admin.ps1 -Publish
```

Nếu Windows chặn file PowerShell, dùng `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-admin.ps1` (tùy chọn chỉ áp dụng cho tiến trình đó, không đổi policy hệ thống).

Bản publish mặc định cần .NET 10 Windows Desktop Runtime. Thêm `-SelfContained` để xuất cả runtime win-x64. Thư mục đầu ra `artifacts/admin/win-x64` được tạo local; script không triển khai ra ngoài. Trường API URL trên màn đăng nhập hoặc biến `BOTZONE_API_URL` cấu hình API, mặc định `http://localhost:5080`.

Phiên đăng nhập giữ trong `CookieContainer` của `HttpClientHandler` trong bộ nhớ. Lấy CSRF trước login, gửi `X-CSRF-TOKEN` cho POST, đổi CSRF sau login/logout. Không lưu mật khẩu, cookie, session hay connection string vào settings. Mỗi lần điều hướng đọc lại actor; API kiểm quyền hiện hành cho từng request. Không tự retry command thất bại; idempotency key được giữ theo nội dung lệnh khi thử lại trong cùng phiên ứng dụng, kể cả mở lại dialog sau lỗi mạng.

## Màn hình và thao tác

| Module | Giao diện / luồng |
|---|---|
| Hóa đơn | `List<InvoiceListItemDto>` → `BindingSource` → `DataGridView`; tìm có debounce 350ms, phân trang, tổng toàn bộ tập riêng tổng trang, sort, detail bằng Enter/nhấp đúp. |
| Bộ lọc | Cây AND/OR/NOT, lookup nhân viên/khách/sản phẩm, DateTimePicker, enum; NULL riêng; tối đa 4 cấp và 30 điều kiện; API validate lại. |
| Chi tiết hóa đơn | Header snapshot, dòng sản phẩm/Care/shipping, units, receipts, credit notes, refunds, attempts, ưu đãi; mở đơn và xuất chứng từ. |
| Báo cáo | Ngày Việt Nam, tháng/quý/năm, nhân viên, model/SKU; chỉ tiêu phát hành/điều chỉnh/ròng/thu/hoàn; drill-down phát hành theo model/nhân viên; SKU giữ ở bảng báo cáo để tránh mở sai tập invoice. |
| Báo cáo nghiệp vụ | Combo báo cáo tồn/giá trị kho, lợi nhuận, sản phẩm bán chạy, ưu đãi, thu cũ, Care; chọn khoảng ngày và tìm kiếm. Owner đọc toàn bộ; warehouse chỉ tồn; order staff chỉ ưu đãi. Definition, số liệu và thời điểm do API trả. |
| Đơn | Xác nhận, hủy trước bàn giao, bàn giao/phát hành, sự kiện giao demo, đối soát banking/COD, mở hóa đơn. API quyết định trạng thái hợp lệ. |
| Catalogue | Owner tạo nhóm/model nháp/SKU bằng trường native, chọn tương thích bằng CheckedListBox, sửa giá/trạng thái/Care/bảo hành; content biên tập VI/EN, SEO. |
| Kho/nhập hàng | Bảng lô/serial, form tạo/sửa nhà cung cấp, phiếu nhập; native grid chọn SKU/số lượng/giá vốn hoặc dòng nhận/serial; dòng nhận lọc theo phiếu; đề nghị và owner duyệt điều chỉnh tồn, chọn từng thiết bị khi theo serial. |
| Hậu mãi | Dialog tiếp nhận/kiểm tra/duyệt trả; trả lại hồ sơ không đủ điều kiện; hoàn đúng nguồn qua API; nhận/kiểm định/báo giá/owner duyệt/chốt thu cũ; Care owner duyệt đổi, warehouse xuất đúng serial/lô cùng SKU, order staff trả máy/đóng hồ sơ. |
| Khách hàng | Danh sách tên/email/điểm/hạng/số dư/chi tiêu ròng/số đơn; Enter/nhấp đúp hoặc Chi tiết mở form native tám tab: thông tin/số dư, đơn, địa chỉ, ledger credit, ledger điểm, trả hàng, thu cũ và Care. Chủ cửa hàng và xử lý đơn được đọc; nhấp đúp dòng đơn mở chi tiết đơn. |
| Ưu đãi/hệ thống | Native form tạo ưu đãi, chỉnh quyền/khóa staff, tỷ giá, chính sách; nhật ký chỉ đọc. |
| Landing CMS | Tree section, VI/EN, poster/model, preset, NumericUpDown camera/tilt/light/speed/fold; lưu nháp, publish, lịch sử/restore, mobile/laptop preview. Owner chỉ đọc; content mới có nút ghi. |

Danh sách ancillary dùng `DataGridView` với dữ liệu API và dialog trường native. Không có textarea JSON để thay cho form nghiệp vụ. Trạng thái rỗng, loading, lỗi API và hủy request có thông báo tại màn hiện hành. Hóa đơn/báo cáo giữ money VND nguyên dưới dạng string/decimal; không dùng float để tính tiền. Tất cả thay đổi đi qua `POST /api/commands`, khóa nút trong lúc gửi và yêu cầu API xác nhận.

Xuất XLSX/PDF: một `ExportRequest` với hai format tạo một snapshot; `SaveFileDialog` chọn từng đích lưu. Download ghi file `.partial` rồi chuyển sang tên cuối sau khi hoàn tất, không để file đích thành dữ liệu dở dang nếu lỗi. API kiểm lại quyền download; cookie không gửi sang origin khác.

## Hợp đồng bổ sung cho API

`GET /api/admin/lookups`: các mảng `{id,name}` gồm categories, staff, customers, products (thêm slug), variants (thêm productId), suppliers, lots, purchaseItems (thêm purchaseOrderId), inventoryUnits (thêm variantId/lotId), tradeInItems (thêm requestId), tradeInQuotes (thêm requestId/status), quoteItems (thêm itemId/requestId). Form không cho tự nhập GUID khi đã có lookup.

`GET /api/admin/data/{module}?q=&page=1&pageSize=20` trả `{rows,totalCount,page,pageSize}`. Module: categories, catalogue, inventory, suppliers, purchases, stock_adjustments, payments, returns, refunds, trade_ins, warranty, customers, promotions, media, staff, settings, audit. Rows có thể dùng snake_case lịch sử; client xử lý alias cho contextual IDs. Chọn bản ghi bằng ID trong hàng đang bind, không dùng vị trí trước khi sort.

`GET /api/admin/customers/{userId}` trả `{customer,balances,addresses,orders,creditEntries,loyaltyEntries,returns,tradeIns,claims,readAt,historyLimit}`. `balances` gồm `credit_vnd`, `balance_points`, `net_spend_vnd`, `tier`, `order_count`, `held_credit_vnd`, `held_points`. `CustomerDetailForm` chỉ đọc, dùng TextBox/Label native cho thông tin và `BindingSource`/`DataGridView` cho các lịch sử; refresh hủy request cũ, bỏ kết quả đến muộn, đóng form hủy request đang chạy. Hiển thị thời điểm snapshot và giới hạn 200 bản ghi mới nhất mỗi lịch sử. Không tự tính lại số dư/hạng trên client và không dùng JSON textbox.

`GET /api/admin/landing`: `{draft:{revision,sections},history:[{id,versionNo,publishedAt,createdBy}]}`. `save_landing {pageSlug:'home',revision,sections}`, `publish_landing {pageSlug:'home',revision}`, `restore_landing {pageSlug:'home',revision,versionId}`, `preview_landing {revision,viewport:'mobile'|'laptop'}` trả `{url}` với grant giới hạn. Không đưa session đăng nhập vào URL preview.

CRUD native: `save_category {id?,code,slug,nameVi,nameEn,active}`; `create_product {categoryId,modelCode,slug,generation?,sourceUrl,poster,nameVi,nameEn,businessStatus:'draft'}`; `create_variant {productId,sku,priceVnd,trackingMode,standardWarrantyMonths,careFeeVnd?,active,options:{color?,storage?,ram?,size?,connectivity?}}`; `set_compatibility {accessoryProductId,targetProductIds[]}`; `save_supplier {id?,code,name,contact,active}`. Cấu hình gửi `update_settings {key,value,reason}`; reason không nằm trong giá trị commerce. Các màn nhập này đã build; chưa coi mọi mutation CRUD là đã được nghiệm thu chỉ từ việc mở form.

Chuyển trạng thái model bằng `update_product_status {productId,businessStatus,reason}`. Báo cáo nghiệp vụ đọc `GET /api/admin/reports/operations?kind=inventory|margin|top_products|promotions|trade_in|warranty&from=YYYY-MM-DD&to=YYYY-MM-DD&q=...`, trả `{rows,columns,readAt,definition,fromInclusive,toExclusive}`. Tham số ngày là ngày lịch Việt Nam; ngày cuối loại trừ. Native không tính lợi nhuận hoặc tự chọn nguồn tài chính.

## Kiểm chứng

Ngày 13/11/2026: `dotnet build src/BotZone.Admin.WinForms/BotZone.Admin.WinForms.csproj --no-restore` đã thành công, **0 warning, 0 error**, SDK 10.0.203. Runtime API/seed và ảnh nghiệm thu chỉ được xác nhận bằng kết quả chạy thật bên dưới; build không thay cho runtime.

Script `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-admin.ps1` cũng đã chạy thành công Release với locked restore, 0 warning/0 error. Lần runtime `artifacts/winforms/layout-first` mở LoginForm thật trên Windows x64, DeviceDpi=120 (125%); ảnh login đã xem lại. API đang khởi động lại nên request login không kết nối được: `smoke.json` ghi **failed**, chưa xác nhận list/detail/export ở lần này.

Các lần chạy sau ngày 13/11/2026 đã có kết quả thực:

| Bằng chứng local | Kết quả |
|---|---|
| `artifacts/winforms/full-native/smoke.json` | **PASS**: login/session/CSRF; danh sách 85 invoice, detail/filter/reports; 19 module được liệt kê trong JSON, bao gồm CMS; XLSX/PDF chung snapshot; logout. Ảnh tại 1366×768 và 1920×1080, DeviceDpi thực 120 (125%). |
| `artifacts/winforms/full-final/smoke.json` | **PASS, 35 checks**: bản kiểm tra mở rộng với 21 module, gồm categories và màn báo cáo nghiệp vụ; cả sáu loại báo cáo vận hành đọc thật từ API; invoice/export/logout tiếp tục đạt. |
| `artifacts/winforms/roles-native-save/smoke.json` | **PASS**: mở form bốn role; owner nút CMS disabled và API publish 403; content invoice/price 403; warehouse export 403; order staff report chỉ scope của mình và settings 403; logout từng role. Nút Lưu nháp native chạy thật, revision 1→2 với nội dung giữ nguyên, không publish. |
| `artifacts/winforms/inventory-api-first/smoke.json` | **PASS**: nhận từng phần, retry không lặp receipt; chặn nhận vượt, duplicate serial, dòng thuộc PO khác; role separation; điều chỉnh serial/cách ly/khôi phục theo nguồn; chặn tồn âm. Tạo PO test riêng trên clone, giữ audit, không direct SQL/xóa/reset. |
| `artifacts/winforms/crud-native/smoke.json` | **PASS, 13 checks**: điền control thật rồi bấm AcceptButton trên NativeCommandForm để tạo nhóm/model/SKU, đổi trạng thái, chọn tương thích, tạo nhà cung cấp, lưu hai cấu hình. Content giả role owner vẫn 403. Kiểm tra DeepEquals xác nhận giá trị settings không đổi. |
| `artifacts/admin/win-x64/` | Publish local framework-dependent đã chạy thành công. |
| `artifacts/winforms/published-release/smoke.json` | **PASS**: DLL trong thư mục publish Release thực sự chạy login, typed invoice binding, hai kích thước native và filter tại DPI 125%; đây là mode layout-only, không lặp mutation tests. |
| `artifacts/winforms/native-handoff-second/smoke.json` | **PASS, 6 checks**: đơn COD `031a182a-59ba-4578-837b-1fa62df6fa33` do trình duyệt tạo; đăng nhập bằng nút LoginForm thật với NV01 rồi warehouse, chọn đúng dòng trong Orders, bấm native command/confirmation để confirm và handoff, logout cả hai phiên. API phát hành `BZ-HD-00000042` / `718486e2-9f26-4a5a-a412-ad3fbdd76ef2`; 10 PNG tại DPI 125%. Lần first chỉ lỗi race mở navigation trước mutation, đã sửa chờ màn chính khởi tạo. |
| `artifacts/winforms/main-lifecycle-before-stop/smoke.json` và `main-lifecycle-after-restart/smoke.json` | **PASS trên main `botzone`**: bản publish Release mới nhất chạy login/list/filter/hai kích thước trước và sau stop/start API+Next; cả hai lần đúng 8 issued invoices / 97.130.000 VND. Không chạy native fulfillment/CRUD/inventory mutation trên main. |
| `artifacts/winforms/customer-details-main/smoke.json` | **PASS**: owner và NV01 mở từ nút Chi tiết của ModuleView thật; tám tab, 10 đơn/1 địa chỉ/3 credit entries/1 loyalty entry/2 trả hàng/1 thu cũ, Care rỗng hiển thị đúng. Ảnh summary và ledger đã xem lại, DPI125%. Chỉ đọc nghiệp vụ. |
| `artifacts/winforms/customer-details-published/smoke.json` | **PASS**: chạy lại cùng luồng bằng DLL publish cuối có enum tiếng Việt; owner và NV01, 3 checks và 16 ảnh tab. DLL SHA-256 `BF41D0F995F6E38BD6806A178903846A13987BE8B8EBD7E0829A689D4D13AC42`. |

Export của full-native: export ID `d597dffb-1a68-4ff7-8907-8f713809b92c`, 85 dòng, 107.692.850 VND; XLSX 24.353 byte SHA256 `0CD3C7C59D20B1CA9CD21F16B76F32B905B3A3CAB39D8A82A84105303D447EE4`; PDF 60.503 byte SHA256 `4B1196A0503CC36DE657F1E1694705BBE6F64D5637C2B618DE1C8952EF862123`. Dataset clone tiếp tục thay đổi khi các tác nhân kiểm thử nghiệp vụ; đây là tổng tại snapshot ghi trong JSON.

Đã xem lại PNG login, invoice, filter, report và content CMS. Lỗi runtime `SplitContainer.RepaintSplitterRect` trên máy Windows này đã được sửa bằng TableLayoutPanel responsive native; các lần thất bại cũ được giữ làm lịch sử. Role smoke kiểm tra focus bằng `SelectNextControl` thật; chưa tuyên bố thử phím vật lý, DPI 100%/150% hay bấm SaveFileDialog thủ công. Full smoke tải export qua cùng ApiClient của UI; dialog lưu/cancel cần nghiệm thu tương tác thêm.

CRUD marker `WF20261113081013615A`: category `ec018754-11b4-4008-a32b-ac2eed4304d1` đã inactive; product `f24270f2-a53c-4a85-bc47-f9433f4cf18d` archived; variant `2344a35a-7556-4572-9134-2886d51a34fa` inactive; supplier `3b430cd8-fd49-4aa4-b6c8-f86fa562e6f9` inactive. Mọi hàng thử/audit còn nguyên trên clone. Đây là fixtures kiểm thử, không phải mẫu sản phẩm chính hãng dùng để bán. Ảnh form SKU/native controls cũng đã xem lại.

```powershell
# Seed demo owner phải tồn tại trong DB local do API quản lý.
./scripts/build-admin.ps1 -Smoke -ApiUrl http://127.0.0.1:5080
```

`--smoke` chạy Windows message loop thật, đăng nhập API bằng tài khoản demo owner (có thể đổi email/password qua `BOTZONE_SMOKE_EMAIL`, `BOTZONE_SMOKE_PASSWORD`), bind hóa đơn, mở detail/module, render native form 1366×768 và 1920×1080, lưu PNG, kiểm signature/hash file XLSX/PDF từ cùng snapshot và logout. `artifacts/winforms/<timestamp>/smoke.json` ghi pass/fail, lỗi thực, kích thước và `DeviceDpi`; không ghi mật khẩu/cookie. Nếu API/fixture chưa sẵn sàng, kết quả là fail, không dùng dữ liệu giả.

Chạy chuyên biệt bằng DLL: `dotnet src/BotZone.Admin.WinForms/bin/Debug/net10.0-windows/BotZone.Admin.WinForms.dll --smoke --roles-only --api http://127.0.0.1:5080 --output artifacts/winforms/roles-check`. Role smoke lưu lại cùng nội dung CMS qua nút native nên tăng revision. `--inventory-only` chủ động tạo PO test mới qua API; chỉ chạy trên clone/local test được chỉ định, không chạy vào dữ liệu thật để thử. `--layout-only` chỉ login, list và layout/filter; kết quả giới hạn này không thay cho full smoke.

`--crud-only` cũng chỉ dành cho clone: tạo fixture riêng qua các form native, thực hiện vòng đời model và lưu lại cùng giá trị settings (tăng version/audit), rồi archive/disable fixture. Không xóa/reset sau test. Mode full mặc định chỉ đọc nghiệp vụ, tạo phiên/export và thu hồi phiên khi xong; không chạy các CRUD/inventory mutation tests.

`--customers-only` đăng nhập owner và NV01, mở màn Khách hàng rồi bấm nút Chi tiết native, bind tám tab và chụp từng tab; không sửa điểm/credit/hồ sơ. Bộ fixture main hiển thị 160 điểm, 450.000 VND credit, hạng Member, chi tiêu ròng 16.000.000 VND tại snapshot kiểm thử.

`--smoke --fulfill-order=<GUID>` chủ động xác nhận và bàn giao **một đơn pending đã có** qua LoginForm → Orders → OrderDetailForm → NativeCommandForm, với NV01 và warehouse. Không tạo đơn mới, không reset đơn đã xử lý; chọn đúng clone và GUID trước khi chạy. Bước khách mở hóa đơn sau handoff đã PASS qua Playwright riêng `tests/web/native-handoff-live.mjs --verify`, evidence tại `artifacts/web/native-handoff-results.json`, invoice ID trùng `718486e2-9f26-4a5a-a412-ad3fbdd76ef2`, không có page errors. Nút nghiệp vụ đơn hàng bình thường cũng dùng cùng NativeCommandForm xác nhận. Màn kho chỉ hiện các tab packing API cấp quyền và địa chỉ giao hàng, không hiện tab thanh toán trống.

Ứng dụng khai báo PerMonitorV2/AutoScaleMode.Dpi. Smoke ghi DPI thật của màn hình hiện tại. Để xác nhận 125%/150%, chạy lại trên Windows đang ở đúng mức scale đó và kiểm ảnh/JSON; không coi phép phóng ảnh là nghiệm thu DPI. Cần kiểm thủ công Tab/Shift+Tab, focus, Enter/Esc, high contrast, thao tác kéo cửa sổ giữa màn hình, SaveFileDialog/cancel, mất mạng, hết phiên, 409 revision và bốn role. Không đổi cài đặt DPI hệ thống tự động.

Giao diện dùng Segoe UI hệ thống, nền sáng, sidebar tối, khoảng cách đều và focus native. Đã áp dụng hướng dẫn desktop/data-dense và keyboard focus của skill ui-ux-pro-max; các đề xuất web không áp dụng máy móc vào WinForms. API Windows Forms async theo [Microsoft .NET 10 WinForms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/whats-new/net100); cookie HTTP theo [HttpClientHandler.CookieContainer](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler.cookiecontainer?view=net-10.0).
