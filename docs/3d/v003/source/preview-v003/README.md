# BotZone 3D Asset Preview v003

Viewer độc lập để xem **chính GLB** trong bộ asset. Không có API, tài khoản,
dịch vụ cloud hoặc CDN. Three.js và các addon đã được lưu cục bộ.

## Mở ngay không cần server

Nhấp đúp **BotZone_3D_Preview_v003.html** trong Chrome hoặc Edge. File này đã nhúng
Three.js, CSS, cấu hình, cả bốn GLB và JPEG hình nền Pro Max trong lần build; không tải CDN, không
cần Python/Node, không cần Internet. Xem standalone-build.json để biết
chính xác SHA-256 và phiên bản GLB đã nhúng.

Khi thay GLB nguồn, phải build lại HTML; nó không tự đọc file bên ngoài.
Bản index.html và hướng dẫn bên dưới vẫn hữu ích khi phát triển hoặc
đổi model thường xuyên.

## Mở bản phát triển trên Windows

Đứng ở thư mục gốc của gói sau khi giải nén (thư mục có `exports/`, `source/` và `preview-v003/`):

```powershell
py -3 preview-v003/serve.py --open
```

Nếu máy dùng lệnh `python`, thay `py -3` bằng `python`.
Trang sẽ mở tại http://127.0.0.1:8765/preview-v003/ .
Giữ cửa sổ terminal mở trong khi xem. Nhấn Ctrl+C để dừng.

Nếu cổng 8765 đã được dùng:

```powershell
py -3 preview-v003/serve.py --port 8766 --open
```

Chỉ BotZone_3D_Preview_v003.html mở trực tiếp được. Không mở index.html bằng cách nhấp đúp: browser cần HTTP để tải ES module và GLB.
Cần browser có WebGL 2. Không cần Internet sau khi đã có bộ bàn giao đầy đủ.

## Điều khiển

- Mặc định mở iPhone Duo trên nền xám sáng trung tính. Có thể chuyển sang Pro Max, chọn chất lượng mobile/desktop và góc nhìn.
- URL device/background hợp lệ được ưu tiên. Đổi sản phẩm sau đó giữ nền người dùng đã chọn.
- Pro Max mở ở góc trước ¾ để thấy màn hình khóa Burgundy. Duo mở 100% và nhìn vào màn hình khóa bên trong; màn ngoài đã tắt.
- Nút Màn ngoài gập Duo về 0% và chuyển góc nhìn ra màn ngoài. Nút Màn trong mở 100% và nhìn vào màn trong. Đặt lại khôi phục góc nhìn/độ mở mặc định của sản phẩm.
- Pro Max có bộ chọn Màn hình khóa / Chỉ hình nền. Đổi lựa chọn không tải lại GLB hoặc đặt lại góc nhìn.
- Duo chỉ có giao diện khóa, không có bộ chọn nội dung. Khi gập lại, màn ngoài bật; khi mở máy, màn ngoài tắt và mặt kính tối vẫn hiện diện.
- Màu mặc định là màu gốc từ GLB.
- Các màu nằm trong viewer-config.json, được sao chép từ material-presets.json của từng model trong references/. Giá trị là sRGB do người dựng hiệu chỉnh từ reference, không phải mã sơn hãng.
- Mỗi preset ánh xạ đầy đủ theo tên material, gồm camera island, logo và antenna. Nhãn màu có tiếng Việt/Anh; chọn Original để khôi phục chính xác màu gốc trong GLB.
- Duo dùng clip Duo_OpenClose: 0% gập, 100% mở, kéo ngược để gập lại.
- Ánh sáng trung tính và nền sáng/tối phục vụ đối chiếu hình học/vật liệu.
- Lưu ảnh PNG chụp canvas thực tế của model đang hiển thị.
- Trên thiết bị cảm ứng, nhấn Khám phá 3D để bật kéo xoay;
  Kết thúc khám phá để vuốt dọc cuộn trang.
- Tôn trọng reduced motion; xoay tự động mặc định tắt.

## Đường dẫn asset

viewer-config.json định nghĩa đường dẫn tương đối. Mặc định:

- ../exports/v003/models/iphone-18-pro-max-desktop.glb
- ../exports/v003/models/iphone-18-pro-max-mobile.glb
- ../exports/v003/models/iphone-duo-desktop.glb
- ../exports/v003/models/iphone-duo-mobile.glb

Tham số URL hỗ trợ `device=promax|duo`, `variant=desktop|mobile`,
`view=front|back|left|right|top|bottom|three-quarter|front-three-quarter|inner-three-quarter`,
`display=lockscreen|wallpaper` (Pro Max),
`fold=0..1`, `background=light|dark`, `lighting=neutral|hero`,
`capture=1` để ẩn giao diện khi chụp ảnh.

## Chế độ màn hình

| Sản phẩm | Nội dung | Hành vi |
| --- | --- | --- |
| Pro Max | Màn hình khóa / Lock screen | Texture gốc trong GLB |
| Pro Max | Chỉ hình nền / Wallpaper | Đổi emissiveMap của MAT_ScreenOuter |
| Duo | Màn hình khóa | Mở: màn trong khóa, màn ngoài tắt. Gập: màn ngoài khóa bật |

