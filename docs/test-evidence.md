# Bằng chứng kiểm thử BotZone BA v1.1 WinForms

Cập nhật ngày 13/11/2026. Đây là đối chiếu bằng chứng thực tế, không phải tuyên bố toàn bộ BA đã nghiệm thu. **73/73 ca C# đạt** trên PostgreSQL clone tại lượt 08:43:58–08:44:16 UTC (15:44 giờ Việt Nam). Kết quả máy đọc được: [last-result.json](../tests/BotZone.IntegrationTests/last-result.json). Mỗi lần chạy mới thay tệp kết quả; thời điểm trong JSON là nguồn xác nhận.

## Môi trường và cách chạy lại

- Clone: `botzone_baseline_test_20261113135743_c0d401c6`, PostgreSQL tại `127.0.0.1:54329`, runtime role `botzone_app`.
- .NET 10 console runner dùng EF/Npgsql và service nghiệp vụ thật. Không dùng EF InMemory, SQLite hoặc mock database. Runner chỉ chấp nhận tên database có prefix test và không tự migrate/reset/drop.
- Clone đã áp dụng chuỗi EF đến `20261113080440_IdentityNamesAndFinancialHistory`. Baseline legacy được đối chiếu trong [baseline-clone-verification.json](database/baseline-clone-verification.json); kết quả baseline không chứng minh backup/restore nghiệp vụ và media.
- Test hậu mãi và thay đổi point policy dùng transaction rollback, ép `SET CONSTRAINTS ALL IMMEDIATE` trước rollback. Test đồng thời dùng connection/DbContext riêng, ghi fixture có mã `CONC-*`, hủy đơn qua command để trả reservation và khóa SKU fixture. Không xóa chứng từ/ledger để dọn test. Test export snapshot chỉ thay JSON của draft D01 rồi phục hồi trong `finally`; không sửa hóa đơn issued.

```powershell
dotnet restore tests/BotZone.IntegrationTests/BotZone.IntegrationTests.csproj --ignore-failed-sources
dotnet build tests/BotZone.IntegrationTests/BotZone.IntegrationTests.csproj --no-restore
dotnet tests/BotZone.IntegrationTests/bin/Debug/net10.0/BotZone.IntegrationTests.dll
```

`BOTZONE_TEST_DATABASE` chọn clone, `BOTZONE_TEST_CONNECTION` chọn connection string. Lượt cuối build integration project cùng các project reference: 0 warning, 0 error; runner exit 0. Đây không thay thế full solution build. Xem [README của runner](../tests/BotZone.IntegrationTests/README.md) về seed và tài khoản demo local.

## Nguồn bằng chứng

| Ký hiệu | Bằng chứng | Phạm vi |
|---|---|---|
| I | [Program.cs](../tests/BotZone.IntegrationTests/Program.cs), [73 kết quả](../tests/BotZone.IntegrationTests/last-result.json) | Seed idempotent/Identity hash, 10 ca filter tương ứng TS, golden, list/detail/report/export và quyền |
| G | [DatabaseGuardChecks.cs](../tests/BotZone.IntegrationTests/DatabaseGuardChecks.cs) | 15 ca SQL bất hợp lệ bị guard/constraint từ chối, mỗi ca rollback |
| A | [AfterSalesChecks.cs](../tests/BotZone.IntegrationTests/AfterSalesChecks.cs) | Thu cũ, trả/hoàn, Care serial/quantity, giao thất bại; checkout thật; policy 2.000 VND/điểm |
| C | [ConcurrencyChecks.cs](../tests/BotZone.IntegrationTests/ConcurrencyChecks.cs) | Hai DbContext/connection cùng tranh tài nguyên qua CommandService thật |
| S | [InvoiceSnapshotChecks.cs](../tests/BotZone.IntegrationTests/InvoiceSnapshotChecks.cs), [export-snapshot-result.json](../tests/BotZone.IntegrationTests/export-snapshot-result.json) | Writer commit thật trong lúc export giữ RepeatableRead, XLSX/PDF và manifest |
| Q | [query-plans.json](../tests/BotZone.IntegrationTests/query-plans.json), [query catalog](query-catalog.md) | EXPLAIN ANALYZE BUFFERS của đúng SQL service đã chạy |
| B | [BaRequirementChecks.cs](../tests/BotZone.IntegrationTests/BaRequirementChecks.cs) | Voucher expiry/minimum/target, 36 tháng/12 quý cho ba metric, staff rename/reassignment rollback và hai cây precedence |
| P | [PaymentDispatchChecks.cs](../tests/BotZone.IntegrationTests/PaymentDispatchChecks.cs), [payment-dispatch-result.json](../tests/BotZone.IntegrationTests/payment-dispatch-result.json) | Dispatcher thật: MoMo/Visa đủ outcome, retries, duplicate place, foreign ownership, banking/COD deny và late reconciliation; IDs và cancel/refund cleanup |
| R | [ReadProjectionChecks.cs](../tests/BotZone.IntegrationTests/ReadProjectionChecks.cs), [query-catalog-result.json](../tests/BotZone.IntegrationTests/query-catalog-result.json) | Kho chỉ nhận dữ liệu đóng gói/serial/address, không nhận tài chính; 9 query SQL catalog chạy read-only |
| J | [InvoiceIssueChecks.cs](../tests/BotZone.IntegrationTests/InvoiceIssueChecks.cs), [invoice-issue-result.json](../tests/BotZone.IntegrationTests/invoice-issue-result.json) | Phát hành có/không draft, retry giữ ID/number, lỗi giữa trừ tồn và invoice rollback toàn bộ; cleanup qua giao thất bại/kiểm hàng/CN/restock |
| V | [layout-results.json](../artifacts/pdf-review-fixed/layout-results.json), [export-results.json](../artifacts/pdf-review-fixed/export-results.json), [review_pdf.py](../tests/BotZone.IntegrationTests/review_pdf.py) | Render từng trang của list7/report3/detail1; 85 ID đủ/không trùng; zero text out-of-bounds; tổng cùng hàng dữ liệu; đã xem ảnh đầu/cuối/list/report/detail và chữ Việt |
| DR | [restore-verification.json](database/restore-verification.json), [restore-results.json](../artifacts/api/restore-results.json) | Restore DB/media sang database riêng, matchedBackupSource=true cho100 bảng/sequence/golden/media; HTTP login/list/export/private file checks |
| CR | [cart-restart-results.json](../artifacts/api/cart-restart-results.json), [cart-restart.mjs](../tests/api/cart-restart.mjs) | API process stop/start thật, customer2 đăng nhập phiên mới, giỏ+DEMO1M deepEqual sau restart và quote giảm1triệu; phục hồi giỏ trước test |
| E | [golden.xlsx](../examples/exports/golden.xlsx), [golden.pdf](../examples/exports/golden.pdf), [manifest.json](../examples/exports/manifest.json) | Stable sample tạo từ owner API5080 với đúng8G01–G08; một export/readAt; kiểm ZIP/XML/PyMuPDF,15 dòng detail,97,13m/3,5m/93,63m, serial0 và SHA-256; không chạy fixture suite trên database chính |
| AU | [auth-results.json](../artifacts/api/auth-results.json) | 8 ca HTTP thật: CSRF, Identity, Mailpit, role, token một lần, reset/session/logout |
| N | [full-final/smoke.json](../artifacts/winforms/full-final/smoke.json) | WinForms Windows x64 chạy thật, module/data/detail/report/export, layout tại DPI thật 120 (125%) |
| NR | [roles-native-save/smoke.json](../artifacts/winforms/roles-native-save/smoke.json) | Bốn role, focus traversal bằng code native, CMS Save button, API từ chối vượt quyền |
| NI | [inventory-api-first/smoke.json](../artifacts/winforms/inventory-api-first/smoke.json) | Luồng API nhập hàng/điều chỉnh và quyền; xem từng assertion trong JSON |
| W | [commerce-results.json](../artifacts/web/commerce-results.json), [aftersales-results.json](../artifacts/web/aftersales-results.json) | Browser checkout COD → handoff/invoice → đối soát; khách tải file/khởi tạo hồ sơ hậu mãi |
| WC | [cms-results.json](../artifacts/web/cms-results.json) | Publish/restore thành version mới, preview một lần/revoke/không kèm actor session, stale revision |
| WL | [landing-results.json](../artifacts/web/landing-results.json), [catalogue-results.json](../artifacts/web/catalogue-results.json), [smoke-results.json](../artifacts/web/smoke-results.json) | VI/EN, các viewport, explore/hinge/focus, reduced motion, WebGL fallback, catalogue/compare |

