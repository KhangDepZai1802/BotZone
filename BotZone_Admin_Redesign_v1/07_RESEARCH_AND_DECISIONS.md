# Nghiên cứu và quyết định thiết kế

**Ngày tra cứu:** 17/11/2026. Nguồn ưu tiên: tài liệu sản phẩm, Microsoft Learn và repository chính thức. Những kết luận dưới đây là diễn giải để áp dụng vào BotZone; chưa phải kết quả đo usability trên các sản phẩm tham khảo.

## 1. Những gì học từ hệ thống quản trị hiện có

| Nguồn chính thức | Điểm có ích | Quyết định cho BotZone |
|---|---|---|
| [Business Central — Designing Role Centers](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-designing-role-centers) | Điểm vào làm việc được tổ chức theo vai trò, thông tin và tác vụ liên quan | Bốn trang đầu khác nhau: quyết định kinh doanh, hàng chờ đơn, phiếu kho, hàng chờ nội dung. Không dùng một dashboard chung cho mọi người. |
| [Shopify — Searching, filtering and sorting orders](https://help.shopify.com/en/manual/fulfillment/managing-orders/viewing-orders/searching-orders) | Chế độ xem lưu các lựa chọn như bộ lọc, cột và sắp xếp | Hàng chờ có tab/view dễ quét; quay lại giữ filter, sort, dòng chọn. |
| [Shopify — Bulk actions](https://help.shopify.com/en/manual/shopify-admin/productivity-tools/bulk-actions) | Phạm vi chọn hiện trang và toàn bộ kết quả cần phân biệt | Thanh chọn nhiều nêu số lượng và phạm vi; export không lẫn trang hiện tại với toàn tập lọc. |
| [Odoo 19 — Inventory adjustments](https://www.odoo.com/documentation/19.0/applications/inventory_and_mrp/inventory/management/inventory_adjustments.html) | On hand, counted và difference là các khái niệm khác; ghi số đếm khác áp điều chỉnh | Màn kiểm kê tách Sổ sách/Thực đếm/Chênh lệch; có lý do, lịch sử và quyền duyệt. Nhận một phần thể hiện Đặt/Đã nhận trước/Nhận lần này/Còn thiếu. |
| [ERPNext — Sales Analytics](https://docs.frappe.io/erpnext/sales-analytics) | Phân tích theo tiêu chí, khoảng thời gian và chứng từ nguồn | Bộ chọn kỳ/nhân viên/sản phẩm rõ; bảng số liệu và drill-down giúp đối chiếu hóa đơn. Công thức tiền tuân BA BotZone, không sao chép định nghĩa của ERP khác. |
| [Shopify — Theme editor features](https://help.shopify.com/en/manual/online-store/themes/customizing-themes/theme-editor/features-overview) | Cây thành phần, thuộc tính của phần đang chọn, xem trước theo viewport | Landing editor native có cây khối, biểu mẫu thuộc tính và preview storefront. Viewport điện thoại chỉ xem website bán hàng. |
| [Strapi — Draft & Publish](https://docs.strapi.io/cms/features/draft-and-publish) | Phân biệt bản nháp, trạng thái công khai và thay đổi cần publish | Tách Lưu nháp/Xem trước/Xuất bản; lỗi xuất bản giữ bản công khai và dữ liệu nhập. Không đưa Strapi vào backend. |

Không sao chép nguyên bộ UI, logo, template trả phí hoặc code của các sản phẩm tham khảo. Mẫu BotZone được tạo riêng bằng vector và dữ liệu minh họa. Giá trị tham khảo là cách sắp xếp công việc, trạng thái và bằng chứng để ra quyết định.

## 2. Tính khả thi trên WinForms

| Nguồn | Điều đã kiểm trong nghiên cứu | Cách áp dụng |
|---|---|---|
| [AntdUI repository](https://github.com/AntdUI/AntdUI) và [license](https://github.com/AntdUI/AntdUI/blob/main/LICENSE) | Thư viện control WinForms, license Apache 2.0 | Phương án cho shell/forms nếu dự án chưa có toolkit ổn. Codex phải chọn phiên bản stable tương thích `.csproj`, thử DPI/keyboard trước khi dùng rộng. Không tự hứa mọi control hỗ trợ mọi target. |
| [Krypton Standard Toolkit license](https://github.com/Krypton-Suite/Standard-Toolkit/blob/master/LICENSE) | License BSD 3-Clause | Nếu dự án đã dùng Krypton ổn, giữ và theme lại; không thêm AntdUI song song cho cùng trách nhiệm. |
| [Microsoft — Automatic scaling](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/forms/autoscale) | WinForms có cơ chế autoscale và cấu hình DPI phụ thuộc target/cách khởi tạo | Kiểm bootstrap, manifest, AutoScaleMode và toolkit hiện có trước khi chọn mode; tránh scale hai lần. Không chỉ đặt một cờ DPI rồi cho là xong. |
| [Microsoft — DataGridView scaling](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/best-practices-for-scaling-the-windows-forms-datagridview-control) | Style, autosize và cách quản lý hàng/cell ảnh hưởng hiệu năng | Shared styles, paging đang có và binding nhất quán; chỉ dùng virtual mode khi dữ liệu thực sự cần. |
| [Microsoft — Accessibility properties](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/properties-on-windows-forms-controls-that-support-accessibility-guidelines) | AccessibleName/Role/Description, TabIndex và keyboard cần được thiết lập | Giữ semantic control native, focus rõ, label thật, Tab order, mnemonic và kiểm Narrator trên app chạy thật. |

License được ghi đúng loại tại thời điểm nghiên cứu, không phải khuyến nghị bỏ qua license của các dependency hoặc font cụ thể khi đóng gói ứng dụng.

## 3. UI/UX Pro Max đã được áp dụng thế nào

Đã đọc skill UI/UX Pro Max và các hướng dẫn ưu tiên UX. Đã chạy truy vấn design system cho bối cảnh retail ERP/desktop operations và enterprise operations dashboard; đồng thời truy vấn UX về keyboard, focus, lỗi và form.

Các khuyến nghị về hệ chữ, phân cấp, khoảng cách, focus/validation được dùng trong bộ thiết kế. Một kết quả design-system trả về pattern “Hero + Features + CTA”, không hợp màn vận hành ERP; pattern đó bị loại. Palette và font tự gợi ý cũng không ghi đè định hướng trắng/đen/burgundy của BotZone. Skill không có stack WinForms riêng trong bản đã đọc; hướng dẫn WPF/WinUI không được coi là hướng dẫn WinForms.

Đây là sử dụng skill có đánh giá bối cảnh, không khẳng định skill nào “tốt nhất” hoặc output tự động luôn đúng. Prompt bàn giao yêu cầu Codex đọc/chạy skill trên repo thực tế, ghi các quyết định áp dụng và rà kết quả đã triển khai.

## 4. Phạm vi chứng cứ và điều chưa biết

- Repository công khai người dùng đưa là [KhangDepZai1802/BotZone](https://github.com/KhangDepZai1802/BotZone). Khi kiểm qua connector trong lượt nghiên cứu, metadata ghi size 0 và yêu cầu đọc contents trả “This repository is empty.” Vì vậy chưa quan sát được code hoặc màn WinForms hiện tại từ repository này.
- Nội dung nghiệp vụ được bám theo yêu cầu của người dùng và BA v1.1 WinForms đã có trong workspace. Kiến trúc .NET/ASP.NET/PostgreSQL trong tài liệu là baseline của BA; phiên bản project và dependency phải xác minh lại trên máy triển khai.
- Chưa chạy benchmark, test accessibility hoặc đo thời gian tác vụ trên app người dùng. Bộ tài liệu đưa tiêu chí nghiệm thu, không đưa số cải thiện giả định như kết quả đã đo.
- 14 ảnh là mẫu do bộ thiết kế dựng; chưa phải ảnh executable. Các module còn lại phải được áp cùng hệ thiết kế theo danh mục trong spec, không bị loại vì không có ảnh riêng.
- Dữ liệu và poster trong ảnh chỉ phục vụ minh họa. Redesign quản trị không xác nhận thông số, lịch ra mắt hoặc màu chính thức của iPhone trong catalogue mẫu.
