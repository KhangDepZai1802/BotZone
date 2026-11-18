# Tiến độ triển khai BotZone v1.1

Cập nhật 13/11/2026. Đã tiếp tục triển khai theo `BotZone_BA_v1.1_WinForms.md`; không còn trạng thái chờ đổi kiến trúc. Hồ sơ trước chuyển đổi được giữ tại `docs/checkpoints/implementation-status-before-v1.1.md`. Chi tiết quyết định: `docs/architecture-change-winforms.md`.

## Kiến trúc và dữ liệu

- Next.js/TypeScript chỉ storefront/proxy API. Toàn bộ quản trị là WinForms native .NET 10. ASP.NET Core API sở hữu auth, nghiệp vụ, query/export và worker; một PostgreSQL Docker, một migration owner EF Core/Npgsql.
- Hai SQL migration cũ giữ nguyên checksum. Năm migration EF đã kiểm clone rồi áp dụng main. Không InitialCreate lên bảng hiện hữu, không copy password hash/session Better Auth sang Identity.
- Main `botzone`: 100 bảng, seed golden + 35 model/37 SKU/chín nhóm. Golden đúng **97.130.000 − 3.500.000 = 93.630.000 VND**. NV03 inactive vẫn có lịch sử. D01 draft không cộng issued; PAG75 chỉ ở test clone.
- Fresh `botzone_integration_test_fresh_20261113` chạy cùng EF và seed hai lần; không nhân dữ liệu. Test clone `botzone_baseline_test_20261113135743_c0d401c6` chứa cả fixture/browser/concurrency, nên tổng toàn clone lớn hơn golden.
- Restore `botzone_restore_20261113153420_6d0c9293` khớp fingerprints/counts 100 bảng, sequences, golden và SHA256 ba media có bản ghi DB.
- Backup gốc: `backups/botzone-20261113-135647-8e85499a`; clone sau nghiệp vụ/media: `backups/botzone-20261113-153319-9685ef00`; main sau seed: `backups/botzone-20261113-153323-0bfe29ae`.
- Không reset Git/drop DB/xóa volume/xóa hàng loạt source, không deploy/push. Workspace không có Git metadata tại kiểm kê đầu. SQL/TS legacy giữ để truy vết, runner TS migration từ chối chạy.

## Mã đã triển khai

| Phần | Phạm vi thực tế |
|---|---|
| Auth | Identity hash/token, verify/reset Mailpit, cookie/session DB, CSRF hai client, revoke/current role/ownership. |
| Catalogue/kho | Category/model/SKU, VI/EN, compatibility, suppliers/PO/nhận một phần, serial/lô/quarantine, kiểm kê/owner duyệt. |
| Thương mại | Giỏ DB, voucher version/quota, quote server, điểm/credit snapshot, giữ tồn, checkout, COD/banking/MoMo/Visa demo, idempotency, confirm/handoff/invoice/shipment/reconciliation. |
| Hóa đơn | C# query dùng lại, `List<InvoiceListItemDto>`, scope + AST whitelist, pagination/matched total/detail; báo cáo tháng/quý/năm/staff/model/SKU; sáu báo cáo vận hành riêng. |
| Export | ClosedXML/PDFsharp-MigraDoc, font local, một dataset RepeatableRead cho cả XLSX/PDF, manifest/checksum/private download/expiry, xuất toàn bộ filter. |
| Hậu mãi | Trả từng unit/kiểm tra/CN/hoàn đúng nguồn, failed delivery, thu cũ quote/duyệt/khách quyết định/credit/freight, Care serial/quantity/repair/replace/refund. |
| Native admin | Login/Main, 21 module, BindingSource/DataGridView, native command forms/filter/report, async/cancellation, bốn role, CMS TreeView/VI–EN/asset/preset/history. |
| Storefront | Landing/catalogue/biến thể/compare/wishlist/cart/checkout/account/order/invoice/trade-in/Care/policies; VI/EN, VND/USD; checkout VND. |
| Media/CMS/3D | Ảnh/GLB validation, private ownership, revision/publish/restore immutable; preview grant một lần/snapshot/revoke; phone/duo GLB/explore/fold/reset/fallback. |
| Local/tooling | EF migrate/seed, native build/publish, API/web host + Docker DB/Mailpit, backup/restore DB mới, README/ERD/data dictionary/query catalog. |

