# Kịch bản trình bày BotZone

**Bản nháp tại điểm dừng:** yêu cầu 13/11/2026 chuyển toàn bộ quản trị sang C# Windows Forms. Các bước admin là nghiệp vụ cần ánh xạ sang desktop, không phải web admin đang hoạt động. Chưa có seed, app chạy xuyên suốt hoặc WinForms. Xem [hiện trạng](implementation-status.md).

Đây là kịch bản nghiệm thu theo BA. Chỉ đánh dấu bước đạt sau khi chạy và lưu bằng chứng tại `implementation-status.md`; không dùng các giá trị kỳ vọng bên dưới thay cho kết quả truy vấn thực tế.

## Chuẩn bị

1. Chạy theo README; kiểm `docker compose ps`, migration và seed. Giữ một database demo và database golden/test tách biệt.
2. Sau khi triển khai hoàn tất, mở storefront web, quản trị WinForms và Mailpit. Kiểm customer trên web và bốn vai trò nhân viên trong WinForms.
3. Chụp snapshot dữ liệu bắt đầu hoặc backup trước khi trình bày. Dùng tài khoản demo trong README; không dùng dữ liệu thật.

## Luồng mua hàng, tiền và tồn

1. Customer xem danh mục/SKU, chọn biến thể còn hàng; đổi VI/EN và VND/USD. Kiểm URL/lựa chọn/giỏ được giữ và checkout vẫn tính VND.
2. Thêm hàng, áp voucher đã seed; xem giảm giá do server xác nhận. Refresh để chứng minh lựa chọn đọc lại từ DB.
3. Tạo đơn MoMo/Visa demo. Thử kết quả thất bại rồi thành công; bấm lặp cùng event/checkout key và đối chiếu chỉ một đơn, một khoản thu hợp lệ.
4. Nhân viên đơn nhận xử lý/xác nhận; kho gán serial đúng SKU, bàn giao vận chuyển. Mở invoice được tạo trong cùng transaction xuất kho.
5. Mở chi tiết invoice: tên/giá snapshot, giảm giá, Care/phí giao, nhân viên phụ trách và người phát hành. Đối chiếu header = tổng lines = tổng unit allocations.
6. Đơn COD: bàn giao/phát hành, mô phỏng đã giao nhưng chưa ghi receipt. Chứng minh dashboard không coi “đã giao” là “đã thu”; nhân viên đủ quyền mới đối soát COD.
7. Hai checkout tranh SKU cuối hoặc lượt voucher cuối: chỉ một được giữ; lỗi nghiệp vụ rõ, không âm tồn/quota.

## Database và tiêu chí học phần

1. Mở màn Hóa đơn WinForms: phân trang, tổng toàn bộ bộ lọc và tổng trang có nhãn riêng. Mở chi tiết từ một dòng. Màn này chưa được tạo.
2. Chỉ ra đường đi từ UI → route → query service/predicate → SQL tham số hóa → DTO `InvoiceListItem[]`.
3. Với golden, đối chiếu 8 issued; tổng phát hành `97.130.000`, điều chỉnh `3.500.000`, ròng `93.630.000`. Draft 99 triệu không được tính.
4. Báo cáo năm 2024/2025/2026 phải lần lượt `28.000.000 / 44.000.000 / 25.130.000`; kỳ trống hiện 0. G05/G06 cùng ngày UTC nhưng khác năm Việt Nam.
5. Theo nhân viên: NV01 `42.800.000`, NV02 `44.500.000`, NV03 `9.830.000`. Đổi tên/người xử lý hiện tại không chuyển doanh số lịch sử.
6. Theo sản phẩm: P13 `43.000.000`, P14 `41.000.000`, AP2 `10.800.000`; cộng Care `2.300.000` và giao `30.000` mới bằng tổng invoice.
7. Lọc `(tổng > 10 triệu AND tổng < 20 triệu) AND (NV01 OR NV02) AND NOT chứa AP2`: phải ra G02/G03/G07, count3, tổng `46.800.000`. Thử đổi nhóm ngoặc để thấy ngữ nghĩa thay đổi; thử NULL bằng lựa chọn riêng.
8. Dùng fixture75 hóa đơn và pageSize20; xuất cùng lúc XLSX/PDF. Mở file thật, kiểm đủ75, cùng ID/readAt/tổng, tiếng Việt rõ, mã/serial giữ số0 đầu. Kiểm mọi trang, không chỉ trang đầu.
9. Chạy test UPDATE/DELETE issued hoặc đổi issued→draft, ID hợp lệ nhưng sai order/invoice/parent; DB phải từ chối. Không sửa tay dữ liệu demo chính để chứng minh.

## Hậu mãi và quyền

1. Thu cũ điện thoại có serial và phụ kiện không serial, nhận từ xa; kho kiểm định, báo giá có version. Khách từ chối tạo luồng trả; khách đồng ý đúng version mới được chốt cấp credit đúng một lần.
2. Checkout dùng một phần credit; hai checkout đồng thời không tiêu vượt số dư. Trả một unit dùng đúng phân bổ lưu lúc mua.
3. Golden CN02 hoàn `450.000` credit và `1.050.000` tiền demo. Chứng từ giảm và refund có trạng thái/ngày riêng; điểm không biến thành tiền hoàn.
4. Trả lỗi/giao sai trong14 ngày: hàng nhận vào quarantine; không tự thành hàng mới chỉ vì đã hoàn tiền. Care chưa dùng gắn món trả được xử lý theo policy.
5. Care: xem ngày bắt đầu sau bảo hành tiêu chuẩn, gia hạn12 tháng, loại rơi vỡ/vào nước; nhánh đổi cùng SKU giữ thời hạn và liên kết serial, không tạo doanh số mới.
6. Customer gọi API admin bị chặn; content không đọc/xuất hóa đơn/sửa giá/hoàn tiền. Owner xem landing nhưng không publish/rollback; content thực hiện được. Thay role phải có hiệu lực trước lần ghi tiếp theo.
7. Đổi ID tệp/hồ sơ riêng tư không được đọc dữ liệu người khác; tệp giả/oversize/model không hợp lệ bị chặn.

## Nội dung, thiết bị và bàn giao

1. Content lưu nháp landing VI/EN; public không đổi. Publish tạo version; restore tạo publication mới, giữ lịch sử. Hai lần sửa cùng revision phải báo xung đột.
2. Mobile/laptop: 360/390/430/768/1024/1366/1440px, VI và EN. Kiểm cuộn dọc trên hero, explore đóng trả focus/vị trí, chỉ một scene hoạt động. Bật reduced motion hoặc mô phỏng lỗi WebGL để xem poster/CTA còn dùng được.
3. Backup khi đã dừng tác vụ ghi, restore vào database test mới cùng uploads; đối chiếu invoices/tổng/constraints/quyền tải tệp.
4. Khi dependency/image/media đã local, ngắt Internet rồi thử mua hàng, đăng nhập/Mailpit, admin, thanh toán demo và 3D. Chỉ gọi offline đạt sau khi quan sát đủ.

Trình bày kết thúc bằng các bằng chứng đã chạy và phần còn thiếu trong trạng thái triển khai; không nói toàn bộ BA đạt chỉ vì một luồng demo thành công.