Các kết quả N/NR/NI/AU/W/WC/WL do các tác nhân phụ trách giao diện/API chạy và lưu; bảng này dẫn đúng artifact, không diễn giải việc mở module thành đã kiểm hết các mutation của module đó.

## Những kết quả tiền và đồng thời đã đối chiếu

| Kiểm tra | Kết quả thực |
|---|---|
| Golden G01–G08 | 8 issued; tổng phát hành 97.130.000 VND; D01 99.000.000 draft bị loại |
| Credit notes / net | 3.500.000 / 93.630.000 VND |
| Theo năm Việt Nam 2024 / 2025 / 2026 | 28.000.000 / 44.000.000 / 25.130.000 VND |
| NV01 / NV02 / NV03 | 42.800.000 / 44.500.000 / 9.830.000 VND |
| P13 / P14 / AP2 | 43.000.000 / 41.000.000 / 10.800.000 VND; tổng sản phẩm 94.800.000, Care 2.300.000, shipping 30.000 |
| Bộ lọc logic BA | G02,G03,G07; 46.800.000 VND |
| Thu / hoàn | Thu 81.130.000 (G02 chưa thu); hoàn 3.500.000, trong đó credit 450.000 và tiền mô phỏng 3.050.000 |
| CN02 và trả một unit khác của G08 | Mỗi unit AP2 net 1.500.000 → credit 450.000 + tiền 1.050.000 theo phân bổ gốc |
| Pagination | 75 invoice PAG, 4 trang cỡ 20, ID không trùng; tổng 7.502.850; export 75 dù yêu cầu page 2 |
| Tồn cuối | Hai khách/connection cùng đặt: một thành công, một STOCK_CONFLICT; available 0 rồi về 1 sau cancel |
| Voucher cuối | Đủ stock cho cả hai; chỉ một redemption reserved, giao dịch kia PROMO_LIMIT |
| Payment idempotency | Cùng key đồng thời: cả hai nhận kết quả, DB chỉ có một receipt |
| Payment dispatcher | Customer MoMo/Visa chọn failed/pending/expired/cancelled/success và retry; chỉ success tạo receipt; cùng key khác payload409; banking/COD customer403, foreign customer404; late success → reconciliation, release stock |
| Refund cạnh tranh | Hai key khác nhau: chỉ một refund completed, yêu cầu sau trả 0; tổng không vượt nguồn đã thu |
| Thay pointValueVnd | Policy tạm 2.000: unit mới một điểm giảm 2.000; unit golden cũ giữ 1.000; rollback policy |
| Export có writer | Hai PostgreSQL PID khác nhau; reader RepeatableRead đọc header trước writer, writer commit khi query items đang chờ; XLSX giữ JSON cũ, lần đọc mới thấy JSON mới; PDF mở được, cả hai checksum khớp một manifest/dataset; draft được phục hồi |
| Phát hành / rollback | J: no-draft và existing-draft đều một invoice/number/shipment/stock movement; same-key retry không đổi; bỏ current handler của fixture khiến Invoice thất bại sau stock loop, transaction trả stock và draft về trạng thái trước |

