# Hợp đồng API BotZone — ASP.NET Core

Đối chiếu mã thực ngày 13/11/2026 theo BA v1.1. API duy nhất là `src/BotZone.Api` (.NET 10), dùng ASP.NET Core Identity, EF Core/Npgsql và PostgreSQL. Website Next.js chỉ proxy cùng origin; quản trị dùng C# WinForms. Không có runtime Better Auth hoặc API quản trị React.

Nguồn chuẩn: [Program.cs](../src/BotZone.Api/Program.cs), [AuthEndpoints.cs](../src/BotZone.Api/AuthEndpoints.cs), [DTO hóa đơn](../src/BotZone.Contracts/Invoices.cs), [dịch vụ nghiệp vụ](../src/BotZone.Infrastructure/Services). Tài liệu ghi contract hiện có; quyền và state được kiểm lại ở API, không suy ra từ nút đang hiện trên client.

## Quy ước truyền dữ liệu

- API local `http://127.0.0.1:5080`; web gọi `/api/*` qua Next ở port 3000. Health trực tiếp là `GET /health`, không phải `/api/health`.
- JSON input dùng camelCase. DTO hóa đơn/catalogue và object bao ngoài dùng camelCase. Các row chi tiết trả từ SQL còn giữ tên snake_case (`order_status`, `offered_vnd`, `display_name`); không đổi tên chúng tùy ý ở client.
- Tiền VND là chuỗi số nguyên, ví dụ `"1530000"`; không dùng số thập phân hoặc float. ID nghiệp vụ là UUID; user/staff ID là chuỗi Identity và có thể không phải UUID. Ngày instant dùng ISO8601 có offset; ngày lịch `YYYY-MM-DD` được hiểu theo UTC+07.
- Lỗi nghiệp vụ có `{ "error": { "code": "STATE_CONFLICT", "message": "..." } }`. 400: input/CSRF; 401: phiên thiếu/hết hạn; 403: quyền; 404: không có dữ liệu trong scope; 409: state/revision/idempotency/constraint; 500: lỗi máy chủ. Không diễn giải HTTP 200 là thanh toán thành công: phải đọc outcome/state.
- Tất cả POST/PUT/PATCH/DELETE dưới `/api`, kể cả login và upload, cần cookie chống giả mạo cùng header `X-CSRF-TOKEN`. GET `/api/auth/csrf` trước khi gửi; lấy token mới sau login/logout/đổi mật khẩu. WinForms dùng một CookieContainer cho cả token/session.
- Lệnh ghi đi qua `POST /api/commands` với `{command,key,input}`. `key` là UUID do client giữ khi retry cùng nội dung. Scope là actor + command; payload khác với key cũ bị 409 `IDEMPOTENCY_CONFLICT`. Hash dùng JSON raw nên retry cần giữ cả serialization ổn định. Quyền hiện tại được kiểm trước trả kết quả cache. Transaction bao nghiệp vụ, ledger, idempotency và audit.

## Identity và phiên

| Method / path | Input | Response / quy tắc |
| --- | --- | --- |
| GET `/api/auth/csrf` | — | `{token}` và cookie `BotZone.Antiforgery` |
| GET `/api/session` | — | `{actor:null}` hoặc `{actor:{id,name,email,role,verified}}` |
| POST `/api/auth/register` | `{name,email,password}` | `{registered:true,requiresEmailVerification:true}`; không nhận role; tạo customer và email outbox |
| POST `/api/auth/login` | `{email,password,clientType:"web"\|"winforms"}` | `{actor}`; client winforms chỉ staff; cookie `BotZone.Session` |
| POST `/api/auth/logout` | `{}` | `{loggedOut:true}`; revoke session hiện tại |
| POST `/api/auth/verify-email` | `{userId,token}` | `{verified:true}`; token đã dùng bị từ chối |
| POST `/api/auth/forgot-password` | `{email}` | `{sent:true}` kể cả email không tồn tại |
| POST `/api/auth/reset-password` | `{userId,token,password}` | `{reset:true,sessionsRevoked:true}`; alias `newPassword` được nhận |
| POST `/api/auth/change-password` | `{currentPassword,newPassword}` | `{changed:true,sessionsRevoked:true}` và logout; cần đăng nhập lại |

