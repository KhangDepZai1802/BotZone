# So sánh thông số sản phẩm

Cập nhật 13/11/2026. Trang `/vi/compare` và `/en/compare` dùng cùng dữ liệu API đang phục vụ 35 model, 609 mục song ngữ, 75 mã trường. Giới hạn vẫn là tối đa ba sản phẩm cùng nhóm. Không gắn quy tắc vào tên hoặc ID một cặp sản phẩm cụ thể.

## Hành vi hiện hành

`src/components/specification-engine.ts` chuẩn hóa đơn vị, nhận diện điều kiện đo và khoảng cấu hình rồi trả kết quả cùng lời giải thích VI/EN. `specification-comparison.ts` giữ adapter cho nơi gọi cũ và lấy dung lượng từ SKU active. `product-compare.tsx` hiển thị các giá trị nguồn, nhóm thông số, giá/cấu hình cửa hàng, nguồn hãng và chế độ chỉ xem khác biệt.

Ô có ưu điểm theo một thông số được in đậm, nền xám nhạt và viền đen mảnh. Lý do ngắn nằm ngay dưới giá trị; nhấn lý do hoặc biểu tượng thông tin của dòng để xem điều kiện. Enter/Space mở, Escape đóng và trả focus về nút mở. Trắng/đen/xám thay toàn bộ nhấn màu nâu/champagne ở bảng. Header sản phẩm vẫn cố định; vùng bảng tận dụng chiều cao cửa sổ sau header website.

Đây là đối chiếu có quy tắc, không gọi mô hình AI hoặc tạo điểm tổng thể cho sản phẩm. Nhiều lõi CPU/GPU là số lõi cao hơn; nó không phải phép đo chạy nhanh hơn. Thế hệ chip, Wi-Fi và Bluetooth mới hơn không tự chứng minh tốc độ thực tế. Chỉ so chip trong cùng dòng/tier và cùng chip phụ.

## Phạm vi quy tắc

| Nhóm | Giá trị được đối chiếu | Điều kiện |
|---|---|---|
| CPU/GPU | Tổng số lõi và khoảng cấu hình | Cả khoảng phải vượt bên còn lại; không lấy GB của điều kiện SSD làm số lõi. |
| RAM, lưu trữ, băng thông | GB/TB, GB/s | RAM/lưu trữ hãng dùng mức tối đa và ghi rõ; dung lượng cửa hàng là hàng riêng. Băng thông khác dung lượng. |
| Màn hình | Số điểm ảnh, ppi, Hz tối đa, độ sáng | Độ sáng thông thường/toàn màn hình, HDR và ngoài trời tách riêng. Nhiều màn hình/kích thước vỏ cần điều kiện đối chiếu rõ. |
| Chip/kết nối | Thế hệ cùng dòng, chuẩn Wi-Fi/Bluetooth, tốc độ cổng Mb/s/Gb/s | Không xếp hạng đầu cắm hoặc suy ra tốc độ thực tế; cổng vào/ra đa điều kiện không gộp. |
| Vật lý | Khối lượng, khối lượng hộp, chiều dày/chiều cao có thứ tự trục rõ | Nhẹ hơn/thấp hơn là chiều so riêng; không tính thể tích thành điểm. Không trộn mỗi tai với cả bộ hoặc pin rời với kính. |
| Pin | Wh; số giờ với cùng tác vụ | Wh không phải thời gian sử dụng; phát video, streaming, Wi-Fi web, ANC và chế độ nguồn là các ngữ cảnh khác nhau. |
| Sạc | Phút tới cùng phần trăm pin; công suất theo MagSafe/Qi2/Qi | Công suất củ sạc yêu cầu không phải tốc độ máy; khả năng tương thích của ốp không phải công suất sạc. |
| Tính năng xác định | Áp lực, nghiêng, hover, haptic, Find My | Chỉ so có/không được công bố rõ; thiếu thông tin không coi là không hỗ trợ. |

Engine có parser số lõi Neural Engine để dùng khi dữ liệu có trường phù hợp. Danh mục hiện tại chưa có trường đó; không tự tạo thông số từ nguồn chưa được nhập. Camera, chất liệu, công nghệ tấm nền và kích thước màn hình vẫn hiển thị đầy đủ để đối chiếu nhưng không có thứ hạng chất lượng chung. Những thông tin mô tả hoặc parser không nhận diện được giữ nguyên và có lời giải thích; không đoán số còn thiếu.