## Bằng chứng và phạm vi đã quan sát

- Checkpoint C# integration **73/73**, DB thật: golden/guards/concurrency/financial snapshots/export writer overlap/dispatcher payment. Kết quả mới nhất: `tests/BotZone.IntegrationTests/last-result.json`.
- Bộ filter TypeScript cũ **10/10**. Next typecheck và production build đạt; lỗi viewer trước đây đã sửa.
- WinForms chạy **thực trên Windows**, 1366×768/1920×1080 tại DPI120/125%: login/list/detail/filter/report/export, 21 modules/sáu operational reports, CRUD native13 assertions và inventory API13 assertions. Build/publish native đạt.
- Luồng duy nhất web→WinForms→web đã đạt: order `031a182a-59ba-4578-837b-1fa62df6fa33` được nv01 confirm và kho handoff bằng native forms; browser khách mở invoice `718486e2-9f26-4a5a-a412-ad3fbdd76ef2` / `BZ-HD-00000042`. Evidence `artifacts/web/native-handoff-results.json` và `artifacts/winforms/native-handoff-second/smoke.json`.
- Full solution Release + locked restore đạt **0 warning/0 error** trên Windows; auth8 HTTP và cart/voucher sau API process restart đều đạt.
- Browser thật: COD checkout→invoice→đối soát, khách tải PDF/XLSX, return private evidence, trade-in chấp nhận→credit; CMS publish/restore/preview/revoke; VI/EN bảy viewport BA, touch emulation/reduced motion/WebGL fallback.
- Auth HTTP/Mailpit tám ca; migration/seed chạy lặp; fresh DB; restore toàn bộ dữ liệu và media. Bằng chứng: `docs/database/main-seed-verification.json`, `fresh-seed-verification.json`, `restore-verification.json`.
- Đã xem ảnh native/web và render PDF để kiểm bố cục. Phát hiện tổng bị đẩy thành trang riêng, đã sửa và kiểm đủ 11 trang của list/report/detail: tổng đi cùng dòng dữ liệu cuối, không chữ vượt biên, filter dễ đọc.

`docs/test-evidence.md` là ma trận từng mã DB-01…DB-07, AC-01…AC-16, DB-T01…DB-T20, WF-01…WF-08 và liên kết artifact. Mở module không được coi là đã chạy mọi nhánh mutation. Tổng clone có fixture khác phải lọc G01…G08 khi đối chiếu BA.

## Giới hạn còn phải ghi rõ

Chưa coi toàn bộ BA đã pass. Chưa nghiệm thu thủ công DPI100%/150%, nhiều màn hình, bàn phím vật lý, SaveFileDialog save/cancel, lỗi mạng/session trên native và demo khi tắt Internet. Không dùng build Linux, scale ảnh hoặc code kiểm focus để thay bằng chứng tương tác thật.

Khang đã bàn giao bộ Blender **v003** từ session khác và yêu cầu tích hợp. Bốn GLB/hai poster đã được sao chép, đối chiếu hash và đưa vào renderer/CMS hiện tại; source gốc không sửa. Chi tiết và bằng chứng WebGL cập nhật tại [tích hợp v003](3d/v003/integration.md). Source fixture mới sửa loyalty maturity, còn ledger append-only của clone seed cũ được giữ nguyên và không dùng làm bằng chứng chính sách mới.

Start→stop→restart thực bằng script đã đạt trên main; native trước/sau đều 8 issued/97.130.000 VND; DB/Mailpit giữ nguyên container/volume. API5080 và web3000 đang chạy bằng PID được script quản lý. Evidence `artifacts/local-runtime/lifecycle-result.json`, `artifacts/winforms/main-lifecycle-after-restart/smoke.json`.

Chạy/build/tài khoản trong `README.md`; demo trong `docs/demo-script.md`; truy vết SQL→List→WinForms→file trong `docs/database/rubric-mapping.md`.

Bổ sung cuối: module Khách hàng có điểm/hạng/credit/số đơn trên list và API detail đọc nhất quán với lịch sử;5ca HTTP quyền/dữ liệu đã đạt; bản native publish đã mở đủ8tab cho owner/nv01, BindingSource và dữ liệu thật, evidence `artifacts/winforms/customer-details-published/smoke.json`. Startup tracking V2 đã kiểm thực loại11 process không thuộc BotZone do Windows tái sử dụng PID; không dừng ứng dụng khác. Evidence trong `docs/test-evidence.md`.