Session cookie HttpOnly, SameSite=Lax, Secure theo HTTPS, hạn 8 giờ, không sliding. API kiểm `auth_sessions`, security stamp, trạng thái nhân viên và lockout mỗi request. Token email/reset có hạn 30 phút. Theo cập nhật của Khang ngày17/11/2026, mật khẩu mới tối thiểu6 ký tự, không bắt buộc chữ hoa/chữ thường/số/ký hiệu; mật khẩu rỗng hoặc chỉ khoảng trắng không hợp lệ. Cùng chính sách Identity áp dụng cho đăng ký, đổi và đặt lại mật khẩu. Mật khẩu/hash/session đã có không được tự chuyển đổi hoặc reset.5 lần sai vẫn gây lockout15 phút. Nhân viên không hoạt động không đăng nhập được; thay role/khóa staff thu hồi session.

Role: `customer`, `owner`, `order_staff`, `warehouse_staff`, `content_staff`. Owner không tự động có quyền sửa nội dung của content hoặc thực hiện bước kho dành riêng cho warehouse.

## Catalogue, landing và tài khoản

| Method / path | Input/query | Response / scope |
| --- | --- | --- |
| GET `/health` | — | `{status,backend,migrationOwner}`; kiểm kết nối DB |
| GET `/api/catalogue` | `locale=vi\|en`, `q`, `category` | `{products,categories,exchangeRateVnd}`; chỉ sản phẩm active, bản dịch published, nhóm active, SKU active |
| GET `/api/catalogue/{slug}` | `locale` | `{product}` hoặc 404 |
| GET `/api/landing` | — | `{revision,sections}` từ bản published home; chưa publish trả revision0/sections[] |
| GET `/api/cart` | — | customer: `{items:[{variantId,quantity,care}],promotionCode}` |
| GET `/api/account` | — | customer: `{creditVnd,points,tier,profile,addresses,wishlist,creditEntries,loyaltyEntries,notifications,tradeIns,returns,entitlements,claims}` |
| GET `/api/orders` | — | customer chỉ đơn mình; owner/order/kho có scope nghiệp vụ; `{orders}` tối đa 200, mới nhất trước |
| GET `/api/orders/{id}` | UUID | `{order,items,units,attempts,receipts,shipments,invoice}`; kho nhận projection đóng gói, không dữ liệu thu tiền |

Product gồm `id,slug,modelCode,category,name,description,sourceUrl,specs,poster,variants,compatibleProducts`. Variant gồm `id,sku,options,priceVnd,available,trackingMode,careFeeVnd,standardWarrantyMonths,active`. `compatibleProducts` hiện là danh sách slug sản phẩm đích của phụ kiện. Category code trả từ DB; client chuẩn hóa chữ thường khi lọc. `tradeIns[].quotes[].items` chứa từng món báo giá và quyết định; quote cũ/hết hạn/chưa owner duyệt không thể chấp nhận.

Guest cart nằm local; sau đăng nhập hợp nhất theo `(variantId,care)` lấy max quantity. Wishlist local được hợp nhất với tài khoản. API không dùng localStorage để xác thực, tính giá hoặc giữ tồn.

## Báo giá và đặt đơn

`POST /api/quote` dành cho customer đã đăng nhập, nhận:

```json
{"items":[{"variantId":"00000000-0000-0000-0000-000000000001","quantity":1,"care":false}],"promotionCode":"","points":0,"creditVnd":"0"}
```