## Hai và ba sản phẩm

Mọi sản phẩm phải có dữ liệu tương thích ở dòng đang xét. Cần ít nhất một điều kiện chung. Một ô chỉ nhận ưu điểm nếu không kém tất cả ô khác ở mọi điều kiện chung và cao/thấp hơn phù hợp ở ít nhất một điều kiện. Hai giá trị cao nhất đồng hạng có thể cùng được nhấn khi có sản phẩm thứ ba thấp hơn; toàn bộ bằng nhau thì không nhấn. Khoảng chồng lấn hoặc đánh đổi giữa các điều kiện không tạo bên thắng toàn dòng. Đổi vị trí hoặc xóa một sản phẩm tính lại kết quả.

Các trường dùng mức tối đa được ghi rõ: lưu trữ, RAM hãng và Hz. Các trường khác không tự biến khoảng cấu hình thành giá trị lớn nhất. Một giá trị thiếu/không rõ khiến dòng đó không chọn ưu điểm trên toàn bộ lựa chọn. Điều này tránh coi dữ liệu trống là 0 hoặc ngầm chấm thua sản phẩm chưa đủ dữ liệu.

## Ví dụ iPad trong yêu cầu

| Thông số | Air 11 M4 | Pro 11 M5 | Cách diễn giải |
|---|---|---|---|
| CPU | 8 lõi | 9 lõi ở 256/512 GB; 10 ở 1/2 TB | Pro nhiều lõi ở cả hai mức, được in đậm. |
| GPU | 9 lõi | 10 lõi | Pro nhiều lõi hơn, được in đậm. |
| RAM hãng | 12 GB | 12 hoặc 16 GB theo dung lượng | Nhấn **RAM tối đa** 16 GB; giữ nguyên điều kiện. SKU 256 GB hiện có chỉ có 12 GB. |
| Độ sáng đối chiếu | 500 nit | 1000 nit toàn màn hình; 1600 nit đỉnh HDR | So 500 với 1000 ở ngữ cảnh chung, không lấy HDR 1600 thay số toàn màn hình. |
| Độ phân giải | 2360 × 1640; 264 ppi | 2420 × 1668; 264 ppi | Pro nhiều điểm ảnh hơn; ppi bằng nhau, không gọi mật độ cao hơn. |

Đã kiểm lại số liệu với [Apple iPad Air](https://www.apple.com/vn/ipad-air/specs/) và [Apple iPad Pro](https://www.apple.com/vn/ipad-pro/specs/). Khi thêm Air M3, hai Air cùng CPU 8/GPU 9 không nhận ưu điểm số lõi; Pro vẫn được nhấn. [Audit nguồn và giới hạn dữ liệu](../artifacts/specifications-research/detailed-comparison-audit.md) ghi rõ cấu hình thực tế và các thông số hãng có công bố nhưng chưa nằm trong danh mục.

## Kiểm chứng và vận hành

- `npm.cmd test`: 42/42 nhóm đạt, giữ đủ 10 kiểm thử filter cũ; 22 nhóm mới kiểm ngữ nghĩa, đơn vị, khoảng, phủ định và điều kiện.
- [Corpus hiện hành](../artifacts/comparison/detailed-corpus.json): 28 trường có dữ liệu, 1.397 cặp và 8.806 bộ ba, 121.889 lần gọi bao gồm hoán vị và VI/EN. Đây là kiểm tính nhất quán thuật toán, không phải 121.889 phép kiểm trình duyệt hoặc xác minh độc lập từng số liệu hãng.
- `npm.cmd run typecheck` và Next production build đạt. [12 ca Edge cuối](../artifacts/web/semantic-compare/README.md) đạt cho hai/ba iPad ở VI/EN,390×844/1366×768; laptop thấy8–10 dòng, header cố định140px. Mobile là giả lập viewport trên Windows.
- Không đổi DB, schema, backend, giá, tồn kho, dữ liệu thông số hoặc model 3D trong đợt này. Những kết quả BA/WinForms trước đó giữ nguyên phạm vi.
