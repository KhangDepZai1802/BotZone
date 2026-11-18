# Nguồn và giấy phép — BotZone 3D v003

## Lần sửa Duo

Nguồn thị giác chính là ảnh người dùng cung cấp `image(20261113-095526).png`, thể hiện Duo ở trạng thái đóng và mở. Đã xem ảnh theo chiều đọc đúng và đối chiếu bố cục màn hình: phong cảnh cồn cát/núi, tông xám be dịu, đồng hồ cao hẹp, ngày tiếng Anh và hai nút nhỏ ở góc dưới phải.

Wallpaper được tái dựng bằng công cụ tạo ảnh từ ảnh tham chiếu; không phải file wallpaper gốc trích từ Apple. Prompt và nguồn nằm trong `references/v003/wallpaper-generation.json`. Kích thước native là1496 × 1051; texture UI lớn hơn được compose riêng. Không tuyên bố wallpaper native 4K hoặc giống từng pixel ảnh gốc.

Đồng hồ 9:41 được dựng bằng đường vector theo hình dáng tham chiếu, ngày dùng Inter, các control là đồ họa do script tạo. Chúng là nội dung minh họa tĩnh. Gói không mô phỏng hoạt động của một hệ điều hành thật.

Mẫu màu vỏ gốc của Duo v003 được hiệu chỉnh thành graphite trung tính theo ảnh người dùng. Các preset màu tùy chọn kế thừa bộ trước là thông số PBR mô phỏng, không phải mã sơn vật lý chính thức.

## Model và thành phần kế thừa

Model tham số, scene, vật liệu, script và render được tạo cho BotZone. Pro Max giữ nguyên file GLB v002; các ảnh kiểm cũ được dùng lại theo SHA-256 khớp tuyệt đối, có ghi rõ nguồn bằng chứng trong báo cáo.

Nguồn đối chiếu hình học và màu của bộ trước được giữ ở `references/promax/` và `references/duo/`. Tài liệu ảnh/AR Apple phục vụ nghiên cứu nội bộ; không kèm mesh USDZ hay ảnh sản phẩm gốc của Apple trong gói. Logo/tên Apple, iPhone và thiết kế sản phẩm thuộc chủ sở hữu tương ứng; model không thể hiện chứng nhận của Apple.

## Thành phần bên thứ ba

| Thành phần | Giấy phép | Bản kèm gói |
| --- | --- | --- |
| Inter Variable | SIL Open Font License 1.1 | `source/v003/fonts/Inter-OFL.txt` |
| Three.js và addon | MIT | `preview-v003/vendor/three/LICENSE` |
| Khronos glTF Validator | Apache-2.0 | `preview-v003/vendor/gltf-validator/LICENSE` |

Giấy phép giữ nguyên khi phân phối các thành phần trên. Nguồn tải/integrity thư viện nằm trong các file `download-source.json`. Blender, Python, Node, Pillow và CairoSVG là công cụ hoặc dependency dựng, không kèm executable trong gói. Script compose dùng wallpaper đã lưu, không tự gọi dịch vụ tạo ảnh.