UUID trên chỉ minh họa hình dạng; lấy variantId thực từ catalogue. Quote trả `items` với `grossVnd,voucherVnd,pointsVnd,netVnd,careVnd`, cùng `subtotalVnd,discountVnd,shippingVnd,totalVnd,creditVnd,cashDueVnd,pointsUsed,quoteHash`. Không giữ hàng khi chỉ báo giá. Gửi lại đầy đủ input và quoteHash ở `place_order`; giá/chính sách thay đổi trả `QUOTE_CHANGED` để khách xem lại.

| Command | Input | Quyền / kết quả chính |
| --- | --- | --- |
| `save_cart` | `{items,promotionCode}` | customer; giỏ tối đa30 dòng, quantity1..100 |
| `place_order` | input quote + `{quoteHash,address:{recipient,phone,province,ward,addressLine,country:"VN"},method}` | customer đã xác minh; method `cod/banking/momo/visa`; `{orderId,orderNo}` |
| `start_payment` | `{orderId}` | customer với đơn của mình; COD chỉ owner/order_staff; trả payment attempt gồm `id,requested_amount_vnd,status` |
| `record_payment` | `{attemptId,outcome,amountVnd}` | owner/order_staff; customer chỉ attempt của chính mình, method MoMo/Visa, DemoMode bật; kết quả `success/failed/cancelled/pending/expired` |
| `confirm_order` | `{orderId}` | owner/order_staff; đơn pending chưa hết hạn, trả trước phải đã thu đủ |
| `create_invoice_draft` | `{orderId}` | owner/order_staff; pending/confirmed; chưa phát hành |
| `handoff` | `{orderId}` | warehouse_staff; phân bổ tồn, phát hành hóa đơn, tạo shipment; `{invoiceId,invoiceNo}` |
| `shipment_event` | `{orderId,status}` | owner/order_staff; `in_transit/delivered/failed/returned`, chuyển trạng thái hợp lệ |
| `cancel_order` | `{orderId}` | customer chủ đơn/owner/order_staff; chưa bàn giao; `{status,refundRequired}` |
| `inspect_failed_shipment` | `{shipmentId,condition,note}` | warehouse_staff; kiện đã returned; condition `new/opened/used/faulty` |
| `close_failed_shipment` / `finalize_failed_delivery` | `{shipmentId,restock,reason}` | owner; kiểm định và chứng từ/hoàn theo trạng thái thực |

COD giao thành công và COD thu tiền là hai sự kiện riêng; đơn chỉ completed khi đủ cả hai. Mọi kênh tiền là mô phỏng. Bảo lưu hàng theo snapshot: MoMo/Visa15 phút, banking30 phút, COD chờ xác nhận24 giờ. API/worker quản lý hết hạn, release và đối soát; client không tự cập nhật tồn.

## Hóa đơn, AST, báo cáo và xuất

| Method / path | Contract | Quyền |
| --- | --- | --- |
| POST `/api/admin/invoices/search` | InvoiceQuery → `{items,matchedCount,matchedTotalVnd,readAt,page,pageSize}` | owner/order_staff |
| GET `/api/invoices/{id}` | `{invoice,items,receipts,creditNotes,refunds,units,attempts,promotions,readAt}` | owner/order_staff; customer chỉ hóa đơn issued của mình |
| POST `/api/admin/reports/invoices` | ReportRequest → `{rows,totalVnd,readAt,groupBy,granularity,metric,fromInclusive,toExclusive,invoiceIds}` | owner; order_staff chỉ hóa đơn thuộc mình |
| POST `/api/invoices/exports` | ExportRequest → `{exportId,rowCount,totalVnd,readAt,links}` | owner/order_staff; customer chỉ detail của mình |
| GET `/api/invoices/exports/{id}/{format}` | binary `.xlsx` hoặc `.pdf` | kiểm phiên/quyền hiện tại, người tạo và scope, hạn24 giờ, checksum |