Các command hiện lấy advisory transaction lock chung. Test C chứng minh xử lý đúng khi các yêu cầu đồng thời đến ứng dụng; không phải chứng minh khả năng mở rộng throughput khi nhiều writer cùng hoạt động.

## Đối chiếu DB-01…DB-07

“Đạt phần tự động” chỉ áp dụng phạm vi được nêu. “Một phần” nghĩa là có triển khai/bằng chứng nhưng còn điều kiện của BA chưa chạy đủ.

| Mã | Trạng thái và bằng chứng |
|---|---|
| DB-01 | Đạt các ca tự động — CR cart/voucher qua API restart; C đặt đơn lưu reservation/version/discount và tranh quota cuối; B expiry/minimum/target; I/G snapshot/version và used target immutable. |
| DB-02 | Đạt phần tự động — I/N: list/count/tổng/detail dùng DB; golden và native detail có dòng thật. |
| DB-03 | Đạt — chung InvoicePredicateBuilder/InvoiceQueryService/InvoiceExportService; I kiểm scope/logic; S kiểm snapshot. Truy vết ở query catalog. |
| DB-04 | Đạt — `ToListAsync()` gán `List<InvoiceListItemDto>`; native `BindingSource`/`DataGridView`; N chạy thật. |
| DB-05A | Đạt — I/B kiểm tổng năm, ranh giới VN và từng hàng của36 tháng/12 quý cho issued/adjustments/net, bao gồm0. |
| DB-05B | Đạt — I/B kiểm đủ ba nhân viên/scope; đổi tên NV01 và current handler G03 trong rollback, report query thật vẫn giữ tên snapshot và tổng NV01/NV02. |
| DB-05C | Đạt tổng model — I kiểm P13/P14/AP2 và tách Care/shipping; Q chạy report product. Chưa assertion đầy đủ từng SKU/kỳ. |
| DB-06 | Đạt các ca BA — I/B: whitelist/parameters, >/< /= /<>, hai cấu trúc ngoặc khác nhau có tập ID khác nhau, AND/OR/NOT, NOT EXISTS, NULL, timezone, độ sâu, BIGINT. |
| DB-07 | Đạt xuất/đọc — I/N/W mở/tải XLSX/PDF thật, 75 dòng, serial số0, checksum/privacy; S writer overlap; V render/check từng trang và quan sát chữ Việt. SaveFileDialog thao tác thủ công còn riêng WF-08. |

## Đối chiếu AC-01…AC-16

| Mã | Trạng thái và bằng chứng |
|---|---|
| AC-01 | Đạt trên máy hiện tại: baseline/fresh EF/seed lặp, start→stop→restart scripts thực, main native trước/sau8issued97,13m, DB/Mailpit giữ nguyên; DB+uploads restore và API/privacy đã kiểm DR. [Lifecycle](../artifacts/local-runtime/lifecycle-result.json). Chưa kiểm trên máy Windows mới hoàn toàn. |
| AC-02 | Đạt ca HTTP tự động AU: signup customer, Mailpit verify một lần, reset thu hồi password/session/token, logout. Token timeout theo thời gian cần test riêng. |
| AC-03 | Đạt các vai trò chính — AU/NR/WC từ chối API trực tiếp sai role; owner không publish, content không tài chính/giá. Không suy toàn bộ payload matrix từ vài endpoint. |
| AC-04 | Đạt tranh tồn quantity cuối — C. Serial cấp kho có guard và luồng A; chưa riêng ca hai người tranh đúng serial cuối. |
| AC-05 | Đạt các ca tự động C/P — payment cùng key đồng thời, duplicate place cùng key trả cùng order, changed retry409; failed/pending/expired/cancelled không thu, late success đưa reconciliation. Timeout được mô phỏng bằng làm quá hạn deadline của fixture riêng. |
| AC-06 | Một phần — I G02 unpaid dù issued; W COD delivery rồi reconciliation; A banking đúng role. Chưa assertion UI chính giữa delivered và reconcile. |
| AC-07 | Một phần — A thu cũ quantity, đổi giá, owner approval, expiry, remote reject/freight hai chiều/cấp một lần; có golden serial. Chưa full positive iPhone serial theo command từ đầu đến cuối. |
| AC-08 | Một phần — A/C hoàn đúng nguồn, phần lẻ/điểm, refund đồng thời; chưa hai checkout tranh cùng số dư credit. |
| AC-09 | Đạt service scenarios A — expiry/ownership, kiểm hàng, full wrong-item có shipping, partial không shipping, used stock quarantine, không tự restock mới. |
| AC-10 | Một phần — A serial và quantity đổi cùng SKU, giữ dates; rơi vỡ loại trừ, Care chưa dùng hoàn/cancel. Chưa mọi nhánh repair thành công/no-stock refund qua UI. |
| AC-11 | Một phần — WC draft/revision/publish/restore/preview thật; WL model hợp lệ. Chưa tất cả loại GLB/media hỏng khi publish. |
| AC-12 | Đạt các ca browser WL — VI/EN 360/390/430/768/1024/1366/1440, explore/hinge/close focus-scroll, reduced-motion/WebGL fallback. Touch trên thiết bị thật chưa kiểm thủ công. |
| AC-13 | Một phần — WL giữ variant/filter khi đổi locale; W mua VND. Chưa kiểm tỷ giá thay đổi xuyên reload và mọi lựa chọn giỏ. |
| AC-14 | Một phần — I/W đổi customer ID bị từ chối cho invoice/export, private evidence validate trong A. Chưa đầy đủ forged MIME/oversize/GLB-corrupt matrix và đọc private media từng case. |
| AC-15 | Một phần — NI nhập/điều chỉnh có bằng chứng riêng; I golden và adjustments/net đúng. Chưa assertion toàn bộ partial PO/cost reconciliation cho mọi tracking mode. |
| AC-16 | Chưa nghiệm thu — tài nguyên GLB/font/email/demo local có sẵn; chưa tắt Internet rồi chạy trọn demo. |