Duo không có màn hình ứng dụng/widget hoặc chế độ hình nền riêng.

Màn OLED có baseColor đen và ảnh trong emissiveMap. Controller giữ nguyên
cách dùng texture của material gốc: không thêm base-color map khi GLB không có.
Không đổi alpha, opacity, màu nền, cường độ emissive, roughness hoặc clearcoat.
Giữ cường độ theo từng GLB: Duo 1.0, Pro Max 1.1. Màu Original giữ đúng màu
khung/lưng gốc của file đang tải, gồm graphite mới của Duo; preset màu có tên
là lựa chọn riêng.
Viewer đặt toneMapped=false chỉ cho MAT_ScreenOuter/MAT_ScreenInner, kể cả
texture gốc, để đường cong studio không làm giảm tương phản của màn OLED.
Các vật liệu thân máy/kính lens giữ cấu hình tone mapping ban đầu.
Texture thay thế được đánh dấu sRGB và flipY=false; giữ UV channel, transform
và sampler. Texture màn hình dùng trilinear mipmaps; anisotropy được giới hạn
ở giá trị nhỏ hơn giữa 8 và khả năng của GPU. Thiết lập áp dụng cả texture
gốc lẫn hình nền Pro Max thay thế. Màu thân máy không tác động đến màn hình
hoặc mặt kính ngoài MAT_ScreenOuterGlass.

Ảnh chỉ decode khi chọn chế độ, sau đó được cache trong phiên xem model.
Đổi sản phẩm/phiên bản giải phóng cache và khôi phục map gốc trước khi hủy model.
Đổi chế độ liên tiếp chỉ áp dụng lựa chọn mới nhất. Khi ảnh không tải được,
viewer giữ lại màn hình trước đó và hiện lỗi ngay dưới bộ chọn.

Ảnh bổ sung duy nhất là ../exports/v003/textures/promax-wallpaper.jpg,
được nhúng trong HTML. Các giao diện khóa mặc định đã nằm trong GLB.

### Bật/tắt màn ngoài của Duo

Bật/tắt là một track scale STEP trong clip GLB Duo_OpenClose. Viewer chạy
đầy đủ clip và không có cơ chế bật/tắt riêng bằng JavaScript. Màn ngoài bật
khi gập kín, tắt từ khoảng 1/60 hành trình mở; kéo ngược dưới ngưỡng này sẽ
bật lại. Mặt kính ScreenOuterGlass tách khỏi node hoạt hình để vẫn hiện khi
lớp phát sáng tắt. Trạng thái ngắn “Màn ngoài đã tắt” trong UI được đọc từ
scale thực tế sau khi animation cập nhật, không dùng để điều khiển model.

## Kiểm tra GLB

Python inspector dùng standard library, không cần cài package:

```powershell
py -3 scripts/validate_glb.py exports/v003/models/iphone-18-pro-max-desktop.glb
```

Inspector đo kích thước ở pose mặc định, byte/checksum, mesh, triangle,
material, ảnh, external URI, node root và animation. Nó không thay thế
Khronos glTF Validator và không đánh giá độ giống sản phẩm thật.

Khronos validator chính thức đã được lưu trong vendor/gltf-validator.
Nếu máy có Node.js:

```powershell
node preview-v003/validate-official.cjs exports/v003/models/iphone-18-pro-max-desktop.glb
```

Hai lệnh ghi report bên cạnh GLB. Có thể truyền nhiều đường dẫn để kiểm nhiều file.

## Browser QA tự động

run-browser-qa.cjs cần Playwright và Chromium của môi trường chạy QA.
Đây là tiện ích dành cho người triển khai, không cần để mở viewer bằng tay.

Snapshot v003 đã hoàn tất kiểm tra **cả bốn GLB cuối cùng**. Inspector đạt;
Khronos ghi **0 error / 0 warning** trên cả bốn file. Material contract xác
nhận baseColor đen, emissiveMap riêng, emission Duo 1.0 / Pro Max 1.1, và
Duo chỉ chứa ảnh khóa màn trong/màn ngoài.

Hai file Duo đều có bốn channel trong Duo_OpenClose. Track ScreenOuter.scale
là STEP với 61 mẫu: bật ở t=0, tắt từ t=0.03333299979567528 giây, tương đương
progress 0.01666649989783764. Phép thử animation bằng Three.js trong Node xác
nhận mở → tắt, kéo ngược/gập → bật. ScreenOuterGlass không kế thừa scale của
lớp phát sáng. Đây là kiểm tra animation không có browser, không phải chứng
nhận hình ảnh WebGL.