InvoiceQuery mặc định `page:1,pageSize:20,sort:[{field:"issuedAt",direction:"desc"}]`, tùy chọn `filter,quickSearch,fromInclusive,toExclusive,lifecycle`. pageSize1..100; sort whitelist `issuedAt,invoiceNo,grandTotalVnd`, tối đa3 trường; id là khóa phụ ổn định. `matchedTotalVnd` là tổng toàn bộ tập lọc, không phải tổng trang.

```json
{
  "filter":{"kind":"group","logic":"AND","children":[
    {"kind":"rule","field":"grandTotalVnd","op":">","value":"10000000"},
    {"kind":"rule","field":"grandTotalVnd","op":"<","value":"20000000"},
    {"kind":"not","child":{"kind":"null","field":"issuedAt","isNull":true}}
  ]},
  "page":1,"pageSize":20,"sort":[{"field":"issuedAt","direction":"desc"}]
}
```

AST hỗ trợ `rule`, `group` AND/OR, `not`, `null`, `product:{productId,mode:"contains"|"not_contains"}`. Tối đa4 cấp/30 lá/90 node. Rule whitelist `invoiceNo,issuedAt,grandTotalVnd,discountTotalVnd,responsibleStaffId,customerId,lifecycle,paymentStatus`; toán tử `>,<,=,<>,>=,<=`. Null chỉ `issuedAt,invoiceNo,responsibleStaffId`. Scope quyền luôn AND bên ngoài biểu thức. Ngày `=D` là `[D,D+1)` giờ Việt Nam; `>D` bắt đầu D+1, `<=D` kết thúc trước D+1. Giá trị được bind Npgsql parameter, không ghép SQL từ client.

ReportRequest: `{query,groupBy:"period"|"staff"|"product",granularity:"month"|"quarter"|"year",metric:"issued"|"adjustments"|"net"|"receipts"|"refunds",dimensionId?,productLevel:"model"|"sku"}`. Mỗi row có `{period,dimensionId,dimensionName,invoiceCount,quantity,totalVnd,cashDemoVnd,storeCreditVnd}`. Điều kiện hóa đơn chứa sản phẩm khác với dimension sản phẩm đang tổng hợp. Khoảng báo cáo mặc định 2024-01-01 đến trước2027-01-01; timestamp theo quy tắc UTC+07.

ExportRequest: `{kind:"detail"|"list"|"report",invoiceId?,query?,formats:["xlsx","pdf"],locale:"vi"|"en",groupBy?,granularity?,metric?,dimensionId?,productLevel?}`. Detail cần invoiceId. Hai định dạng được dựng từ cùng dataset vật chất hóa một lần, trả cùng readAt; xuất list bỏ phân trang, giới hạn tối đa10.000 hóa đơn và không âm thầm cắt. XLSX dùng ClosedXML, PDF dùng PDFsharp/MigraDoc và font NotoSans local; tên file/MIME là file thật. Manifest lưu checksum, dataset, quyền và các invoice ID. Hóa đơn issued không có endpoint sửa/xóa; điều chỉnh dùng credit note và refund có nguồn.

## CMS native, preview và media

| Endpoint / command | Input | Quyền / hành vi |
| --- | --- | --- |
| GET `/api/admin/landing` | — | content_staff/owner: `{draft:{revision,sections,updated_at},history:[{id,versionNo,publishedAt,createdBy}]}` |
| `save_landing` | `{pageSlug:"home",revision,sections}` | content_staff; revision+1, không đổi public |
| `publish_landing` | `{pageSlug:"home",revision}` | content_staff; version immutable mới và revision+1 |
| `restore_landing` | `{pageSlug:"home",revision,versionId}` | content_staff; tạo version mới từ lịch sử, không xóa lịch sử |
| `preview_landing` | `{pageSlug?:"home",revision,viewport:"mobile"\|"laptop"}` | content_staff/owner; `{grantId,revision,expiresAt,url}`; grant10 phút gắn snapshot chính xác |
| POST `/api/landing/preview/redeem` | `{code}` | không cần actor, vẫn cần CSRF; code một lần → cookie HttpOnly `BotZone.Preview`, path `/api/landing/preview`, SameSite=Strict |
| GET `/api/landing/preview` | preview cookie | `{revision,sections,expiresAt}`; kiểm issuer còn quyền/hoạt động; không tạo auth session |
| `revoke_landing_preview` | `{grantId}` | issuer owner/content; revoke có hiệu lực lần đọc tiếp |
| POST `/api/media` | multipart `file,visibility,referenceType?,referenceId?,sourceUrl?,licenseNote?` | trả `{id,previewUrl,mime,sizeBytes,metadata}`; CSRF bắt buộc |
| GET `/api/media/{id}` | UUID | public ẩn danh; private cần role và quyền hồ sơ; checksum kiểm trước trả file |