## Đối chiếu DB-T01…DB-T20

| Mã | Trạng thái và bằng chứng |
|---|---|
| DB-T01 | Đạt CR/C: giỏ+voucher giữ nguyên qua stop/start API và phiên mới, quote giảm1triệu; đặt đơn lưu redemption/version/phân bổ trong DB. |
| DB-T02 | Đạt: C tranh lượt cuối; B expired/minimum/wrong-target bị từ chối và valid control thực sự giảm1.000. |
| DB-T03 | Đạt: I/N + code typed list và cùng predicate/service. |
| DB-T04 | Đạt: 8 / 97.130.000; draft 99 triệu loại. |
| DB-T05 | Đạt: I/B năm/biên VN; đủ36 tháng và12 quý, issued/adjustments/net từng kỳ đúng expected bao gồm0. |
| DB-T06 | Đạt: I/B ba tổng staff/scope; đổi current name/handler trong rollback, report query vẫn giữ snapshot/tổng lịch sử. |
| DB-T07 | Đạt tổng golden: model 94,8 triệu + Care2,3 triệu + ship30k =97,13 triệu. |
| DB-T08 | Đạt golden: nhiều attempts/lines không nhân header; G03/G04 cùng18 triệu đều tính. |
| DB-T09 | Đạt: bộ BA ra G02,G03,G07; B kiểm A AND(B OR C) ra G01,G03,G07 còn (A AND B)OR C thêm G05,G06; =18triệu trả cả G03/G04, <> loại cả hai. |
| DB-T10 | Đạt: AP2 dùng NOT EXISTS; hóa đơn chứa hỗn hợp bị loại; NULL có node riêng, rule =null bị từ chối. |
| DB-T11 | Đạt: giá trị SQL injection chỉ thành parameter; scope đặt ngoài OR/NOT; customer/content/warehouse financial deny. |
| DB-T12 | Đạt: 75 invoice, page20, ID/tổng/export và sort ổn định. |
| DB-T13 | Đạt I/V: XLSX/PDF mở thật, chung manifest/count/tổng, serial0; render mọi trang, không chữ vượt biên/replacement glyph; ảnh đại diện chữ Việt đã xem. Lỗi trang chỉ có tổng đã sửa bằng total row giữ cùng data row; filter PDF dễ đọc. Không tuyên bố đã mở Excel hay SaveFileDialog bằng tay. |
| DB-T14 | Đạt: S buộc overlap thật bằng barrier, hai connection/PID; writer commit trong snapshot; JSON cũ trong export, fresh read mới; hai file checksum đúng cùng dataset. |
| DB-T15 | Đạt các guard G: issued→draft, delete, line/frozen unit, used promo target bị chặn. Chưa mọi mutation SKU/name/price trên current catalog rồi đối chiếu snapshot. |
| DB-T16 | Một phần: 15 guard G bao phủ FK, duplicate SKU, sai allocationSKU/parent, tender cap, PO cap, uncommitted building/header sai. Chưa đủ từng CHECK/UNIQUE/event-key/zero-net edge. |
| DB-T17 | Đạt J/A: phát hành có/không draft, retry giữ đúng ID/number, không lặp shipment/stock; lỗi invoice sau stock mutation rollback tất cả; COD failed-delivery có CN và không tạo khoản hoàn khi chưa thu. |
| DB-T18 | Đạt các ca tài chính: C refund hai connection, I CN02 450k/1.05m, A qty>1 lẻ VND/điểm dùng ordinal, failed refund retry/zero funds. |
| DB-T19 | Đạt: CN01 tháng5 dù G04 tháng4; issued/adjustments/net/receipts/refunds đối chiếu riêng. |
| DB-T20 | Đạt DR: restore riêng `botzone_restore_20261113153420_6d0c9293`;100 bảng/sequence/golden/media khớp backup; EF update no-op; HTTP Identity với restored keys,8golden97,13m, XLSX/PDF,3mediaSHA256 và private anonymous401/customer khác403. |

## Đối chiếu WF-01…WF-08

| Mã | Trạng thái và bằng chứng |
|---|---|
| WF-01 | Đạt N: executable Windows native, Login/Main/modules là WinForms controls. |
| WF-02 | Đạt: browser tạo order031a182a…; LoginForm/Orders/NativeCommandForm nv01 xác nhận, kho handoff chính order; browser mở invoice718486e2… /BZ-HD-00000042. [Native evidence](../artifacts/winforms/native-handoff-second/smoke.json), [browser evidence](../artifacts/web/native-handoff-results.json). |
| WF-03 | Đạt I/N và code typed EF→List→DTO→BindingSource/DataGridView; cùng service filter/report/export. |
| WF-04 | Một phần: N 1366×768 và1920×1080 tại DPI120/125%; NR SelectNextControl. Không tuyên bố 150%, đa màn hình hay bàn phím thủ công đã đạt. |
| WF-05 | Chưa nghiệm thu đủ: async/cancel/debounce/retrykey có code; chưa fault injection API chậm/mất mạng/đổi filter/hết phiên trên native. |
| WF-06 | Đạt NR các role/quyền chính: owner/order/warehouse/content và scope report; AU direct API. |
| WF-07 | Một phần: NR nút native Save thật; WC API publish/restore/preview/revoke/stale revision; chưa nhấn native publish/restore toàn chuỗi. |
| WF-08 | Một phần: N download XLSX/PDF thật; I/V mở file/đếm/tổng/render/chữ Việt; [winforms.md](winforms.md) build/publish và Windows runtime. SaveFileDialog/cancel thao tác thủ công chưa nghiệm thu. |