## Cập nhật ảnh và tương tác storefront

Đã nhập 37 ảnh Apple cho 35 model, có nguồn và ánh xạ đúng thế hệ/màu; C# importer chỉ đổi poster và ghi audit. Đối chiếu API giữ nguyên ID, biến thể, giá, tồn kho, thông số và tương thích. Kiểm tra ảnh qua Edge/API đạt 39 mục; golden sau cập nhật vẫn đúng **97.130.000 − 3.500.000 = 93.630.000 VND** (`artifacts/product-photos/`).

Dropdown Radix dùng chung; catalogue phân trang 6/mobile và 12/desktop với URL/history/filter/locale; motion cho route/card/button/menu/dialog và reduced motion. Đã chạy 13 interaction checks, Select fixture 390/1440, catalogue regression và 26 smoke checks. Bản native CMS thêm lựa chọn model/poster v003 đã build/publish trên Windows; không dùng kết quả build này để tuyên bố đã kiểm tương tác mới trên WinForms.

## Nghiệm thu landing v003 trên production local

Edge/WebGL thật trên Windows/Intel Iris Xe đạt **20/20 ca**, 19 ảnh canvas có kiểm pixel và 47 ảnh chụp. Đã xem trực tiếp tổng thể mobile/laptop, Pro Max, Duo đóng/mở một phần/mở 100% và kính ngoài tắt; kiểm đủ bốn track, giữ cuối clip và năm vòng đảo chiều cho cả hai bản GLB. VI/EN ở 390/430/1366/1440px; lazy, chọn chất lượng trước tải, touch scroll, giảm chuyển động, fallback khi tải lỗi/mất WebGL và vòng đời scene đã kiểm. [Báo cáo cuối](../artifacts/3d-v003/final/README.md) phân biệt đầy đủ ca thực, mô phỏng visibility và giới hạn thiết bị.

Quan sát ngắn trên host: Pro Max khoảng60FPS, RAF median16,7ms/p95 16,8ms; fetch/hash/parse setup122–373ms. Không coi giả lập viewport là kiểm điện thoại thật. Còn đường nếp gập Duo rất mảnh ở vài góc; giữ nguyên source/model/UV/PBR. Favicon404 riêng đã sửa, rebuild production và kiểm Edge200/không lỗi console. Bộ ảnh39 mục được chạy lại trên production cuối; dữ liệu catalogue giữ nguyên ngoài poster/metadata ảnh. Tài liệu tích hợp, nguồn ảnh, README, ERD và data dictionary đã cập nhật; không có migration mới trong đợt này.

## Hoàn thiện VI/EN, thông số, so sánh, giỏ và tài khoản — 13/11/2026

Đã bổ sung 609 mục thông số song ngữ cho đủ 35 model từ nguồn Apple, dùng mã trường thống nhất cho chi tiết và so sánh. C# importer thêm nội dung vào JSON hiện có, giữ metadata trước đó, ghi audit và chạy lại no-op; không tạo migration. Giá, tồn kho, SKU, ảnh, settings và snapshot hóa đơn được đối chiếu bảo toàn. [Nguồn và những số liệu chưa xác nhận](catalogue-spec-sources.md).

Dock so sánh có ảnh, phản hồi ngay, giới hạn 3 sản phẩm, bỏ từng món hoặc xóa tất cả; bảng chia nhóm thông số, đánh dấu/lọc khác biệt và hỗ trợ cuộn bàn phím. Thêm giỏ có trạng thái loading/check, badge và mini bag có ảnh; [clip thao tác thực](../artifacts/web/compare-bag/shopping-feedback.webm) ghi lại chuyển động. Các hiệu ứng tôn trọng reduced motion.

Đăng nhập/đăng ký dùng màn riêng, password eye, xác nhận mật khẩu, điện thoại và địa chỉ giao hàng tùy chọn. Ghi nhớ đăng nhập dùng cookie HttpOnly trong giới hạn session 8 giờ của BA, không lưu mật khẩu; WinForms vẫn không persistent. VI/EN bao phủ thông số, màu, trạng thái, lỗi API, thông báo hệ thống và các luồng đã kiểm; dữ liệu do khách/nhân viên nhập được giữ nguyên. Đổi ngôn ngữ giữ lựa chọn sản phẩm, query, tab hồ sơ và bản nháp auth không chứa mật khẩu.

