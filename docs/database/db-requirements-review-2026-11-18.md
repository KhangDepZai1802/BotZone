# Đối chiếu yêu cầu DB và kịch bản bảo vệ đồ án

Kiểm tra ngày **18/11/2026**, trên API đang chạy cổng 5080, database `botzone`, website cổng 3000 và bản quản trị `artifacts/admin/win-x64/BotZone.Admin.WinForms.exe`.

**Các nhóm yêu cầu được hỏi đều có triển khai.** Đã chạy bản WinForms thật để đọc danh sách/chi tiết, mở giao diện bộ lọc và xuất XLSX/PDF. Đã kiểm API lọc nâng cao bằng dữ liệu hiện tại, đối chiếu với SQL chỉ đọc và kiểm file xuất. Không kết luận toàn bộ đồ án đã nghiệm thu chỉ từ các kiểm tra này.

| Yêu cầu | Kết quả hiện tại | Điểm cần nhớ khi trình bày |
|---|---|---|
| Áp dụng khuyến mãi → DB | Có bảng mã, phiên bản, phạm vi, lượt dùng; quote đọc DB; đặt đơn ghi lượt và tiền giảm trong transaction | `DEMO1M` chỉ áp dụng sản phẩm `P13`/slug `p13`; không áp dụng mọi điện thoại |
| Danh sách và xem chi tiết hóa đơn | WinForms có Hóa đơn, tìm nhanh, vòng đời, phân trang, tổng, mở chi tiết | Đơn hàng và hóa đơn là hai nghiệp vụ riêng; hóa đơn phát hành khi bàn giao vận chuyển |
| DB → danh sách trong code | SQL được EF materialize thành `List<InvoiceListItemDto>`, trả API rồi bind `BindingSource` | Danh sách lấy DB; không phải mảng hóa đơn viết sẵn trong form |
| Tái sử dụng code đọc/tìm/xuất | List, detail, search và export dùng chung `ReadList`, `ReadDetail`, `InvoicePredicateBuilder` theo nghiệp vụ | Có thể chỉ trực tiếp caller trong IDE; không cần sửa code khi thay bộ lọc |
| Tìm kiếm nâng cao | Có >, <, =, <>, >=, <=, AND/OR/NOT, nhóm, NULL và chứa/không chứa sản phẩm | Bộ lọc nâng cao là chức năng quản trị WinForms; tài khoản khách trên website không có màn này |
| Đọc và xuất Excel/PDF | Xuất một hóa đơn hoặc toàn bộ tập lọc; XLSX/PDF là file thật, chung dataset/readAt | Tải file và mở file; không chỉ chụp nút “Xuất” |

## Chuẩn bị trước khi người chấm xem

1. API và website phải đang chạy. Nếu cần khởi động: `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-local.ps1 -Production`.
2. Mở `artifacts/admin/win-x64/BotZone.Admin.WinForms.exe`. API URL: `http://localhost:5080`.
3. Đăng nhập demo `owner@botzone.local`, mật khẩu demo công khai `BotZoneDemo!2026`. Dùng owner để thấy đủ màn Hóa đơn, Ưu đãi và thao tác tạo mã.
4. Mở IDE tại repo và kết nối PostgreSQL database `botzone`. Dùng cấu hình local đã có; không đưa thông tin kết nối riêng vào báo cáo hoặc slide.
5. Mở sẵn [SQL đối chiếu](db-requirements-demo.sql). Các câu SQL này đều chỉ đọc. Không seed/reset DB khi đang bảo vệ.

Số liệu tại lượt kiểm tra: **11 hóa đơn issued / 101.720.000 VND**, thêm **1 draft / 99.000.000 VND**. Tổng danh sách issued không cộng draft. Có **22 dòng invoice_items**. Cả 12 đầu hóa đơn đều khớp tổng dòng. Số liệu có thể tăng sau khi phát hành đơn mới; khi đó đối chiếu UI với SQL cùng bộ lọc, không buộc giữ nguyên 11.

## 1. Chứng minh khuyến mãi thật sự đọc và lưu DB

