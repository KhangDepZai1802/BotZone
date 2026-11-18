# Kiểm tra bàn giao — BotZone 3D v003

Đã kiểm cấu trúc GLB, texture nhúng, UV, animation và ảnh render từ GLB nhập lại. Chưa kiểm runtime trình duyệt hoặc thiết bị thật.

## File cuối

| Model | Byte | MB thập phân | Triangle | Khronos error / warning |
| --- | ---: | ---: | ---: | ---: |
| iPhone 18 Pro Max desktop | 3,232,596 | 3.233 | 119,522 | 0 / 0 |
| iPhone 18 Pro Max mobile | 2,038,128 | 2.038 | 68,212 | 0 / 0 |
| iPhone Duo desktop | 4,697,068 | 4.697 | 98,172 | 0 / 0 |
| iPhone Duo mobile | 2,519,684 | 2.520 | 53,564 | 0 / 0 |

Tất cả dưới mục tiêu mobile 4 MB / desktop 10 MB. Model dùng mét, Y-up, root DeviceRoot identity. Buffer và texture mặc định được nhúng vào GLB. File ảnh rời chỉ dùng khi chủ động đổi chế độ màn hình.

## Đã xác minh

- Contract inspector và Khronos Validator đã chạy trên bốn file cuối; checksum khớp manifest và dữ liệu nhúng trong HTML tự chứa.
- Pro Max không thay byte GLB so với v002; dùng lại bộ ảnh và báo cáo nhập lại Blender của v002, đối chiếu SHA-256 khớp tuyệt đối. Báo cáo ghi rõ nguồn bằng chứng.
- Duo desktop được nhập lại và render màn ngoài sáng khi đóng, màn trong mở, gập một nửa và góc nhìn màn ngoài tắt khi đang mở. Cùng một atlas phủ cả hai nửa và vùng uốn. Chữ không bị soi gương và hình không bị lặp.
- Duo dùng hai ảnh lock screen; không có app/widget/home texture của v002. Đồng hồ được dựng bằng vector theo tỷ lệ tham chiếu, ngày là Wed Apr 1.
- Duo_OpenClose có bốn kênh, 0–2 giây. Track mới điều khiển scale của ScreenOuter bằng STEP; tắt từ 1/30 giây, bật lại khi scrub về đóng. Mặt kính tối ScreenOuterGlass luôn tồn tại riêng. GLB ở tư thế đóng; preview mặc định mở vào màn khóa trong.
- Màn hình giữ base color đen, không color map, ảnh đặt trong emissiveMap. Strength Pro Max 1.1, Duo 1.0. Preview đặt toneMapped=false riêng cho material ảnh; kính tắt giữ shader phản xạ thông thường.
- Ảnh kiểm dùng Cycles với Standard view transform, exposure 0. Đây là cài đặt của renderer, không phải ảnh được vẽ đè lên model.
- Bộ điều khiển màn hình được kiểm bằng JavaScript không qua browser: giữ alpha/base color/emission, sRGB, flipY=false, đổi về ảnh nhúng, cache và lựa chọn bất đồng bộ.

## Phạm vi hình ảnh

Wallpaper Duo được tái dựng theo ảnh người dùng cung cấp, native 1496 × 1051. Atlas lớn hơn giúp phần chữ/vector sắc nét, không biến wallpaper thành ảnh native 4K. UI chỉ có ngày, giờ và control tĩnh. Không tuyên bố đây là wallpaper gốc chính thức Apple hoặc giống từng pixel. Vỏ graphite và phản xạ được chỉnh theo hướng tham chiếu.

## Giới hạn kiểm tra

Browser/GLTFLoader thực tế, FPS, draw call, VRAM, context-loss, touch và thiết bị thật chưa được xác minh. Lần kiểm browser trước bị chặn socket, yêu cầu chạy ngoài sandbox bị automatic approval review từ chối, và cloud browser từ chối URL file local. Không chạy đường vòng để vượt chặn. Các kết quả ở đây không được gọi là browser pass.

Cycles có thể còn hạt nhỏ ở vùng kim loại/kính. Không có cam kết giống CAD hoặc sản phẩm thật tuyệt đối. Đánh giá cuối về thẩm mỹ dựa trên ảnh render và preview khi mở trên máy người dùng.

## Bằng chứng

- asset-manifest.json: file, byte, SHA-256, material, animation và giới hạn.
- preview-v003/qa/asset-validation-summary.json: tổng hợp validator và HTML.
- preview-v003/qa/*.inspection.json và *.khronos-report.json: output kiểm tra từng GLB.
- reports/v003/promax-glb-reimport.json và duo-glb-screen-review.json: hash GLB dùng cho ảnh render.
- reports/v003/duo-*-uv.json và duo-*-stats.json: UV, ảnh nhúng và power contract.
- reports/v003/duo-screen-textures-manifest-v003.json: nguồn và thông số bố cục màn hình.