## Giới hạn dữ liệu và việc còn mở

Golden, PAG và các đơn browser/fixture cùng tồn tại trong clone. Để đối chiếu BA phải lọc G01…G08; tổng mặc định của toàn DB sẽ lớn hơn 97.130.000. Không reset database để làm con số trông giống golden.

[expected-results.json](../expected-results.json) chép literal BA và tính nguyên độc lập cho năm/12quý/36tháng/staff/model/CN/filter; không sinh bằng query đang test. [constraints-and-transactions.md](database/constraints-and-transactions.md) mô tả nguồn guard, deferred checks, rollback, snapshot và giới hạn outbox at-least-once.

Source seed mới có tên hiển thị đúng cho chín nhóm và NV03 inactive nhưng vẫn giữ lịch sử G05/G06. Những thay đổi INSERT mới không cập nhật clone đã có base marker, tài khoản hay category do người dùng sửa. Lượt idempotence trên clone đã seed chỉ chứng minh không ghi đè; không phải bằng chứng fresh seed của các giá trị mới. Root đã chạy fresh EF/seed hai lần riêng: [fresh-seed-verification.json](database/fresh-seed-verification.json) xác nhận chín tên nhóm, NV03 inactive và hai hóa đơn lịch sử; main cũng có [main-seed-verification.json](database/main-seed-verification.json).

Clone ban đầu từng seed loyalty earn ngay tại delivery rồi reversal. Source `DemoSeeder` đã sửa cho seed mới: earn sau thời hạn trả và theo net trước maturity. Ledger append-only của clone cũ không bị viết lại; do đó không dùng lịch sử loyalty cũ trong clone này làm bằng chứng hoàn tất chính sách maturity 14 ngày. Các tổng invoice/credit/refund golden vẫn đúng. Cần chạy fixture mới trên clone sạch để chứng minh riêng lịch earn này.

Chuỗi sửa chữa Care nhiều lần cho cùng unit quantity/cùng lot, mọi nhánh no-stock refund, payload malformed, tải đồng thời lớn và toàn bộ UX ngoại tuyến vẫn cần thêm ca riêng. Các mục “Một phần/Chưa” bên trên được giữ nguyên cho tới khi có bằng chứng thực tế.

## Kiểm tra bổ sung sau checkpoint 73 ca

- [customer-detail-results.json](../artifacts/api/customer-detail-results.json): 5 ca HTTP thật trên main, owner/nv01 đọc được snapshot khách/điểm/hạng/credit/ledger, list và detail khớp; unknown customer404, warehouse/content/customer403. Chỉ đọc nghiệp vụ.
- [process-generation-check.json](../artifacts/local-runtime/process-generation-check.json): phát hiện và loại11 process cũ bị nhầm parent PID; dry-run không liệt kê chúng, stop thật chỉ đóng BotZone, cả11 process vẫn chạy đúng timestamp. Traversal V2 kiểm creation time từng cạnh cha–con, stop kiểm lại executable/thời điểm và loại record đời cũ. Không ứng dụng ngoài BotZone bị dừng trong các lượt đã kiểm.

- [Native customer detail](../artifacts/winforms/customer-details-published/smoke.json): bản publish Windows thực, owner/nv01 mở8tab; hồ sơ/điểm/hạng/credit/holds và các lịch sử khớp API; đã xem ảnh summary. Full solution Release sau bổ sung vẫn0warning/0error.

## Ảnh sản phẩm, tương tác và landing v003 — 13/11/2026

Các kết quả dưới đây bổ sung cho ma trận BA, không tự chuyển các mục “Một phần/Chưa” phía trên thành đạt.

| Kiểm tra thực tế | Bằng chứng và phạm vi |
|---|---|
| 35 model / 37 ảnh | [39 mục đạt](../artifacts/product-photos/live-results.json) trên production cuối: bytes/SHA/MIME/kích thước decode, VI/EN, ánh xạ theo màu. So sánh catalogue với snapshot trước nhập: ID, giá, tồn kho, biến thể, tên, thông số, compatibility và settings giữ nguyên. |
| Khung ảnh | [5 mục đạt](../artifacts/product-photos/framing-results.json): iPad đầy đủ trên card/detail, giỏ390px không tràn ngang, hover/reduced motion giữ đúng tỷ lệ. Đã xem ảnh catalogue mobile/desktop sau khi cuộn tải đủ lazy images. |
| Dropdown/phân trang/motion | [13 interaction checks](../artifacts/web/interactions-results.json), [Select fixture390/1440](../artifacts/web/select-component-results.json):6/12 sản phẩm mỗi trang, URL/filter/locale/history, bảy chiều rộng, bàn phím/typeahead/portal/required/FormData/Escape/focus và reduced motion. Catalogue regression và26 storefront smoke checks cũng đạt; không có mutation nghiệp vụ trong các lượt UI này. |
| Golden sau cập nhật nội dung | [Đọc API thật](../artifacts/product-photos/golden-preserved.json):8 hóa đơn G01…G08, issued97.130.000, credit3.500.000, net93.630.000 VND. Không seed/reset/export trong kiểm tra này. |
| V003 nguồn và runtime | [Sáu file khớp hash](../artifacts/3d-v003/import-verification.json); [20/20 browser cases](../artifacts/3d-v003/final/results.json),19 decoded pixel checks,47 PNG. Four-track clip, STEP screen, hold100%,5reverse cycles mỗi variant, outer glass, materials/UV, fallback/lazy/touch/explore được kiểm thực. |
| Hình ảnh và hiệu năng | [Báo cáo quan sát](../artifacts/3d-v003/final/README.md): Edge/WebGL2 Intel Iris Xe; VI/EN390/430/1366/1440. Pro Max59,99FPS trong983,5ms; RAFmedian16,7/p9516,8ms; setup122–373ms. Đã xem trực tiếp mobile/laptop và các pose chính. Không phải benchmark điện thoại thật hoặc thermal kéo dài. |
| Build và favicon | Typecheck/Next production build đạt; .NET solution Release0warning/0error, native publish đạt. [Favicon200 và zero console errors](../artifacts/3d-v003/final/favicon-verification.json) sau bản build cuối. Repository không có script lint riêng. |

