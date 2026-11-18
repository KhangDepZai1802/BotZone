# Storefront: ngôn ngữ, thông số, so sánh và tài khoản

Cập nhật ngày 13/11/2026 cho yêu cầu hoàn thiện trải nghiệm mua sắm. Website chạy local tại `http://localhost:3000/vi` và `/en`. Kiến trúc vẫn là Next.js storefront → ASP.NET Core API → PostgreSQL; quản trị là Windows Forms, EF Core vẫn là migration owner duy nhất. Đợt này không có migration, reset, triển khai cloud hoặc push remote.

## Dữ liệu kỹ thuật và bản dịch

`data/product-specifications.json` chứa 35 model, 609 mục VI/EN, 75 mã trường thống nhất. [Hồ sơ nguồn](catalogue-spec-sources.md) liên kết nguồn Apple cho từng model và giải thích những số liệu bị bỏ vì chưa xác nhận được. Cấu hình hãng hỗ trợ được tách khỏi SKU cửa hàng đang bán; không gán RAM, trọng lượng hoặc camera của thế hệ khác.

`ProductSpecificationImporter.cs` kiểm đủ35 model/tên/nguồn HTTPS Apple/nhãn song ngữ/ID/highlights trước khi ghi. Một transaction dùng kết nối EF hiện có và khóa các product; nội dung được thêm vào `products.specs.technicalSpecifications`, giữ mọi key trước đó. Audit lưu before/after. Mọi chỉnh sửa khác với technical content đã tồn tại đều được giữ bằng cách từ chối import. Kế hoạch và kết quả nằm trong `artifacts/specifications/`; chạy lại trả35 already-applied/0changed.

`CataloguePresentation.cs` đọc dữ liệu thực từ PostgreSQL và trả `technicalSpecs` đã chọn đúng ngôn ngữ. API không đưa `fixture`, `priceDisclosure`, `sourceCheckedAt` vào các dòng thông số công khai. `Variant.optionLabels` là nhãn hiển thị riêng: đổi ngôn ngữ không sửa raw `options`, mã SKU, ảnh theo màu, tồn kho hoặc snapshot hóa đơn. Tên màu Duo được hiển thị Star White/Night Sky ở EN, Trắng Ánh Sao/Trời Đêm ở VI. Nhãn nhóm, trạng thái, thanh toán, số tiền, ngày, aria-label và metadata trang cũng được rà.

`client.tsx` gửi `Accept-Language` theo route cho API/CSRF/upload; API trả thông báo lỗi tương ứng. `CustomerNotificationPresentation.cs` dịch template hệ thống đã biết, giữ nguyên ghi chú do khách/nhân viên nhập và lịch sử. Không tự dịch tên người, địa chỉ hay text nằm trong texture3D bàn giao. URL giữ filter/page/variant/tab hồ sơ khi đổi ngôn ngữ. Auth form giữ bản nháp không chứa mật khẩu qua thao tác đổi ngôn ngữ cùng màn, trong bộ nhớ trang dùng một lần/tối đa30giây; không lưu thông tin này vào URL hay browser storage.

## So sánh và thêm vào giỏ

`product-compare.tsx` dùng stable field IDs để ghép các model theo nhóm, tô nhẹ các khác biệt, có chế độ chỉ xem khác biệt và liên kết nguồn. Giá/cấu hình đang bán là phần riêng. Giới hạn3 sản phẩm cùng nhóm được áp dụng cả khi thao tác và khi phục hồi localStorage; ID trùng/hỏng/không tồn tại được loại.

`shopping-feedback.tsx` tạo dock3slot có ảnh, tên, số lượng lựa chọn, bỏ từng sản phẩm, xóa tất cả và CTA mở bảng. Xóa tất cả xóa cả lựa chọn đã lưu; không bắt người dùng vào bảng để bỏ từng món. Trạng thái thêm/bỏ/đạt giới hạn được thông báo ngay, có chuyển động và aria-live. Trang so sánh có thao tác xóa riêng và vùng cuộn có nhãn/bàn phím.

Thêm giỏ dùng button loading→check, badge giỏ phản hồi và bảng xác nhận cạnh biểu tượng giỏ có ảnh/biến thể/số lượng/giá/CTA. Không khóa trang hoặc tự chuyển focus. Các lần nhấn liên tiếp được xếp hàng; khách đã đăng nhập chỉ nhận thông báo thành công sau API lưu giỏ thành công. Escape/nút đóng/đổi route đóng panel; hover/focus tạm dừng auto-dismiss. Reduced motion bỏ chuyển động. Toast thêm giỏ cũ không còn dùng cho thao tác này.