HTML cuối cùng nhúng **4 GLB + 1 JPEG hình nền Pro Max**. Tất cả phần nhúng
đã được giải mã và đối chiếu byte/SHA-256 với file xuất cuối cùng. Không có
import bên ngoài. JavaScript trích từ HTML cũng qua kiểm tra cú pháp.
HTML: **17,655,800 bytes**, SHA-256:
`1fdb40ded229f3ccbba0e121ccbe7c215077748ed418fa05f05001759c6e88f4`.
Cấu hình/markup mặc định là Duo mở hoàn toàn, màn khóa trong, nền xám sáng.

| GLB | Dung lượng | Khronos |
| --- | ---: | --- |
| iPhone 18 Pro Max desktop | 3,232,596 bytes | 0 error / 0 warning |
| iPhone 18 Pro Max mobile | 2,038,128 bytes | 0 error / 0 warning |
| iPhone Duo desktop | 4,697,068 bytes | 0 error / 0 warning |
| iPhone Duo mobile | 2,519,684 bytes | 0 error / 0 warning |

Báo cáo tổng hợp: qa/asset-validation-summary.json. Power samples:
qa/duo-power-behavior.json. Chi tiết material/sampler: qa/preview-contract.json.
Non-browser texture tests: qa/display-mode-contract.json. SHA-256 từng tài
nguyên và HTML: standalone-build.json.

Chưa xác nhận chạy browser: môi trường dựng chặn Unix socket của Chromium,
và yêu cầu chạy ngoài sandbox bị chính sách tự động từ chối. Browser được
cấp qua CUA cũng từ chối mở file:// theo URL policy. Không có phép kiểm
runtime browser đã đạt; không dùng host/publish hay URL thay thế để vượt chặn.
Bản HTML được đóng gói để người nhận mở trực tiếp trên máy của họ.
Kiểm cấu trúc, cú pháp và non-browser unit check không chứng minh chất lượng
hiển thị WebGL, hiệu năng thiết bị hay độ giống sản phẩm thật.

Chạy lại kiểm tra map và cấu hình:

```powershell
node preview-v003/verify-preview-contract.cjs
node preview-v003/test-display-modes.mjs
py -3 preview-v003/verify-standalone.py
```

## Dependency và giấy phép

- Three.js 0.187.0 — MIT, xem vendor/three/LICENSE.
- gltf-validator 2.0.0-dev.3.10 — Apache-2.0, xem thư mục vendor/gltf-validator.
- Phiên bản, tarball và integrity được lưu trong download-source.json của từng package.

Không có decoder Draco/KTX2/Meshopt trong viewer hiện tại. Bản GLB bàn giao cần
dùng core glTF hoặc extension được GLTFLoader hỗ trợ trực tiếp; nếu nén theo
các định dạng trên, người tích hợp phải bổ sung decoder local tương ứng.

## Build lại HTML tự chứa

Cần Node.js và esbuild cài trong phạm vi preview-v003/.tooling. Từ thư mục gốc của gói:

```powershell
npm install --prefix preview-v003/.tooling --no-audit --no-fund --package-lock=false esbuild
node preview-v003/build-standalone.cjs
```

Builder yêu cầu đủ cả bốn GLB và tất cả JPEG được cấu hình; thiếu file sẽ dừng
thay vì xuất bản snapshot không đầy đủ. Builder ghi checksum từng GLB, texture
và toàn bộ HTML. Không phân phối thư mục .tooling cho người chỉ cần xem model;
nó không cần để mở HTML.

## Nguồn và người tạo

Preview được tạo trong phiên làm việc BotZone này; GLB do pipeline Blender của
bộ bàn giao xuất. Đây không phải viewer hoặc asset được Apple chứng nhận.
Reference, kích thước và tên màu được đối chiếu từ:

- https://www.apple.com/vn/iphone-18-pro/specs/
- https://www.apple.com/vn/iphone-duo/specs/
- https://www.apple.com/iphone-duo/specs/ (Night Sky / Star White).

Nguồn preset vật liệu: references/promax/material-presets.json và
references/duo/material-presets.json. Giá trị màu là artist approximation;
không đồng nhất tên thương mại chính thức với giá trị sRGB do người dựng chọn.
Danh sách ảnh/tư liệu và quyền sử dụng nằm trong hồ sơ references/ của bộ asset.
Màn hình v003 dùng artwork và giao diện được tạo cho bản dựng này theo ảnh
người dùng cung cấp, không phải wallpaper hoặc screenshot chính thức của
Apple. Xem reports/v003/duo-screen-textures-manifest-v003.json,
reports/v003/duo-screens-final-visual-review-v003.md và
references/v003/wallpaper-generation.json. PNG master nằm trong source/v003/textures/.

Viewer đổi màu bằng tên semantic material, bỏ hậu tố duplicate như .001 hoặc _2.
Triples RGB được chuyển từ sRGB sang working space đúng một lần; màu gốc đọc từ
GLB được sao lưu/khôi phục trong working space. Viewer không tự sửa roughness,
metalness, opacity hoặc kính lens khi đổi màu thân máy.

Các snapshot preview/, preview-v002/, exports/v001/ và exports/v002/
được giữ nguyên. Bản cập nhật nằm ở preview-v003/ và exports/v003/.