Còn đường nếp gập Duo rất mảnh ở một số góc. GLB, texture và UV gốc không bị sửa. Tab-hidden test điều khiển visibility API, không đo tab OS thực; mobile là touch/DPR giả lập trong Edge. Chưa kiểm native interaction của hai lựa chọn CMS v003 mới; build/publish không thay thế bằng chứng đó. Các artifact3D ở thư mục gốc/center-fix là lịch sử chẩn đoán, không phải lần nghiệm thu cuối. Lỗi favicon404 xuất hiện trong lượt3D ban đầu đã có kiểm chứng sửa riêng; lỗi tải model được cố ý tiêm trong negative case.

## VI/EN, thông số, so sánh, giỏ và auth — bản production cuối 13/11/2026

| Kiểm tra thực tế | Kết quả và bằng chứng |
|---|---|
| Nguồn/dữ liệu thông số | 35 model, 609 mục song ngữ, 75 mã trường. [Nguồn theo model](catalogue-spec-sources.md); [plan](../artifacts/specifications/import-plan.json), [apply](../artifacts/specifications/import-applied.json), [chạy lại: 0 thay đổi](../artifacts/specifications/import-after.json). Import cùng backend C#, không migration mới. |
| API và bảo toàn catalogue | [39 checks đạt](../artifacts/specifications/live-results.json): list/detail VI/EN, 609 mục, nguồn, nhãn biến thể; ID/giá/tồn/ảnh/raw options/settings giữ nguyên so với snapshot ngay trước import. Metadata cũ vẫn còn trong JSON DB. |
| Rà ngôn ngữ | [137 checks được nghiệm thu](../artifacts/localization/acceptance.json): 70 model/viewport, 45 public routes/API/metadata/query và 22 signed-in account/service/order/invoice. Không lỗi browser hoặc ứng viên sai ngôn ngữ còn lại trong phạm vi kiểm. [Năm lỗi đã sửa](../artifacts/localization/issues.md); các báo cáo lỗi ban đầu giữ làm lịch sử, không dùng để ghi pass. |
| Compare và mini bag | [4 nhóm chức năng](../artifacts/web/compare-bag/results.json), [14 layout](../artifacts/web/compare-bag/layout-results.json) VI/EN × 360/390/430/768/1024/1366/1440 px đạt. Max 3, cùng nhóm, xóa tất cả, lọc khác biệt, feedback, Escape/focus/reduced motion, nhấn giỏ liên tiếp. [Clip Edge thực 7,8 giây](../artifacts/web/compare-bag/shopping-feedback.webm), [video evidence](../artifacts/web/compare-bag/video-evidence.json). |
| Auth thật trên clone | [9 nhóm đạt và 16 bố cục](../artifacts/web/auth-ui/README.md): register có/không address, profile persistence, Mailpit EN verify/reset, cookie phiên/remember, hai session bị revoke khi change/reset, token dùng một lần, role guard, safe redirect, WinForms nonpersistent. Không ghi mật khẩu/token/cookie value vào report. |
| Kiểm lại lỗi locale/layout | [23 checks đạt](../artifacts/web/commerce-locale-regression/results.json): login/register drafts giữ qua đổi locale, mật khẩu xóa; 20 bố cục đơn/hóa đơn VI/EN ở 390/1440; GET trước/sau xác nhận item snapshots không đổi. Shipping hệ thống được dịch ở presentation, tên/địa chỉ nhập tay giữ nguyên. |
| Hồi quy storefront | 39 ảnh/model checks chạy lại đạt; bỏ các khác biệt presentation có chủ đích khỏi phép so snapshot cũ, vẫn kiểm raw variants/giá/tồn/ảnh. Catalogue regression, Select fixture 390/1440 và 26 smoke checks đạt. Auth smoke đã chuyển kiểm menu/currency sang catalogue vì auth dùng bố cục riêng. |
| Build/unit | 15/15 unit tests, gồm 10 filter cũ và 5 auth helpers. Typecheck/Next production build đạt. Toàn solution .NET Release trên Windows đạt 0 warning/0 error sau khi đóng API clone. Không có script lint riêng. |
| Golden | [Đọc API sau import thông số](../artifacts/specifications/golden-preserved.json): 8 G01…G08, issued 97.130.000, giảm 3.500.000, net 93.630.000 VND. |