Section có `{id,type,title:{vi,en},subtitle:{vi,en},productSlug,poster,model,colors,preset,camera,tilt,light,speed,fold}`; type `hero/duo/collection/benefits`, 1..20 sections, ID duy nhất, VI/EN1..500 ký tự. Camera2..12, tilt0..30, light0.2..5, speed0..3, fold0..180; preset `float/rotate/fold`, màu1..8 mã `#RRGGBB`. Model builtin `procedural:phone`/`procedural:duo` hoặc `/api/media/{uuid}` public đã validate; fold cần Hinge, đổi màu cần material phù hợp. Poster từ allowlist local hoặc public media; publish kiểm sản phẩm CTA active và đủ bản dịch published. Owner chỉ đọc, không save/publish/restore.

Upload public chỉ content_staff, cần nguồn `https/http/original:` và ghi chú license. Private cho customer đã xác minh/owner/order_staff/warehouse_staff và reference server kiểm chủ hồ sơ: `order,entitlement,trade_in,trade_in_inspection,return,warranty`. Ảnh JPEG/PNG/WebP tối đa5 MB,20 MP; GLB tối đa20 MB, tự chứa, không URI ngoài/decoder CDN. Không nhận SVG/HTML upload. Đây là validation cấu trúc/signature/dimensions; không tuyên bố giải mã đầy đủ JPEG/WebP. Khách có thể đọc ảnh staff tải lên thuộc hồ sơ của mình; không cần là người upload. Evidence request tối đa6 ảnh, truyền ID qua bảng liên kết, không dùng URL công khai.

## Lệnh hậu mãi

| Command | Input chính | Quyền |
| --- | --- | --- |
| `request_return` | `{orderId,reason:"defective"\|"wrong_item",items:[{orderItemId,unitIds,actualReceivedIdentifier?}],evidenceIds?}` | customer chủ đơn đã giao, trong cửa sổ snapshot |
| `approve_return_intake` | `{returnId,approve,reason}` | order_staff |
| `receive_return` | `{returnId,note}` | warehouse_staff |
| `inspect_return` | `{returnId,condition,eligible,note,evidenceIds?}` | warehouse_staff |
| `approve_return` | `{returnId,restock,reason}` | owner; tạo credit note nếu đủ điều kiện |
| `resolve_return` | `{returnId,trackingNo,note}` | order_staff; ghi gửi trả món bị từ chối, giữ state rejected |
| `execute_refund` | `{creditNoteId?,orderId?,outcome:"success"\|"failed"}` | owner, DemoMode; cần một nguồn; tiền theo phân bổ receipt gốc |
| `request_trade_in` | `{receptionMethod:"store"\|"remote",items:[{productId,variantId?,quantity,identifier?,condition:{...}}]}` | customer đã xác minh |
| `receive_trade_in` | `{requestId?,itemId?,note}` | warehouse_staff |
| `inspect_trade_in` | `{itemId,condition:{...},evidenceIds?}` | warehouse_staff; remote cần kiện đã đến và ảnh |
| `quote_trade_in` | `{requestId,expiresAt,items:[{itemId,offeredVnd}]}` | owner/order_staff; đủ món đã kiểm định, vượt/chưa có khung cần owner duyệt |
| `approve_trade_in_quote` | `{quoteId,reason}` | owner |
| `decide_trade_in` | `{quoteId,quoteItemId,decision:"accepted"\|"rejected"}` | customer chủ hồ sơ; phiên bản mới nhất/đã duyệt/chưa hết hạn |
| `finalize_trade_in` | `{itemId,quoteItemId}` | owner/order_staff; cấp store credit một lần sau khách chấp nhận |
| `trade_in_shipment` | `{shipmentId,status,note}` | order_staff/warehouse_staff; inbound delivered cần kho |
| `return_trade_in` | `{itemId,note}` | warehouse_staff; món bị từ chối tại store |
| `resolve_trade_in_exception` | `{requestId,note}` | owner |
| `request_warranty` | `{entitlementId,description,damageKind:"hardware"\|"accidental"\|"liquid",evidenceIds?}` | customer chủ quyền lợi; API kiểm ngày/phạm vi |
| `process_warranty` | `{claimId,action,note,replacementUnitId?,replacementLotId?,evidenceIds?}` | action `receive/close`: order_staff; `inspect/repair/repaired/unrepairable`: kho; `replace`: owner duyệt rồi kho bàn giao; `approve_replace/refund/reject`: owner |

