# Kịch bản trình bày BotZone v1.1

Chạy local theo README, mở web `http://localhost:3000`, API `http://127.0.0.1:5080`, Mailpit `http://localhost:8025` và `artifacts/admin/win-x64/BotZone.Admin.WinForms.exe`. PostgreSQL nằm trong Docker; desktop không kết nối DB trực tiếp.

## Chuẩn bị

1. Khởi động bằng `scripts/start-local.ps1`. Chỉ truyền `-Migrate -Seed` khi cần chủ động migration/seed và API đã dừng; không reset dữ liệu để demo.
2. Demo password chung `BotZoneDemo!2026`: web `customer@botzone.local`; native `owner@botzone.local`, `nv01@botzone.local`, `warehouse@botzone.local`, `content@botzone.local`. NV03 inactive, dùng để chứng minh lịch sử báo cáo, không dùng đăng nhập.
3. Xem `docs/test-evidence.md` để phân biệt bằng chứng đã chạy và mục còn kiểm thủ công. Main có golden; clone test có thêm PAG75/giao dịch, phải lọc G01…G08 khi so tổng.

## Luồng web → WinForms → web

1. Web chọn sản phẩm còn hàng, thêm giỏ, đăng nhập customer, điền địa chỉ Việt Nam; đổi VI/EN/VND/USD để xem lựa chọn còn giữ, checkout vẫn VND.
2. Với P13, áp voucher `DEMO1M`, tải lại để chứng minh giỏ/voucher lưu DB. API tính quote và kiểm lại khi đặt; giá demo không phải giá thị trường.
3. Chọn COD và xác nhận đặt hàng. Ghi mã đơn, không tạo đơn thay thế nếu chỉ muốn retry.
4. WinForms nv01 → Đơn hàng → tìm mã đơn → xác nhận. Đăng xuất, đăng nhập warehouse → tìm chính đơn đó → bàn giao/xuất kho. Inventory/serial và invoice được ghi trong một transaction C#.
5. Customer tải lại đơn trên web → Xem hóa đơn → tải PDF/XLSX. Customer2 không được đọc hóa đơn/file của customer.
6. Nv01 mô phỏng shipment `in_transit`, rồi `delivered`; COD chưa thu không được coi là đã thu. Tạo payment attempt và ghi success đúng số tiền; trạng thái hoàn tất chỉ sau khi đủ điều kiện.
7. MoMo/Visa: customer chỉ chọn kịch bản demo của payment thuộc đơn mình; banking/COD cần owner/nv01. Không nhập thông tin thanh toán thật.

Bài tự động chia đúng ba giai đoạn: `tests/web/native-handoff-live.mjs` tạo đơn qua browser → WinForms `--fulfill-order <id>` thao tác native → script browser `--verify` mở hóa đơn. Kết quả trong `artifacts/web/native-handoff-results.json` và native smoke tương ứng, không coi gọi HTTP từ test là bấm form.

## Trình bày tiêu chí database

1. Owner mở Hóa đơn: danh sách/chi tiết, page20, tổng trang và tổng toàn bộ bộ lọc có nhãn riêng. Trỏ mã `InvoiceQueryService.ListInvoicesAsync` → SQL Npgsql có tham số → `ToListAsync()` → `List<InvoiceListItemDto>` → API DTO → `BindingSource` → `DataGridView`.
2. Lọc golden G01…G08: issued 8, tổng97.130.000; CN3.500.000; ròng93.630.000. D01 draft99triệu không được tính.
3. Thống kê năm2024/2025/2026:28.000.000 /44.000.000 /25.130.000. G05/G06 kiểm biên năm Việt Nam; tháng/quý trống hiện0.
4. Staff NV01/NV02/NV03:42.800.000 /44.500.000 /9.830.000. NV03 inactive vẫn xuất hiện; đổi người xử lý đơn/tên hiện tại không chuyển doanh số invoice snapshot.
5. Model P13/P14/AP2:43.000.000 /41.000.000 /10.800.000; cộng Care2.300.000 và giao30.000 mới bằng tổng issued. Không cộng header sau JOIN dòng/attempt.
6. Bộ lọc native: `(tổng > 10 triệu AND tổng < 20 triệu) AND (NV01 OR NV02) AND NOT chứa AP2` → G02/G03/G07, count3, tổng46.800.000. Đổi nhóm ngoặc; xem NULL bằng điều kiện riêng.
7. Clone PAG75, pageSize20 → xuất cả XLSX/PDF: đủ75, cùng readAt/exportId/manifest/tổng, không chỉ trang đang xem. Mở file thực, xem mã/serial0 đầu và tiếng Việt; chọn đường dẫn bằng SaveFileDialog.
8. `docs/database/query-catalog.sql`, `rubric-mapping.md`, `erd.svg`, `data-dictionary.md` giải thích bảng/PK/FK/check/index/query. Test bất biến/wrong-parent/concurrency chạy trên clone, không sửa tay invoice main.

## Hậu mãi, kho và quyền

1. Nhập PO một phần rồi đủ: owner duyệt, warehouse nhận đúng nguồn/serial, không vượt PO; kiểm kê cần lý do và owner duyệt, không kho tự duyệt.
2. Return lỗi/giao sai trong14ngày: customer gửi từng món và ảnh riêng; nv01 tiếp nhận, kho kiểm, owner duyệt CN và hoàn. Hàng về quarantine; chỉ restock khi đúng điều kiện mới. G08 phân bổ mỗi AP2 net1.500.000 → credit450.000 +cash1.050.000.
3. Trade-in trực tiếp/từ xa: nhận/kiểm từng món, quote có version/expiry/duyệt giá, customer quyết định đúng version. Hoàn tất mới cấp credit một lần; phí hai chiều không trừ ngầm giá thu. Hàng thu cũ không vào tồn mới bán.
4. Care: hiệu lực12tháng sau bảo hành tiêu chuẩn; hardware, không rơi/vào nước. Sửa→không sửa được→đổi cùngSKU hoặc ownerduyệt hoàn; serial/quantity có nguồn và giữ thời hạn còn lại.
5. Kiểm quyền: owner xem CMS nhưng không save/publish/restore; content không giá/tồn/invoice/refund; warehouse chỉ dữ liệu đóng gói và KPI kho. Native khóa nút và API vẫn từ chối gọi trực tiếp.

## CMS và vận hành

1. Content mở Landing: câysection, VI/EN, chọn product/poster/model/preset, sửa thông số trong giới hạn. Save draft không đổi public. Preview dùng đúng renderer storefront; grant một lần/có hạn/thu hồi được.
2. Publish tạo version; History/Restore tạo version mới, không sửa lịch sử. Owner chỉ xem/giám sát. Hai lần lưu cùng revision trả409 và giữ nội dung chưa gửi.
3. Website360/390/430/768/1024/1366/1440: cuộn hero, explore/hinge/reset/close focus; reduced motion và WebGL lỗi vẫn có poster/CTA. Model Blender do session khác phụ trách; model hiện tại là minh họa local.
4. Backup chỉ sau khi dừng writer, restore luôn vào DB mới. Đối chiếu rows/sequence/golden/hash media rồi đăng nhập/tải file/check quyền trên API phục hồi. Không chạy drop/reset để demo.
5. Checklist thủ công còn ghi rõ: DPI100/150%, nhiều màn hình, phím Tab/Shift+Tab/Enter/Esc vật lý, SaveFileDialog/save/cancel, mất mạng/hết phiên, và demo khi thực sự tắt Internet. Không đánh pass từ mã hoặc từ ảnh scale.