Main browser kiểm UI dưới dạng guest/read-only; các lần đăng ký, đổi/reset mật khẩu và đồng bộ giỏ/wishlist đã đăng nhập chạy trên DB clone qua cùng API binary 5091/Next production 3001. Helper QA đã đóng với xác minh PID/path/creation time; main 5080/3000 vẫn chạy. Không deploy/push/reset. Ảnh mobile là viewport Edge trên Windows; chưa kiểm điện thoại vật lý. Không OCR hoặc sửa chữ trong texture 3D bàn giao. Không mở lại toàn bộ chuỗi WinForms trong đợt storefront này, các trạng thái BA “Một phần/Chưa” giữ nguyên.

## Điều chỉnh khám phá 3D

[Kiểm tra cuối](../artifacts/3d-v003/exploration-controls/final/results.json) đạt cho Pro Max/Duo tại 1440px EN và 390px VI: wheel hai chiều không đổi camera distance, kéo vẫn xoay, reset giữ framing theo màn hình; màu sáng chuyển nền tối qua opacity trung gian thực; màu gốc/tối trở lại nền sáng; reduced motion tức thời theo clamp 0,01ms chung. Không còn pause/ba nút gập; slider gập vẫn hoạt động. [Hồ sơ](../artifacts/3d-v003/exploration-controls/README.md) ghi thêm ba nhóm hồi quy animation/material/keyboard/reset đạt, hash sáu file không đổi, typecheck/Next production build đạt và lỗi assertion reduced-motion ban đầu đã được sửa. Không tuyên bố đã chạy lại toàn bộ 20 ca cũ hoặc kiểm điện thoại vật lý.

## Bố cục laptop, ưu thế thông số và nền tài khoản

| Phạm vi | Kết quả quan sát |
|---|---|
| Modal 3D | [10 tổ hợp máy/viewport](../artifacts/3d-v003/laptop-refinement/README.md):1024×600,1093×614,1280×720,1366×768,1440×900. scrollHeight không vượt clientHeight, mọi nút/input nằm trong cửa sổ; canvas cao ít nhất220px. Một nút chuyển mặt thay cặp nút cũ. Duo animation hai viewport đạt; [controls cuối](../artifacts/3d-v003/laptop-refinement/final-controls/results.json) đạt sau khi đo camera đã ổn định. |
| So sánh | [11 nhóm đạt](../artifacts/web/refinement-compare/README.md):VI/EN390/1024/1366/1440; header140/146px,7 dòng dữ liệu đầy đủ trên laptop1366×768 cho2/3 sản phẩm. Sticky từng `th` giữ đúng khi cuộn; highlighting, keyboard/max3/same-category/remove/clear đạt. Không lỗi browser/ghi API. |
| Ý nghĩa giá trị | Unit20/20 gồm5 nhóm mới về GB/TB, RAM kèm điều kiện SSD, Hz danh sách/range, unknown/ties/bit-byte và active SKU. [Audit corpus216 phép so](../artifacts/specifications-research/comparison-helper-review.json) đúng trên dữ liệu thực; hai edge case ngoài corpus Hz đảo thứ tự và bit/byte được sửa và có unit regression sau audit. Không đánh giá thắng tổng thể hay suy luận chip/màn hình/camera. |
| Auth gradient | [12/12 layout/interaction](../artifacts/web/refinement-auth/README.md):login/register VI/EN1366×768/1440×900/390×844, eye/remember/optional address/invalid form focus. Gradient liền đến chân trang, không overflow; không request ghi API. Logic auth không đổi, không chạy lại mutation suite. |
| Build/phạm vi | Typecheck,20 unit và Next production build đạt. Đã xem trực tiếp ảnh 3D laptop, compare hai/ba sản phẩm và auth desktop/mobile. Chỉ storefront; không DB/schema/backend/GLB changes, không suy ra nghiệm thu physical mobile hoặc các mục WinForms còn mở. |

Lỗi thực mobile compare overflow do accessible text thoát cell đã sửa và kiểm lại. Các lỗi harness đo `thead` thay `th`, seed localStorage lặp và đo camera quá sớm được giữ trong artifacts chẩn đoán, tách khỏi report cuối. Mọi browser QA đã đóng; main web3000/API5080 vẫn chạy.

## So sánh chi tiết CPU/GPU và bảng trắng–đen

Kết quả 13/11/2026, thay phạm vi helper chỉ storage/RAM/Hz của lượt trên. [Hợp đồng thuật toán](product-comparison.md), [audit nguồn Apple](../artifacts/specifications-research/detailed-comparison-audit.md).

| Phạm vi | Bằng chứng hiện hành |
|---|---|
| Unit | `npm.cmd test`:42/42 đạt,0 skipped; giữ đủ10 filter cũ,22 nhóm mới trong `tests/unit/specification-comparison-detailed.test.ts`. Có ca CPU/GPU iPad thật, RAM có điều kiện SSD, đơn vị, khoảng cấu hình, ngữ cảnh độ sáng/pin/sạc, phủ định tính năng và dữ liệu thiếu. |
| Corpus/hoán vị | [Báo cáo](../artifacts/comparison/detailed-corpus.json):28 trường,1.397 cặp,8.806 bộ ba,121.889 lần gọi VI/EN và hoán vị. Kiểm tính nhất quán và thứ tự, không phải121.889 lần xác minh độc lập tính đúng của thông số hay test browser. |
| Browser | [12/12 ca cuối](../artifacts/web/semantic-compare/README.md):3 lựa chọn cặp/bộ ba ×VI/EN ×390×844/1366×768. Nhấn CPU/GPU/chip/RAM/độ sáng/số điểm ảnh đúng, bằng nhau/Hz thiếu không tự chọn bên thắng. Xóa sản phẩm tính lại; chế độ khác biệt, giải thích bàn phím/chạm và Escape/focus đạt. |
| Bố cục | Header140px desktop/146px mobile; laptop8–10 dòng dữ liệu đầy đủ khi vùng bảng nằm trong màn hình. Nền ô nhấn là xám trung tính, font700 và có lý do. Không tràn viewport; mobile vẫn cuộn ngang trong bảng. Đã xem trực tiếp ảnh cặp iPad VI, ba iPad EN và mobile VI. |
| Build/vận hành | Typecheck/Next production build đạt; bản cuối chạy3000, API5080 giữ nguyên. Browser QA đã đóng,0 page error và0 request ghi API. Không thay DB/schema/backend, không chạy lại auth/3D/WinForms hoặc financial suite trong lượt này. |