Condition thu cũ chỉ nhận các trường `condition,battery,screen,components,notes,note,powersOn,activationLock,identifierMatches,hardware,grade`; không gửi mật khẩu. Return proof upload trước với `referenceType=order,referenceId=orderId`; warranty proof với `entitlement`; staff kiểm định thu cũ dùng `trade_in,requestId`. Lý do/ghi chú nghiệp vụ thường5..2.000 ký tự. Tiền refund/credit không do client tự đặt; server tính từ snapshot từng đơn vị và nguồn thanh toán.

## Lệnh hồ sơ, catalogue và vận hành

| Command | Input chính | Quyền |
| --- | --- | --- |
| `save_profile` | `{name,phone}` | customer |
| `save_address` | `{id?,recipient,phone,province,ward,addressLine,isDefault}` | customer, chỉ địa chỉ của mình |
| `sync_wishlist` | `{productIds:UUID[]}` | customer; tối đa100 |
| `read_notification` | `{id}` | actor nhận thông báo |
| `save_category` | `{id?,code,slug,nameVi,nameEn,active}` | owner |
| `create_product` | `{categoryId,slug,modelCode,generation?,sourceUrl,poster?,nameVi,nameEn}` | owner; sản phẩm và bản dịch ở draft |
| `update_product_status` | `{productId,businessStatus:"draft"\|"active"\|"discontinued"\|"archived"}` | owner |
| `create_variant` | `{productId,sku,options,priceVnd,trackingMode:"serialized"\|"quantity",careFeeVnd?,standardWarrantyMonths,active}` | owner |
| `update_variant` | `{variantId,priceVnd,active,careFeeVnd,standardWarrantyMonths}` | owner |
| `set_compatibility` | `{accessoryProductId,targetProductIds}` | owner; không tự tham chiếu |
| `update_translation` | `{productId,locale,name,description,seoTitle,contentStatus:"draft"\|"published"}` | content_staff |
| `create_promotion` | `{code,discountType:"fixed"\|"percentage",value,maxDiscountVnd?,minPurchaseVnd,startsAt,endsAt,allProducts,targets?,maxUses?,maxUsesPerCustomer?,active?}` | owner; tạo version mới; target chọn đúng một categoryId/productId/variantId |
| `update_staff` | `{userId,staffCode,role,active,reason}` | owner; giữ ít nhất một owner hoạt động, revoke session |
| `save_settings` / `update_settings` | `{key,value,reason}` | owner; whitelist `commerce,exchangeRateVnd,store,tradeIn`, lưu version |
| `save_supplier` | `{id?,code,name,contact,active?}` | owner/warehouse_staff |
| `create_purchase` | `{supplierId,items:[{variantId,quantity,unitCostVnd}]}` | owner/warehouse_staff; tạo draft |
| `approve_purchase` | `{purchaseId}` | owner |
| `receive_goods` | `{purchaseId,items:[{purchaseOrderItemId,quantity,serials?}]}` | warehouse_staff; nhập một phần, không vượt phần còn lại; serialized cần đúng số mã |
| `request_stock_adjustment` | `{lotId,quantityDelta,reason}` | warehouse_staff |
| `approve_stock_adjustment` | `{adjustmentId,approve,reason,inventoryUnitIds?}` | owner; không tự duyệt, không giảm dưới lượng đang giữ |