Tham khảo cấu trúc nhóm thông số của [Apple Compare](https://www.apple.com/iphone/compare/) và nguyên tắc phản hồi phù hợp ngữ cảnh của [Carbon Notifications](https://www.carbondesignsystem.com/building-blocks/core/components/notification/guidelines), áp dụng cùng typography/màu/spacing hiện có của BotZone.

## Đăng nhập và đăng ký

`auth-page.tsx`, `password-field.tsx` và CSS modules tạo màn riêng với header thương hiệu/về cửa hàng/đổi ngôn ngữ; không có catalogue hoặc footer bán hàng bao quanh. Password eye có accessible label/state, confirm password và lỗi cạnh trường/tóm tắt focusable. Luồng forgot/verify/reset/change vẫn dùng Identity/CSRF/Mailpit thật. Return URL chỉ cho phép đường dẫn nội bộ hợp lệ.

`CustomerAuthentication.cs` mở rộng endpoint hiện có. Đăng ký lưu tên/email/số điện thoại trong Identity và `customer_profiles`; nếu gửi địa chỉ đầy đủ thì thêm địa chỉ mặc định trong cùng transaction. Số điện thoại bắt buộc ở form mới; API giữ tương thích client cũ không gửi phone. Địa chỉ đăng ký tùy chọn; checkout vẫn cần địa chỉ giao hàng. Public registration luôn customer, từ chối role tự cấp.

“Ghi nhớ đăng nhập” tạo cookie HttpOnly có expiry cùng session server, tối đa8giờ, không sliding và không lưu mật khẩu. Không tick thì dùng session cookie; WinForms luôn không persistent dù payload gửi rememberMe. Logout/reset/change/revocation vẫn kiểm bằng session DB/security stamp. [ASP.NET cookie documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0) giải thích `IsPersistent` và `ExpiresUtc` được dùng ở đây.

## Kiểm chứng

- [Specification/API39 checks](../artifacts/specifications/live-results.json):35model609fields, detail/list, VI/EN, options raw không đổi, source, mã lỗi và toàn bộ giá/tồn/ID/settings/ảnh được bảo toàn. [Import plan](../artifacts/specifications/import-plan.json), [applied](../artifacts/specifications/import-applied.json), [idempotence](../artifacts/specifications/import-after.json).
- [Compare/bag](../artifacts/web/compare-bag/results.json) và [14 layout combinations](../artifacts/web/compare-bag/layout-results.json): actual Edge, guest-only/main không command nghiệp vụ. Có ảnh mobile/desktop, table rows, dock và bag confirmation.
- [Auth UI](../artifacts/web/auth-ui/README.md): đăng ký thật, profile/address persistence, mail verify/reset EN, cookies, hai phiên độc lập bị thu hồi, role guard và WinForms nonpersistent trên clone. Các test này dùng cùng binary/API ở5091 và cùng Next production ở3001, không phải backend triển khai thứ hai.
- [137 kiểm tra ngôn ngữ đạt](../artifacts/localization/acceptance.json): đủ 35 model ở hai viewport, public routes và tài khoản/dịch vụ/đơn/hóa đơn. [23 kiểm tra bổ sung](../artifacts/web/commerce-locale-regression/README.md) xác nhận auth drafts, layout hẹp và snapshot item không đổi. Lượt phát hiện lỗi giữ riêng; chỉ report final sau sửa được dùng nghiệm thu.
- [Clip thao tác Edge thực](../artifacts/web/compare-bag/shopping-feedback.webm) dài 7,8 giây, ghi hai thao tác chọn so sánh và hai lần thêm giỏ với motion bật. Không có API mutation trong lượt quay.
- 15/15 unit tests (giữ 10 filter cũ), 26 smoke checks và 39 kiểm tra ảnh đạt; typecheck, Next production build và toàn solution .NET Release đạt, .NET 0 warning/0 error. [Ma trận và giới hạn](test-evidence.md) ghi rõ từng phạm vi.
- [Golden G01…G08](../artifacts/specifications/golden-preserved.json) vẫn 97.130.000 − 3.500.000 = 93.630.000 VND. Browser mobile là giả lập Edge trên Windows, không phải kiểm thiết bị điện thoại vật lý. Các trạng thái “chưa/một phần” của BA không tự đổi thành pass vì đợt UI này.

Lệnh kiểm tra phù hợp: `npm.cmd run typecheck`, `npm.cmd test`, `npm.cmd run build`, `node tests/api/specifications-live.mjs`, `node tests/web/compare-bag-live.mjs`. Auth mutation suite chỉ bật bằng `BOTZONE_AUTH_CLONE=1` với test web 3001; không chạy mutation suite trên main. Hai helper QA 3001/5091 đã đóng đúng PID/path/thời điểm tạo, record tại `artifacts/local-runtime/auth-ux-*.json`; main API 5080 và Next production 3000 vẫn chạy.

## Tinh chỉnh bố cục laptop và phong cách sáng

Khám phá 3D dùng chiều cao cửa sổ và hàng điều khiển gọn; hai nút mặt trong/ngoài thành một nút chuyển mặt. Nút đặt lại luôn nằm trong khung laptop đã kiểm ở năm kích thước từ 1024×600 đến 1440×900, cho cả hai máy. Khóa zoom, chuyển nền theo màu, slider gập và bàn phím vẫn hoạt động. [Bằng chứng 3D](../artifacts/3d-v003/laptop-refinement/README.md).

Bảng so sánh dùng đầu sản phẩm cố định gọn khoảng 140px trên desktop. Bản hiện hành dùng trắng–đen và xám trung tính, thay nền champagne của lượt tinh chỉnh đầu. `specification-engine.ts` đối chiếu CPU/GPU, bộ nhớ, màn hình, kết nối, trọng lượng/kích thước và pin/sạc theo từng quy tắc cùng đơn vị/điều kiện; ô nhấn có lý do mở rộng được bằng bàn phím hoặc chạm. Thế hệ chip mới hơn được ghi đúng là thế hệ, không suy ra hiệu năng. Thiếu một giá trị thì không chọn ô vượt trội; toàn bộ bằng nhau không có ô nhấn, nhiều ô đồng hạng cao nhất được nhấn nếu còn ô thấp hơn. [Quy tắc và giới hạn hiện hành](product-comparison.md) thay phạm vi chỉ dung lượng/RAM/Hz của lượt đầu.

Dung lượng đang bán lấy raw options của SKU active (có thể đang tạm hết hàng), tách khỏi các lựa chọn tối đa hãng công bố. Parser phân biệt GB/TB, GB với Gb, danh sách/range và điều kiện RAM gắn với SSD. Ví dụ `12 GB ở 256/512 GB; 16 GB ở 1/2 TB` được hiểu RAM tối đa 16GB. [Audit dữ liệu](../artifacts/specifications-research/comparison-audit.md) và 5 nhóm unit mới kiểm các trường hợp này; hai edge case Hz đảo thứ tự và bit/byte được sửa sau audit. Tổng unit suite 20/20 đạt, giữ đủ 10 filter cũ.

Đăng nhập/đăng ký đổi sang gradient champagne–rose–lilac sáng, chữ tối và form trắng trong nhẹ; bỏ khối tối lớn. Gradient liên tục đến chân trang. Chỉ CSS thay đổi, không đổi auth, thông tin đăng ký, draft, cookie hay thời hạn phiên. [12 kiểm tra giao diện VI/EN](../artifacts/web/refinement-auth/README.md) đạt, không lỗi JavaScript hay request ghi dữ liệu; đã xem ảnh desktop/mobile.

[11 nhóm kiểm tra so sánh cuối](../artifacts/web/refinement-compare/README.md) đạt: 8 tổ hợp VI/EN/viewport và các luồng lựa chọn, highlight, keyboard. Header desktop 140px/mobile 146px; laptop 1366×768 nhìn được 7 dòng dữ liệu đầy đủ khi vùng bảng nằm trong màn hình, kể cả ba sản phẩm. Lỗi tràn ngang mobile do mô tả accessible đã được sửa; báo cáo ban đầu giữ riêng. Build production cuối thành công và tiếp tục phục vụ tại cổng 3000, API 5080 giữ nguyên.

## Cập nhật đăng ký và điều khiển 3D — 17/11/2026

Mật khẩu mới có tối thiểu6 ký tự, không bắt buộc phối hợp chữ hoa/chữ thường/số/ký hiệu. Frontend, lời nhắc/lỗi VI/EN và ASP.NET Identity cùng áp dụng cho đăng ký, đổi, reset; mật khẩu/hash cũ không bị đổi. [Kiểm thử auth thực trên clone](../artifacts/web/password-policy-six/README.md) xác nhận đăng ký sáu số, đổi sáu chữ thường lặp, reset sáu chữ hoa, từ chối dưới6 và giữ session/token/role guards.

Tùy chọn lưu địa chỉ giao hàng mở/thu bằng grid transition320ms, fade220ms và dịch nhẹ6px. Trường vẫn giữ dữ liệu khi thu lại nhưng bị disabled/inert/aria-hidden ngay khi đóng, không còn trong thứ tựTab hay validation bắt buộc. Checkbox có aria-expanded/aria-controls và trả focus về checkbox nếu vùng đang focus bị đóng. Reduced motion tắt transition. [Báo cáo kiểm chuyển động](../artifacts/web/auth-address-motion-20261117/results.json) lưu riêng so với lượt auth mutation trước đó.

Modal3D gộp nhãn/giá trị độ mở và góc xoay trên một dòng; hai thanh kéo44px cùng baseline, bố cục desktop/mobile căn lại theo nhóm. [16 kiểm tra](../artifacts/web/scene-controls-20261117/results.json) đạt. Không sửa hình/model/hero pointer/camera, không bật zoom lại.