**Phần xem quản trị/DB đã có dữ liệu:**

1. Trong quản trị, vào **Ưu đãi**, tìm `DEMO1M`. Mã đang active, giảm cố định **1.000.000 VND**, giới hạn 100 lượt toàn mã/10 lượt mỗi khách. Phiên bản hiện tại chỉ nhắm tới sản phẩm `P13`.
2. Vào **Hóa đơn**, nhập `G08` vào **Tìm chứng từ**. Nhấp đúp dòng để mở chi tiết.
3. Trong chi tiết, vào **Thông tin → Ưu đãi và phiên bản**. Đối chiếu `DEMO1M`, phiên bản 1, `consumed`, giảm 1.000.000 VND.
4. Chạy câu SQL số 2 và 3 trong `db-requirements-demo.sql` để chứng minh các giá trị đó nằm trong bảng thật.

**Phần áp dụng mã trực tiếp trên website:**

1. Đăng nhập tài khoản khách demo, ví dụ `customer2@botzone.local` với mật khẩu demo ở trên.
2. Mở `http://localhost:3000/vi/products/p13`, thêm một sản phẩm P13 vào giỏ.
3. Để chỉ một dòng P13, tắt Care, không dùng điểm/số dư và xóa mã cũ. Tổng quote đã kiểm là **8.000.000 VND**.
4. Nhập `DEMO1M`, bấm **Áp dụng và tính lại**. Tổng quote đã kiểm thành **7.000.000 VND**.
5. Giải thích rõ: chỉ quote chưa ghi nhận lượt mua/giữ hàng. Khi xác nhận đặt đơn, backend mới ghi `promotion_redemptions` và phân bổ tiền giảm. Nếu tiếp tục demo đặt đơn, đọc lại DB để thấy lượt `reserved`; khi đơn được xác nhận, lượt chuyển `consumed`.

**Code mở để giải thích:**

- `src/components/commerce.tsx`: `Cart.price()` gọi `POST /api/quote`, gửi mã từ form.
- `src/BotZone.Api/Program.cs:64`: route quote gọi `CommerceService.Quote`.
- `src/BotZone.Infrastructure/Services/CommerceService.cs:23`: tìm mã và phiên bản trong `promotions`/`promotion_versions`; các dòng kế tiếp kiểm phạm vi, điều kiện và quota.
- `src/BotZone.Infrastructure/Services/CommerceService.cs:52`: đặt đơn ghi `promotion_redemptions` và `order_adjustments`.
- `src/BotZone.Infrastructure/Services/CommandService.cs:9`: transaction bọc lệnh, idempotency và audit.
- `docs/database/current-schema.sql:1205`: các bảng redemption, target, version và mã, cùng CHECK/FK/UNIQUE trong schema.

## 2. Danh sách hóa đơn và mở chi tiết

1. Trong quản trị chọn **Hóa đơn** ở thanh điều hướng hoặc ô chọn màn phía trên.
2. Chọn **Vòng đời → Đã phát hành**, xóa nội dung tìm/bộ lọc. Lượt kiểm tra hiển thị **11 hóa đơn / 101.720.000 ₫**.
3. Nhập `G08` vào **Tìm chứng từ**; nhấp đúp dòng, hoặc chọn dòng và nhấn **Enter**, hoặc **Thao tác → Mở hóa đơn đã chọn**.
4. Trong chi tiết, xem **Mặt hàng**, **Thanh toán**, **Thông tin**. Đối chiếu số lượng, đơn giá, giảm và thành tiền với câu SQL số 4.
5. Chuyển danh sách sang **Bản nháp** để cho thấy draft là một trạng thái riêng, không nằm trong tổng issued.

**Code:**

- `src/BotZone.Admin.WinForms/InvoicesView.cs:78`: `RefreshAsync()` gọi API danh sách.
- `src/BotZone.Admin.WinForms/InvoicesView.cs:84`: `binding.DataSource = data.Items`.
- `src/BotZone.Admin.WinForms/InvoicesView.cs:34`: `grid.DataSource = binding`.
- `src/BotZone.Admin.WinForms/InvoicesView.cs:108`: `DetailAsync()` mở `InvoiceDetailForm` theo ID dòng đã chọn.
- `src/BotZone.Admin.WinForms/InvoiceDetailForm.cs:36`: tải chi tiết; phần Thông tin có mục ưu đãi tại dòng 54.
- `src/BotZone.Api/Program.cs:84`: route danh sách; dòng 85: route chi tiết.