Nghiệm thu cuối: 137 kiểm tra ngôn ngữ; 39 kiểm tra API/thông số; 4 nhóm compare/bag + 14 bố cục; 9 nhóm auth thật trên clone; 23 kiểm tra bổ sung auth draft/đơn/hóa đơn; 39 kiểm tra ảnh và 26 smoke checks đạt. Unit tests 15/15 giữ đủ 10 kiểm thử filter cũ. Typecheck, Next production build và toàn solution .NET Release đạt; .NET 0 warning/0 error. Đã xem ảnh Edge desktop/mobile, không coi giả lập viewport là kiểm điện thoại vật lý. [Bằng chứng chi tiết](storefront-ux-localization.md).

API chính 5080 và Next production 3000 tiếp tục chạy; hai helper QA 5091/3001 đã đóng sau khi xác minh PID, executable và thời điểm tạo. Golden vẫn **97.130.000 − 3.500.000 = 93.630.000 VND**. Các giới hạn nghiệm thu WinForms/BA ở trên giữ nguyên; đợt storefront này không thay thế chúng.

Điều chỉnh khám phá 3D tiếp theo: khóa wheel/pinch zoom, giữ camera theo viewport; Pro Max Bạc/Băng Thanh và Duo Trắng Ánh Sao chuyển nền tối với fade 650ms. Bỏ nút pause và ba shortcut gập 0/50/100%, giữ thanh kéo gập/xoay/reset. Đã kiểm Edge/WebGL hai máy ở 390/1440px, fade thực/reduced motion, radius camera và hồi quy animation; typecheck/build đạt. [Bằng chứng](../artifacts/3d-v003/exploration-controls/README.md). Model gốc và dữ liệu không sửa.

## Tinh chỉnh laptop, so sánh và nền tài khoản

Modal 3D co theo chiều cao cửa sổ, một nút chuyển mặt thay hai nút trong/ngoài; cả hai máy đã kiểm không cuộn/khuất điều khiển trên 5 viewport laptop từ1024×600 đến1440×900. [Kết quả 3D](../artifacts/3d-v003/laptop-refinement/README.md) giữ kiểm khóa zoom/fade/slider/reset và ghi rõ phép đo camera sau khi ổn định.

Bảng so sánh chuyển sang màu trung tính ấm, header cố định gọn140px, nhìn được7 dòng dữ liệu ở1366×768; giá trị dung lượng/RAM/Hz cao hơn có thể đối chiếu được in đậm và tô nhấn. Cấu hình đang bán và mức tối đa hãng hỗ trợ tách riêng; thiếu dữ liệu không coi là0. [11 nhóm QA](../artifacts/web/refinement-compare/README.md) đạt; mobile overflow đã sửa. Auth dùng gradient sáng champagne–rose–lilac liền mạch, bỏ khối đen lớn; [12 QA VI/EN](../artifacts/web/refinement-auth/README.md) đạt, chỉ CSS thay đổi. Unit20/20, typecheck và Next production build đạt. Không thay backend, schema, dữ liệu hoặc assets3D; các giới hạn BA trước đó vẫn giữ nguyên.

## So sánh chi tiết và phong cách trắng–đen — 13/11/2026

Đã thay phạm vi so sánh hẹp của lượt trên bằng engine nhận diện đơn vị, khoảng cấu hình và điều kiện cho CPU/GPU, RAM/lưu trữ, màn hình, kết nối, trọng lượng/kích thước, pin/sạc và tính năng xác định. Các ưu điểm có lý do VI/EN mở được bằng bàn phím/chạm; thiếu dữ liệu, phạm vi khác nhau, giá trị bằng nhau và đánh đổi được xử lý riêng. Bảng dùng trắng/đen/xám, bỏ nhấn nâu/champagne. [Quy tắc và ví dụ iPad](product-comparison.md) phân biệt số lõi với hiệu năng, RAM hãng tối đa với cấu hình cửa hàng, HDR với độ sáng toàn màn hình.

