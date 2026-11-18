# BotZone 3D v003 — Duo theo ảnh tham chiếu

Duo dùng màn khóa phong cảnh trên cả màn ngoài và màn trong: cồn cát xám be, núi, trời xanh nhạt, ngày `Wed Apr 1`, đồng hồ cao hẹp `9:41` và hai nút nhỏ ở góc dưới phải. Khi mở máy, màn ngoài chuyển sang kính tối; khi gập về trạng thái đóng, màn ngoài sáng lại. Màn trong không có lưới ứng dụng hoặc widget.

Đã cập nhật model Blender, GLB desktop/mobile, texture và viewer. Bộ Pro Max được mang sang nguyên vẹn từ v002 để dùng cùng gói. Phạm vi và bằng chứng kiểm tra cuối nằm trong `reports/v003/QA_REPORT.md` và `asset-manifest.json`.

## Mở xem

1. Giải nén toàn bộ gói.
2. Trong `preview-v003`, mở `BotZone_3D_Preview_v003.html` bằng Chrome hoặc Edge có WebGL 2.
3. Chọn Duo. Preview mặc định mở máy và nhìn vào màn khóa bên trong.
4. Bấm **Màn ngoài** để đóng máy; bấm **Màn trong** để mở. Có thể dùng thanh gập và xoay ra phía màn ngoài để thấy màn nhỏ đã tắt.

HTML tự chứa đã nhúng thư viện và model; không cần cài Node/Python để mở. Khi sửa GLB bên ngoài, cần build lại HTML. Hướng dẫn phát triển nằm trong `preview-v003/README.md`.

## Nội dung

| Thư mục / file | Công dụng |
| --- | --- |
| `exports/v003/models/` | Bốn GLB: Pro Max và Duo, desktop/mobile |
| `exports/v003/textures/` | Texture màn khóa; Pro Max có thêm hình nền không UI |
| `exports/v003/posters/` | Ảnh xem từ model bàn giao |
| `source/v003/` | Hai master Blender, wallpaper và PNG màn hình |
| `scripts/build_duo_v003.py` | Dựng lại Duo và animation tắt màn ngoài |
| `scripts/compose_duo_screens_v003.py` | Dựng lại texture màn khóa Duo |
| `preview-v003/` | Viewer tự chứa, mã nguồn và báo cáo kiểm |
| `HANDOFF_CODEX.md` | Contract tích hợp cho Codex |

## Cách tắt màn ngoài

Hành vi này nằm trong chính clip `Duo_OpenClose` của GLB. Màn ngoài sáng ở trạng thái đóng. Khi mở qua khung hình đầu tiên của clip (khoảng 3°), lớp ảnh màn hình ẩn tức thời và để lộ mặt kính tối bên dưới. Scrub ngược về đóng sẽ bật lại. Viewer dùng toàn bộ clip, không dùng một animation riêng để giả lập việc tắt màn.

## Dựng lại trên Windows

Chạy từ thư mục gốc của gói, trong cửa sổ PowerShell riêng. Các script Blender khởi tạo scene rỗng nên không chạy trong file Blender đang chỉnh chưa lưu.

```powershell
py -3 -m pip install -r scripts/requirements-screens-v003.txt
py -3 scripts/compose_duo_screens_v003.py
blender --background --factory-startup --threads 6 --python scripts/build_duo_v003.py
blender --background --factory-startup --threads 6 --python scripts/build_duo_v003.py -- --mobile
```

Nếu chưa có lệnh `blender`, dùng đường dẫn đầy đủ tới `blender.exe`. CairoSVG cần Cairo của hệ thống; ảnh và model đã xuất sẵn nên không cần cài các dependency này để xem. Dùng Blender 4.3.2 để tái tạo cùng pipeline, hoặc kiểm lại export nếu dùng phiên bản khác.

## Chất lượng và phạm vi

Ảnh mới được tái dựng theo ảnh người dùng cung cấp. Wallpaper gốc có kích thước 1496 × 1051; phần đồng hồ, ngày và biểu tượng được ghép riêng ở độ phân giải atlas cao hơn để giữ nét. Không mô tả việc phóng atlas thành wallpaper native 4K.

Đồng hồ/ngày và các nút trên màn là texture minh họa tĩnh. Bật/tắt màn ngoài là animation của model. Đây là model dựng từ reference, không phải CAD sản xuất hoặc bản quét sản phẩm. Các ảnh kiểm được render từ GLB thực tế; không suy ra FPS hay độ khớp hình ảnh WebGL từ kiểm cấu trúc.

Browser/thiết bị thật chưa được kiểm trong môi trường dựng do giới hạn truy cập đã ghi trong báo cáo. Cần kiểm trực tiếp trên máy triển khai trước khi demo web.
