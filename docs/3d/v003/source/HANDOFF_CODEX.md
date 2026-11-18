# Handoff Codex — BotZone 3D v003

## Yêu cầu lần sửa

Duo dùng một màn khóa phong cảnh theo ảnh tham chiếu, trên cả màn ngoài và màn trong. Không đưa lại giao diện app/widget của v002. Khi mở Duo, màn ngoài phải tắt; khi đóng lại phải sáng. Tích hợp GLB v003 và dùng đầy đủ clip hiện có.

## Model và clip

- GLB dùng mét, Y-up; `DeviceRoot` ở identity. GLB Duo mặc định đóng; preview chủ động scrub tới trạng thái mở.
- Clip `Duo_OpenClose` dài 2 giây: progress 0 đóng, 1 mở.
- Ngoài các track bản lề, spine và morph của màn hình uốn, clip có track scale chuẩn glTF cho `ScreenOuter`.
- Track power dùng STEP: scale 1 ở t = 0; scale 0 từ t = 1/30 giây đến hết clip. Tương đương ngưỡng progress 1/60. Giữ đúng interpolation và giữ cả track này khi optimize hoặc chuyển format.
- `ScreenOuterGlass` là lớp kính tối luôn tồn tại dưới lớp ảnh. Không xóa node này khi gộp mesh. Khi lớp ảnh bị ẩn, vẫn có bề mặt kính và phản xạ của điện thoại.
- Không xoay hinge bằng tay đồng thời với mixer. Không chỉ lấy rotation track rồi bỏ morph/scale track.
- Nên dùng một animation action ở chế độ giữ pose cuối, scrub progress vào [0, 2] giây, tránh lặp về t = 0 khi đang mở 100%.

## Màn hình và màu

`MAT_ScreenOuter` và `MAT_ScreenInner` dùng base color đen và texture trong `emissiveMap`. Giữ thông số emission thực tế từ GLB. Trên Three.js, đặt `toneMapped=false` riêng cho hai material ảnh, texture ở sRGB và `flipY=false`. Giữ UV, sampler và trilinear mipmaps. Có thể dùng anisotropy giới hạn bởi GPU; viewer chặn ở mức tối đa 8.

Mặt kính tắt `ScreenOuterGlass` phải dùng vật liệu tối có phản xạ, không emission. Giữ tone mapping thông thường của kính. Không áp preset màu vỏ lên vật liệu màn sáng hoặc kính màn tắt.

Màn trong có một atlas liên tục qua `ScreenInnerFixed`, `ScreenInnerFlex`, `ScreenInnerMoving`; không lặp toàn bộ ảnh ở từng nửa. UV không đổi khi gập.

Pro Max giữ các chế độ lockscreen/wallpaper từ v002. Duo chỉ dùng lockscreen gốc; không thêm menu ứng dụng hoặc lựa chọn màn home cũ.

## File dùng trong web

Copy model cần dùng từ `exports/v003/models/` vào thư mục asset public của repo. Chọn đúng bản desktop/mobile và lazy-load theo viewport. Dùng poster khi đang tải hoặc WebGL không khả dụng. HTML preview là công cụ kiểm tài sản, không nên nhúng nguyên snapshot chứa cả bốn GLB vào landing page.

Các texture mặc định đã nhúng trong GLB. PNG master và wallpaper trong `source/v003/textures/` dùng để chỉnh; không cần tải chúng cùng trang web. Muốn chỉnh chữ/bố cục, sửa `scripts/compose_duo_screens_v003.py`, chạy lại compose rồi rebuild Duo.

Giữ cuộn mobile bình thường, chỉ bật kéo xoay khi người dùng chọn khám phá 3D. Tôn trọng reduced motion, dừng render khi canvas không hiển thị, đo DPR/FPS trên thiết bị thật.

## Kiểm khi tích hợp

1. Kiểm đúng version/hash trong `asset-manifest.json` và không dùng GLB v002 đã cache.
2. Mở/đóng/scrub ngược nhiều lần: màn ngoài sáng lúc đóng, tối lúc mở; không bị lóe sáng khi giữ progress 1.
3. Xoay sang phía ngoài trong khi đang mở để kiểm mặt kính tối và camera trước.
4. Kiểm chữ đúng chiều, hình liền qua nếp gập, mặt kính không bị tint theo màu vỏ.
5. Kiểm laptop/mobile, tải lỗi, touch và hiệu năng tại repo thật.

Bộ bàn giao có bằng chứng cấu trúc và render Blender. Browser/WebGL runtime chưa được xác minh trong môi trường dựng; không thay bước kiểm trực tiếp bằng kết quả validator.