## 3. Chứng minh “đọc DB → danh sách”, và code có tái sử dụng

Mở `src/BotZone.Infrastructure/Invoices/InvoiceQueryService.cs:72`. Dòng đang chạy có dạng:

```csharp
List<InvoiceListItemDto> items = await Db.Database
    .SqlQueryRaw<InvoiceListItemDto>(sql, parameters.Bind())
    .ToListAsync(cancellationToken);
```

Đây là cách rút gọn để thuyết trình; dòng code thật có SQL projection, WHERE, ORDER BY và phân trang.

Chỉ tiếp luồng **PostgreSQL → EF query → List DTO → API JSON → BindingSource → DataGridView**. Khi người chấm hỏi tái sử dụng, mở các caller sau:

| Nghiệp vụ | Đoạn dùng chung |
|---|---|
| Danh sách/tìm kiếm | `InvoiceQueryService.cs:58` gọi `ReadList`; dòng 64 dựng bộ lọc chung |
| Chi tiết một hóa đơn | `InvoiceQueryService.cs:79` gọi lại `ReadList` với `invoiceId` |
| Xuất danh sách | `InvoiceExportService.cs:100` gọi lại `queries.ReadList(..., all: true, ...)` |
| Xuất một hóa đơn | `InvoiceExportService.cs:88` gọi lại `queries.ReadDetail(...)` |
| Điều kiện/phạm vi quyền | `InvoicePredicateBuilder.cs:117`, `Build()` dùng chung whitelist và bind parameters |

Không nói “đã có class Repository riêng” nếu chỉ theo tên class: code đọc DB hiện nằm trong các service/query builder nói trên. Yêu cầu tái sử dụng được thể hiện bằng các lời gọi thật, không phụ thuộc tên lớp là Repository.

## 4. Bộ lọc nâng cao: thao tác và kết quả để đối chiếu

**Ví dụ dễ làm và đã kiểm kết quả:**

1. **Hóa đơn → Thao tác → Xóa bộ lọc**, xóa ô tìm, chọn **Đã phát hành**.
2. Bấm **Bộ lọc**. Chọn nút gốc **Tất cả điều kiện (AND)**.
3. Bấm **+ Điều kiện**; chọn **Trường dữ liệu: Tổng hóa đơn**, **Toán tử: Lớn hơn (>)**, **Giá trị: 10000000**.
4. Chọn lại nhóm gốc rồi thêm điều kiện thứ hai: **Tổng hóa đơn**, **Nhỏ hơn (<)**, **20000000**.
5. Bấm **Áp dụng**. Với dữ liệu kiểm tra, kết quả là **6 hóa đơn G01, G02, G03, G04, G07, G08 / 87.300.000 ₫**. Chạy SQL số 5 để đối chiếu.
6. Để chứng minh `<>`, xóa bộ lọc và tạo **Tổng hóa đơn khác 18000000**: hiện **9 hóa đơn / 65.720.000 ₫**, không có G03/G04.

**Ví dụ nhóm và NOT:**

```text
AND
  OR
    Tổng hóa đơn > 10000000
    Mã hóa đơn = BZ-HD-00000003
  NOT
    Tổng hóa đơn = 18000000
```

Dùng **+ Nhóm** để tạo nhóm OR, đổi **Quan hệ nhóm → Ít nhất một điều kiện (OR)**. Để tạo NOT, chọn điều kiện/nhóm cần phủ định rồi **Thêm → Phủ định NOT**. Kết quả tại lượt kiểm tra: **5 hóa đơn / 52.830.000 ₫**. Câu SQL số 7 tương đương. Nếu người chấm hỏi ngoặc, đổi vị trí nhóm và giải thích tập kết quả thay đổi theo AND/OR.