`commerce` setting có `shippingVnd,freeShippingVnd,pointsPerVnd,pointValueVnd,maxPointsPercent,returnDays`; chính sách mới không sửa snapshot tài chính cũ. Giá/chi phí dùng chuỗi VND, số lượng dùng integer. Archive/discontinue thay xóa dữ liệu đã được chứng từ tham chiếu.

## Read API cho WinForms

`GET /api/admin/overview` trả `demoMode,role` và KPI phù hợp role. `GET /api/admin/lookups` trả các ID/nhãn chọn theo scope: categories/products/variants, staff/customers, suppliers/lots/purchaseItems/inventoryUnits, tradeInItems/tradeInQuotes/quoteItems.

`GET /api/admin/data/{module}?q=&page=1&pageSize=20` trả `{rows,columns,totalCount,page,pageSize}`; pageSize1..100, q tối đa200. Module hiện có: `categories,catalogue,inventory,purchases,suppliers,orders,payments,returns,trade-in` (alias `tradeins,trade_ins`), `warranty,customers,promotions,staff,settings,audit,stock_adjustments,refunds,media`. Role được kiểm trong ReadService; kho không nhận giá vốn/giá trị tài chính, content không nhận sổ thu/hoàn. Audit của nhân viên chỉ nhóm thao tác phù hợp; owner đọc toàn bộ.

`GET /api/admin/customers/{userId}` chỉ dành cho owner/order_staff. Trả `{customer,balances,addresses,orders,creditEntries,loyaltyEntries,returns,tradeIns,claims,readAt,historyLimit:200}` trong một transaction đọc RepeatableRead. Balances có `credit_vnd,balance_points,net_spend_vnd,tier,held_credit_vnd,held_points,order_count`; ledger giữ delta có dấu và nguồn order/unit. Từng danh sách lịch sử giới hạn 200 bản ghi gần nhất, UI ghi rõ giới hạn. Không trả password hash, cookie, token, thông báo riêng hay wishlist của khách. Danh sách customers cũng có điểm/hạng/số dư/số đơn để tìm nhanh, chi tiết mở bằng form native và các tab bảng.

`GET /api/admin/reports/operations?kind=&from=2024-01-01&to=2027-01-01&q=` trả `{rows,columns,readAt,definition,fromInclusive,toExclusive}`. Kind: `inventory,margin,top_products,promotions,trade_in,warranty`. Inventory cho owner/kho (kho ẩn cost), promotions cho owner/order_staff; các loại còn lại owner. Date window kết thúc loại trừ, giới hạn30 năm; tối đa10.000 row. Inventory là ảnh chụp tồn hiện tại, không phải tồn lịch sử. `definition` diễn giải chỉ số; margin không phải lợi nhuận kế toán đầy đủ.

## Bằng chứng và giới hạn

Contract được đọc từ route/DTO/service .NET và đối chiếu kết quả live trong `artifacts/web` cùng các báo cáo kiểm thử C# của dự án. Schema thực và năm migration EF nằm tại [database/erd.md](database/erd.md), [database/data-dictionary.md](database/data-dictionary.md). Có endpoint sử dụng JsonElement và validation runtime; chưa có OpenAPI schema được công bố. Không giả định các trường chưa liệt kê là CRUD tự do hoặc quyền owner được bỏ qua state machine.