Initial density report/screenshot được giữ riêng: bản đầu chỉ thấy6 hàng với cặp Air/Pro vì tổng scroll-padding và scroll-margin dư90px. Đã bỏ scroll-margin dư và để bảng cao theo phần còn lại của viewport, không giảm kích thước chữ hoặc bỏ điều khiển. Báo cáo cuối ở đường dẫn trên được tạo sau build sửa lỗi. Mobile là Edge giả lập viewport trên Windows, chưa phải thiết bị vật lý. Không gán ưu điểm cho mọi thông số mô tả; không tuyên bố có AI hoặc benchmark hiệu năng.

## Tương tác con trỏ ở hero — 14/11/2026

[Báo cáo cuối và clip](../artifacts/3d-v003/hero-pointer/README.md), script `tests/web/hero-pointer-live.mjs`: **7/7 nhóm đạt**, Edge/WebGL thực trên Windows. Pro Max/Duo tại1366×768 và1920×1080 phản hồi cả chuột bên vùng chữ; sweep trái/phải thay đổi yaw khoảng21,7–22°, pitch7,1–8°. Snapshot trung gian xác nhận damping, khi rời vùng trở về tâm. Camera, screen material/UV và pose clip Duo không đổi qua sweep.

Tám ảnh canvas có pixel mô hình thực và silhouette không chạm dải6px ở mép; đã xem trực tiếp toàn cảnh Pro Max trái/phải và Duo. Modal vẫn drag được, wheel không đổi camera, pointer hero được vô hiệu hóa khi modal mở. CDP touch390px cuộn trang được và không điều khiển parallax; reduced motion giữ poster/link.0 page error,0 API ghi dữ liệu; browser đã đóng. Build production/TypeScript đạt; không tuyên bố kiểm điện thoại thật hoặc FPS bền vững.

Hai chẩn đoán đầu được giữ riêng: thiếu helper ghi video (đã cài trong cache workspace) và phép chiếu AABB thế giới quá rộng tại góc nghiêng. Ảnh thực không bị cắt; kiểm mép đã chuyển sang pixel của silhouette thay vì coi các góc trống của AABB là hình dựng. Không sửa model hoặc camera để làm phép đo đó qua.

## Căn hàng 3D và mật khẩu sáu ký tự — 17/11/2026

- [3D16/16 tổ hợp](../artifacts/web/scene-controls-20261117/results.json): Duo VI/EN ở390/430/768/1024/1366/1440px và Pro Max VI/EN ở390/1366px. Nhãn/output cùng hàng, tâm thanh kéo thẳng nhau, nút desktop cùng baseline; modal vừa viewport. Gập25% đọc clip0,5s, xoay bằng phím, đổi mặt/màu/reset, Escape/focus và wheel không zoom đều đạt.0 page error/0 API writes. Đã xem ảnh Duo390 và1024.
- [Auth10 nhóm chức năng](../artifacts/web/password-policy-six/README.md), thêm bản ghi cookie và16 bố cục: đăng ký thực bằng sáu số, đổi mật khẩu sáu chữ thường lặp, reset sáu chữ hoa; dưới6 bị chặn ở frontend/backend VI/EN. Mail verification/reset, session revocation, token single-use, remember giới hạn8h và role guards đạt. Cùng binary/API nhưng dùng DB clone5091/web3001; không tạo tài khoản thử trên main.
-42/42 unit đạt; .NET API build0warning/error và Next production/TypeScript đạt. Không đổi schema, migration, hash hay password tài khoản có sẵn. Backend/website chính tiếp tục chạy; chỉ helper QA bị đóng sau đối chiếu PID/path/thời điểm tạo. Không tuyên bố chạy lại financial suite hoặc WinForms runtime.

### Mở/thu địa chỉ giao hàng

[5/5 ca cuối](../artifacts/web/auth-address-motion-20261117/results.json), `tests/web/auth-address-motion-live.mjs`, chạy trên production3000 sau lần build bổ sung animation. VI/EN ở390/1366px có height/opacity trung gian khi mở và thu; reduced motion không có frame trung gian. Khi thu, chiều cao vùng bằng0 và không còn gap; fieldset bị disabled, wrapper inert/aria-hidden ngay. Tab bỏ qua trường ẩn, focus quay về checkbox, năm lần bật/tắt nhanh giữ trạng thái cuối, giá trị/autofill được bảo toàn, validation chỉ yêu cầu địa chỉ khi chọn lưu. minLength6 vẫn giữ.0 page error/0APIwrite, browser đã đóng.

Hai lỗi assertion harness ban đầu đã sửa trước report cuối: kiểm `fieldset.disabled` trực tiếp và kiểm input con thay matcher disabled không áp dụng cho fieldset; reduced-motion duration so≤1ms vì CSS chung clamp0,01ms. Không đổi ứng dụng để làm các assertion này qua. Bộ auth mutation trên clone ở phần trên được chạy trước bổ sung animation; không coi5 ca chỉ đọc này là lượt đăng ký thật mới.