42/42 unit đạt, gồm 22 nhóm mới; corpus 28 trường kiểm 1.397 cặp và 8.806 bộ ba cùng hoán vị/VI/EN. Đây là kiểm thuật toán, không phải số ca trình duyệt. [12/12 ca Edge cuối](../artifacts/web/semantic-compare/README.md) đạt cho Air M3/M4, Air M4/Pro M5 và ba iPad, cả VI/EN ở390×844 và1366×768. Header cố định140/146px; laptop thấy8–10 dòng dữ liệu sau khi bỏ khoảng cuộn dư. Đã xem ảnh thực hai/ba sản phẩm và mobile; không lỗi browser, không API ghi dữ liệu trong lượt QA.

Typecheck và Next production build cuối đạt, web3000 chạy bản mới, API5080 được tái sử dụng. Không đổi backend/DB/schema, dữ liệu thông số, auth hoặc3D; không chạy lại toàn bộ kiểm thử nghiệp vụ và không nâng trạng thái nghiệm thu BA còn mở. Lượt browser đầu chưa đạt mật độ hiển thị được giữ riêng trong artifacts, không dùng làm kết quả pass.

## Tăng tương tác con trỏ ở hero — 14/11/2026

Pro Max và Duo ở landing nghiêng/xoay/dịch chuyển rõ hơn theo chuột trên toàn vùng hero, có damping theo thời gian và trở về tâm khi rời vùng. Biên độ yaw tối đa khoảng12,6° mỗi phía; camera/default zoom không đổi. Chạm trên mobile, reduced motion và modal khám phá giữ cơ chế riêng; không sửa asset, rig, texture hoặc dữ liệu CMS.

[7/7 nhóm Edge/WebGL đạt](../artifacts/3d-v003/hero-pointer/README.md): hai máy ở1366×768/1920×1080, hình dựng không chạm mép canvas, easing/return, modal drag và khóa zoom, cuộn cảm ứng390px và reduced motion. Đã xem ảnh hai đầu chuyển động cùng ảnh Duo; có clip thực. TypeScript và Next production build đạt, web3000 chạy bản mới. Không lỗi page hoặc API mutation trong lượt kiểm. Không chạy lại bộ kiểm thử nghiệp vụ/WinForms trong đợt chuyển động này.

## Căn hàng điều khiển 3D và mật khẩu sáu ký tự — 17/11/2026

Nhãn và giá trị của độ mở/góc xoay nằm cùng hàng, hai thanh kéo có cùng chiều cao44px. Nhóm màu, slider và nút thẳng hàng trên desktop; mobile chia hàng có chủ đích trong modal vừa viewport. [16 kiểm tra Edge/WebGL](../artifacts/web/scene-controls-20261117/results.json) đạt trên VI/EN, Duo và Pro Max: không cắt/overflow/cuộn modal, gập, xoay, màu, đổi mặt, reset, Escape/focus và khóa zoom hoạt động. Đã xem ảnh desktop/mobile thực.

Theo yêu cầu mới, đăng ký/đổi/reset mật khẩu tối thiểu6 ký tự, bỏ bắt buộc chữ hoa/chữ thường/số/ký hiệu ở cả frontend và ASP.NET Identity. [Kiểm thử auth trên clone](../artifacts/web/password-policy-six/README.md) đạt: đăng ký sáu số, đổi sang sáu chữ thường lặp và reset sang sáu chữ hoa; trường hợp dưới6 bị từ chối. Hash/tài khoản chính không thay, không migration/seed.42 unit đạt; .NET build0warning/error, Next build/TypeScript đạt. Hai helper QA đã đóng, main API5080/web3000 giữ chạy.

Bổ sung cùng ngày: checkbox lưu địa chỉ giao hàng mở/thu mềm320ms, fade220ms; giữ giá trị nhập và xử lý bật/tắt liên tiếp. Khi đóng, nội dung không để khoảng trống, bị disabled/inert/aria-hidden và không tham gia validation/Tab. [5/5 kiểm tra chuyển động](../artifacts/web/auth-address-motion-20261117/results.json) đạt trên VI/EN390/1366 và reduced motion: có frame trung gian thực khi bật motion, không có khi giảm motion; focus/autofill/validation và mật khẩu6 ký tự giữ đúng. Next build/TypeScript cuối đạt, main web phục vụ bản mới;0page error/0APIwrite trong lượt animation.