**Code:** `FilterDialog.cs:23` dựng giao diện; dòng 109 dựng cây điều kiện. `InvoicePredicateBuilder.cs:117` dựng SQL, dòng 143 biên dịch AND/OR/NOT. Giá trị dùng `NpgsqlParameter`; không nối trực tiếp nội dung nhập vào SQL.

## 5. Xuất Excel và PDF, đối chiếu với danh sách

1. Giữ bộ lọc **10 triệu < tổng < 20 triệu** để có 6 kết quả/87.300.000 ₫.
2. Trên danh sách bấm **Xuất danh sách → Tạo Excel / PDF**.
3. Kiểm tra hộp kết quả ghi cùng số hóa đơn, tổng tiền và thời điểm đọc.
4. Bấm **Lưu XLSX**, chọn chỗ lưu; bấm **Lưu PDF**, chọn chỗ lưu.
5. Mở cả hai file. Excel có sheet **HoaDon**, **ChiTietHoaDon**, **DieuKienLoc**. Đối chiếu mã, dòng, tổng và bộ lọc; PDF phải mở thật, đọc được tiếng Việt.
6. Để xuất riêng một hóa đơn: mở chi tiết G08 → **Xuất hóa đơn → Tạo Excel / PDF** → lưu hai định dạng.

**Code:**

- `InvoicesView.cs:113`: xuất `loadedQuery`, dùng toàn bộ tập lọc.
- `InvoicesView.cs:201`: gọi API tạo file; dòng 216: hộp thoại chọn đường dẫn và tải file.
- `InvoiceExportService.cs:38`: đọc dataset một lần trong snapshot; dòng 56 dựng cả hai định dạng từ dataset đó.
- `InvoiceExportService.cs:100`: xuất list gọi lại `ReadList` với `all=true`, không chỉ trang đang xem; tối đa 10.000 hóa đơn.
- `InvoiceExportService.cs:190`: dựng XLSX bằng ClosedXML; dòng 311: dựng PDF bằng MigraDoc/PDFsharp.
- `InvoiceQueryService.cs:47`: snapshot `RepeatableRead`, read-only.

## Bằng chứng đã chạy và giới hạn kết luận

- `artifacts/winforms/db-requirements-review-20261118/smoke.json`: bản quản trị thật đạt; có ảnh danh sách, bộ lọc, chi tiết, Ưu đãi và các module. Kiểm ở Windows DPI 125%, hai kích thước 1366×768/1920×1080.
- `artifacts/winforms/db-requirements-review-20261118/invoices.xlsx` và `invoices.pdf`: cùng `exportId`, `readAt`, 11 hóa đơn/101.720.000 VND. Đã đọc cấu trúc workbook: 3 sheet, sheet HoaDon có 13 hàng gồm tiêu đề và tổng. PDF đọc được 2 trang và có tổng 101.720.000/G08.
- `artifacts/db-requirements-review/api-checks.json`: các phép lọc AND/OR/NOT/khác, tổng chi tiết của cả 11 issued và quote P13 giảm 1 triệu đều có assertion đạt.
- Lượt này không tự thao tác hộp thoại lưu bằng tay hoặc mở Microsoft Excel tương tác. Hãy làm bước chọn nơi lưu/mở hai file trước người chấm.
- 73 ca C# trong `tests/BotZone.IntegrationTests/last-result.json` là bằng chứng **ngày 13/11/2026 trên clone test**, không phải 73 ca vừa chạy lại trên DB chính. Không dùng số lượng test để khẳng định mọi tiêu chí đồ án đã nghiệm thu.
- Hai tài liệu cũ `docs/query-catalog.md` và `docs/database/rubric-mapping.md` còn ghi `/api/checkout/quote`; route thực hiện tại là **`POST /api/quote`**. Khi trình bày dùng `Program.cs` và hướng dẫn này.
- Bản quản trị của dự án là **ứng dụng WinForms**, không có trang quản trị React/Next.js. Website có hóa đơn của riêng khách; tìm kiếm nâng cao và danh sách quản trị đầy đủ nằm trong WinForms.
