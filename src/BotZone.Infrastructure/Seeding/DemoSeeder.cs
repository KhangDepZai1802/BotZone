using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace BotZone.Infrastructure.Seeding;

public sealed record SeedResult(string Profile, bool AlreadyApplied, IReadOnlyDictionary<string,Guid> InvoiceIds,
    IReadOnlyDictionary<string,Guid> ProductIds, string DemoPassword);

/// <summary>Additive, explicit development fixtures. No migration, reset, overwrite or production auto-run.</summary>
public sealed class DemoSeeder(BotZoneDbContext db)
{
    public const string Password = "BotZoneDemo!2026";
    public const string OwnerId = "demo-owner";
    public const string CustomerId = "demo-customer";
    public static Guid Id(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes("botzone:seed:v1.1:"+key)).AsSpan(0,16));
    private NpgsqlConnection Connection => (NpgsqlConnection)db.Database.GetDbConnection();
    private NpgsqlTransaction Transaction => (NpgsqlTransaction)db.Database.CurrentTransaction!.GetDbTransaction();
    private CancellationToken _ct;
    private sealed record Json(string Text);
    private static Json J(object value)=>new(JsonSerializer.Serialize(value));
    private static DateTime At(string value)=>DateTimeOffset.Parse(value,System.Globalization.CultureInfo.InvariantCulture).UtcDateTime;
    private static string Staff(int n)=>$"demo-nv{n:00}";
    private static readonly (string Code,string Category,string Name,string Source,string Poster,long Price,bool Serial)[] Catalog =
    [
        ("P13","iphone","iPhone 13","https://support.apple.com/en-us/111872","phone-blue.svg",8_000_000,true),
        ("P14","iphone","iPhone 14","https://support.apple.com/en-us/111850","phone-front.svg",12_000_000,true),
        ("AP2","airpods","AirPods Pro (2nd generation)","https://support.apple.com/en-us/111834","airpods.svg",1_500_000,false)
    ];

    public async Task<SeedResult> SeedAsync(string profile="demo",CancellationToken ct=default)
    {
        if(profile is not ("demo" or "golden" or "pagination" or "catalog"))throw new ArgumentException("Seed profile must be demo, golden, pagination or catalog.");
        _ct=ct;
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await Execute("SELECT pg_advisory_xact_lock(714811061331); SELECT set_config('botzone.actor_role','owner',true);");
        var marker=profile=="demo" ? "golden" : profile=="catalog" ? "base" : profile;
        var exists=await Scalar("SELECT EXISTS(SELECT 1 FROM app_settings WHERE key=@p0)","seed:v1.1:"+marker);
        var already=exists is true;
        await BaseAsync();
        if(!already && profile!="catalog")
        {
            if(marker=="golden")await GoldenAsync();
            else for(var n=1;n<=75;n++)await InvoiceAsync(new($"PAG{n:000}","2026-06-01T10:00:00+07:00",1,[new("AP2",1,100_000+n)],false));
            await Insert("app_settings",("key","seed:v1.1:"+marker),("value",J(new{version=1,profile=marker,appliedAt=DateTime.UtcNow})));
        }
        await tx.CommitAsync(ct);
        return new(profile,already,Enumerable.Range(1,8).ToDictionary(n=>$"G{n:00}",n=>Id($"invoice:G{n:00}")),Catalog.ToDictionary(p=>p.Code,p=>Id("product:"+p.Code)),Password);
    }

    private async Task BaseAsync()
    {
        if(await Scalar("SELECT EXISTS(SELECT 1 FROM app_settings WHERE key='seed:v1.1:base')") is true)return;
        var users=new (string Id,string Name,string Email,string? Role,string? Code)[]
        {
            (OwnerId,"Chủ cửa hàng BotZone","owner@botzone.local","owner","OWNER"),
            (Staff(1),"Nhân viên 01","nv01@botzone.local","order_staff","NV01"),
            (Staff(2),"Nhân viên 02","nv02@botzone.local","order_staff","NV02"),
            (Staff(3),"Nhân viên 03","nv03@botzone.local","order_staff","NV03"),
            ("demo-warehouse","Nhân viên kho","warehouse@botzone.local","warehouse_staff","KHO01"),
            ("demo-content","Biên tập viên","content@botzone.local","content_staff","CMS01"),
            (CustomerId,"Nguyễn Minh Anh","customer@botzone.local",null,null),
            ("demo-customer-other","Trần Khánh Linh","customer2@botzone.local",null,null)
        };
        foreach(var person in users)
        {
            var existing=await db.Users.SingleOrDefaultAsync(u=>u.Id==person.Id || u.NormalizedEmail==person.Email.ToUpperInvariant(),_ct);
            if(existing is not null)
            {
                if(existing.Id!=person.Id || !string.Equals(existing.Email,person.Email,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Demo identity collision; existing account was preserved.");
            }
            else
            {
                var user=new User{Id=person.Id,Name=person.Name,Email=person.Email,UserName=person.Email,NormalizedEmail=person.Email.ToUpperInvariant(),NormalizedUserName=person.Email.ToUpperInvariant(),EmailConfirmed=true,SecurityStamp=Guid.NewGuid().ToString(),ConcurrencyStamp=Guid.NewGuid().ToString(),LockoutEnabled=true,CreatedAt=DateTime.UtcNow,UpdatedAt=DateTime.UtcNow,PasswordResetRequired=false};
                user.PasswordHash=new PasswordHasher<User>().HashPassword(user,Password);
                db.Users.Add(user);await db.SaveChangesAsync(_ct);
            }
            if(person.Role is not null)await Insert("staff_profiles",("user_id",person.Id),("staff_code",person.Code),("role",person.Role),("active",person.Id!=Staff(3)));
            else
            {
                await Insert("customer_profiles",("user_id",person.Id),("display_name",person.Name),("phone","0901234567"));
                await Insert("store_credit_accounts",("user_id",person.Id));
                await Insert("loyalty_accounts",("user_id",person.Id));
                await Insert("addresses",("id",Id("address:"+person.Id)),("user_id",person.Id),("recipient",person.Name),("phone","0901234567"),("province","Hồ Chí Minh"),("ward","Phường Sài Gòn"),("address_line","123 Đường Nguyễn Huệ"),("is_default",true));
            }
        }
        foreach(var category in new (string Slug,string Vi,string En)[]{("iphone","iPhone","iPhone"),("mac","Mac","Mac"),("ipad","iPad","iPad"),("watch","Apple Watch","Apple Watch"),("airpods","AirPods","AirPods"),("vision","Apple Vision","Apple Vision"),("tv-home","TV & Nhà","TV & Home"),("accessories","Phụ kiện","Accessories"),("displays","Màn hình","Displays")})
            await Insert("categories",("id",Id("category:"+category.Slug)),("code",category.Slug.ToUpperInvariant()),("slug",category.Slug),("name_vi",category.Vi),("name_en",category.En));
        await Insert("warehouses",("id",Id("warehouse")),("code","MAIN"),("name","Kho chính BotZone"));
        await Insert("suppliers",("id",Id("supplier")),("code","DEMO-SUPPLIER"),("name","Nhà cung cấp mô phỏng BotZone"),("contact","supplier@botzone.local"));
        await Insert("purchase_orders",("id",Id("po")),("supplier_id",Id("supplier")),("code","DEMO-PO-001"),("status","received"),("created_by",OwnerId),("approved_by",OwnerId),("created_at",At("2024-01-01T00:00:00Z")));
        await Insert("goods_receipts",("id",Id("receipt")),("purchase_order_id",Id("po")),("receipt_no","DEMO-GR-001"),("received_by","demo-warehouse"),("received_at",At("2024-01-02T00:00:00Z")),("event_key","seed:goods-receipt"));
        foreach(var product in Catalog)
        {
            var p=Id("product:"+product.Code);var v=Id("sku:"+product.Code);var qty=product.Serial?30:200;
            await Insert("products",("id",p),("category_id",Id("category:"+product.Category)),("slug",product.Code.ToLowerInvariant()),("model_code",product.Code),("source_url",product.Source),("poster","/media/"+product.Poster),("generation",product.Code=="P13"?"2021": "2022"),("specs",J(new{fixture=true,priceDisclosure="Giá mô phỏng phục vụ kiểm thử",sourceCheckedAt="2026-11-13"})));
            foreach(var locale in new[]{"vi","en"})await Insert("product_translations",("product_id",p),("locale",locale),("name",product.Name),("description",locale=="vi"?"Sản phẩm trong bộ dữ liệu lịch sử kiểm thử BotZone. Giá bán mô phỏng.":"BotZone historical test catalog. Simulated retail price."),("seo_title",product.Name+" | BotZone"));
            await Insert("product_variants",("id",v),("product_id",p),("sku",product.Code+"-DEMO"),("option_signature","demo-standard"),("options",J(product.Serial?new Dictionary<string,string>{{"storage","128GB"},{"color","Midnight"}}:new Dictionary<string,string>{{"color","White"}})),("price_vnd",product.Price),("tracking_mode",product.Serial?"serialized":"quantity"),("care_fee_vnd",product.Serial?500_000L:null));
            await Insert("purchase_order_items",("id",Id("po-item:"+product.Code)),("purchase_order_id",Id("po")),("variant_id",v),("quantity",qty),("unit_cost_vnd",product.Price/2));
            await Insert("goods_receipt_items",("id",Id("receipt-item:"+product.Code)),("receipt_id",Id("receipt")),("purchase_order_id",Id("po")),("purchase_order_item_id",Id("po-item:"+product.Code)),("received_qty",qty),("unit_cost_vnd",product.Price/2));
            await Insert("inventory_lots",("id",Id("lot:"+product.Code)),("warehouse_id",Id("warehouse")),("variant_id",v),("receipt_item_id",Id("receipt-item:"+product.Code)),("received_at",At("2024-01-02T00:00:00Z")),("unit_cost_vnd",product.Price/2),("qty_on_hand",qty));
            if(product.Serial)for(var n=1;n<=qty;n++)
            {
                var unit=Id($"stock:{product.Code}:{n}");
                await Insert("inventory_units",("id",unit),("lot_id",Id("lot:"+product.Code)));
                await Insert("inventory_identifiers",("id",Id($"serial:{product.Code}:{n}")),("inventory_unit_id",unit),("kind","serial"),("normalized_value",$"000{product.Code}{n:000000000000}"));
            }
            await Insert("stock_movements",("id",Id("stock-receive:"+product.Code)),("variant_id",v),("lot_id",Id("lot:"+product.Code)),("quantity_delta",qty),("kind","receive"),("source_event_key","seed:receive:"+product.Code),("actor_user_id","demo-warehouse"),("created_at",At("2024-01-02T00:00:00Z")));
        }
        await Insert("warranty_plans",("id",Id("care-plan")),("code","BOTZONE-CARE"));
        await Insert("warranty_plan_versions",("id",Id("care-v1")),("plan_id",Id("care-plan")),("version_no",1),("term_months",12),("fee_vnd",500_000L),("policy_snapshot",J(new{hardware=true,accidental=false,liquid=false,price="snapshot_per_order"})));
        await Insert("promotions",("id",Id("promo")),("code_normalized","DEMO1M"),("max_uses",100),("max_uses_per_customer",10));
        await Insert("promotion_versions",("id",Id("promo-v1")),("promotion_id",Id("promo")),("version_no",1),("discount_type","fixed"),("value",1_000_000L),("starts_at",At("2026-01-01T00:00:00+07:00")),("ends_at",At("2030-01-01T00:00:00+07:00")),("all_products",false));
        await Insert("promotion_targets",("id",Id("promo-target")),("promotion_version_id",Id("promo-v1")),("product_id",Id("product:P13")));
        await Insert("app_settings",("key","commerce"),("value",J(new{shippingVnd="30000",freeShippingVnd="2000000",pointsPerVnd="100000",pointValueVnd="1000",maxPointsPercent=5,returnDays=14})));
        await Insert("app_settings",("key","exchangeRateVnd"),("value",J("25000")));
        await Insert("app_settings",("key","seed:v1.1:base"),("value",J(new{version=1})));
    }

    private sealed record Line(string Code,int Quantity,long Price,long Voucher=0,string Type="product",int? Parent=null);
    private sealed record Fixture(string No,string Date,int Staff,Line[] Lines,bool Paid=true,bool FailedAttempt=false,bool Mixed=false,bool Draft=false,bool Cancelled=false,string? Delivered=null);
    private async Task GoldenAsync()
    {
        Fixture[] fixtures =
        [
            new("G01","2024-01-15T10:00:00+07:00",1,[new("P13",1,10_000_000),new("AP2",1,2_000_000)],FailedAttempt:true),
            new("G02","2024-11-20T14:00:00+07:00",2,[new("P14",1,15_000_000),new("CARE",1,1_000_000,Type:"warranty",Parent:1)],Paid:false),
            new("G03","2025-01-10T09:00:00+07:00",1,[new("P13",2,9_000_000)]),
            new("G04","2025-04-12T11:00:00+07:00",2,[new("P14",1,14_000_000),new("AP2",2,2_000_000)],FailedAttempt:true,Delivered:"2025-04-20T09:00:00+07:00"),
            new("G05","2025-12-31T23:59:00+07:00",3,[new("P13",1,8_000_000)]),
            new("G06","2026-01-01T00:01:00+07:00",3,[new("AP2",1,1_800_000),new("SHIP",1,30_000,Type:"shipping")]),
            new("G07","2026-04-05T15:00:00+07:00",1,[new("P14",1,12_000_000),new("CARE",1,800_000,Type:"warranty",Parent:1)]),
            new("G08","2026-11-01T10:00:00+07:00",2,[new("P13",1,8_000_000,1_000_000),new("AP2",2,1_500_000),new("CARE",1,500_000,Type:"warranty",Parent:1)],Mixed:true,Delivered:"2026-11-02T09:00:00+07:00"),
            new("D01","2026-11-01T10:00:00+07:00",1,[new("P13",1,99_000_000)],Paid:false,Draft:true),
            new("CANCELLED01","2026-11-01T10:00:00+07:00",1,[new("AP2",1,1_500_000)],Paid:false,Cancelled:true)
        ];
        await TradeInAsync();
        foreach(var fixture in fixtures)await InvoiceAsync(fixture);
        await ReturnAsync("CN01","G04",2,2_000_000,"2025-05-01T09:00:00+07:00","2025-05-02T10:00:00+07:00",false);
        await ReturnAsync("CN02","G08",2,1_500_000,"2026-11-12T09:00:00+07:00","2026-11-13T10:30:00+07:00",true);
        // Both golden returns occur inside the original return window. Only the
        // remaining eligible product spend may earn points after that window.
        foreach(var fixture in fixtures.Where(f=>f.Delivered is not null&&f.Paid))
        {
            var eligibleAt=At(fixture.Delivered!).AddDays(14);if(eligibleAt>DateTime.UtcNow)continue;
            var oid=Id("order:"+fixture.No);
            var productSpend=Convert.ToInt64(await Scalar("SELECT COALESCE(sum(i.net_amount_vnd),0)-(SELECT COALESCE(sum(ci.amount_vnd),0) FROM credit_note_items ci JOIN credit_notes c ON c.id=ci.credit_note_id JOIN invoice_items ii ON ii.id=ci.invoice_item_id WHERE c.order_id=@p0 AND c.status='issued' AND ii.line_type='product') FROM order_items i WHERE i.order_id=@p0 AND i.line_type='product'",oid));
            var points=checked((int)(productSpend/100_000));if(points>0)await Insert("loyalty_entries",("id",Id("earn:"+fixture.No)),("user_id",CustomerId),("delta",points),("kind","earn"),("order_id",oid),("event_key","seed:earn:"+fixture.No),("created_at",eligibleAt));
            await Execute("UPDATE loyalty_accounts SET balance_points=balance_points+@p0,net_spend_vnd=net_spend_vnd+@p1 WHERE user_id=@p2",points,productSpend,CustomerId);
        }
    }

    private async Task InvoiceAsync(Fixture fixture)
    {
        var oid=Id("order:"+fixture.No);var iid=Id("invoice:"+fixture.No);var date=At(fixture.Date);var subtotal=fixture.Lines.Sum(l=>checked(l.Price*l.Quantity));var discount=fixture.Lines.Sum(l=>l.Voucher);var total=subtotal-discount;
        var customer=J(new{name="Nguyễn Minh Anh",email="customer@botzone.local",phone="0901234567"});
        var address=J(new{recipient="Nguyễn Minh Anh",phone="0901234567",province="Hồ Chí Minh",ward="Phường Sài Gòn",addressLine="123 Đường Nguyễn Huệ",country="VN"});
        await Insert("orders",("id",oid),("order_no","DEMO-"+fixture.No),("customer_id",CustomerId),("responsible_staff_id",Staff(fixture.Staff)),("order_status","building"),("payment_method",fixture.Paid?"banking":"cod"),("address_snapshot",address),("total_vnd",total),("policy_snapshot",J(new{returnDays=14,pointsPerVnd="100000",fixture="BA-v1.1"})),("expires_at",date.AddDays(1)),("created_at",date.AddHours(-1)),("delivered_at",fixture.Delivered is null?null:At(fixture.Delivered)));
        var units=new List<(Guid Id,long Cash,long Credit)>();
        for(var index=0;index<fixture.Lines.Length;index++)
        {
            var line=fixture.Lines[index];var n=index+1;var item=Id($"item:{fixture.No}:{n}");var variant=line.Type=="product"?Id("sku:"+line.Code):(Guid?)null;var product=line.Type=="product"?Id("product:"+line.Code):(Guid?)null;
            var name=line.Type=="product"?Catalog.Single(p=>p.Code==line.Code).Name:line.Type=="warranty"?"BotZone Care · 12 tháng":"Phí giao hàng Việt Nam";
            var options=J(line.Type=="product"?new{fixture=true,sku=line.Code+"-DEMO"}:new{fixture=true,sku=line.Code});
            await Insert("order_items",("id",item),("order_id",oid),("line_no",n),("line_type",line.Type),("product_id",product),("variant_id",variant),("parent_item_id",line.Parent is int parent?Id($"item:{fixture.No}:{parent}"):null),("quantity",line.Quantity),("unit_price_vnd",line.Price),("voucher_discount_vnd",line.Voucher),("net_amount_vnd",line.Price*line.Quantity-line.Voucher),("snapshot",J(new{name,sku=line.Code+"-DEMO",standardWarrantyMonths=12,options=new{fixture=true}})));
            for(var ordinal=1;ordinal<=line.Quantity;ordinal++)
            {
                var unit=Id($"unit:{fixture.No}:{n}:{ordinal}");var voucher=line.Voucher/line.Quantity+(ordinal<=line.Voucher%line.Quantity?1:0);var net=line.Price-voucher;var credit=fixture.Mixed?net*3/10:0;
                await Insert("order_item_unit_amounts",("id",unit),("order_item_id",item),("unit_ordinal",ordinal),("parent_unit_amount_id",line.Parent is int parentNo?Id($"unit:{fixture.No}:{parentNo}:{ordinal}"):null),("gross_vnd",line.Price),("voucher_discount_vnd",voucher),("points_discount_vnd",0L),("net_amount_vnd",net),("credit_planned_vnd",credit),("cash_planned_vnd",net-credit));
                units.Add((unit,net-credit,credit));
                if(line.Type=="product" && !fixture.Draft && !fixture.Cancelled)
                {
                    var lot=Id("lot:"+line.Code);var stock=Catalog.Single(p=>p.Code==line.Code);
                    var physical=stock.Serial?(Guid?)await Scalar("SELECT id FROM inventory_units WHERE lot_id=@p0 AND custody_status='available' ORDER BY id LIMIT 1 FOR UPDATE",lot):null;
                    if(stock.Serial && physical is null)throw new InvalidOperationException("Demo stock exhausted; existing data was preserved.");
                    if(physical is not null)await Execute("UPDATE inventory_units SET custody_status='shipped' WHERE id=@p0",physical);
                    await Execute("UPDATE inventory_lots SET qty_on_hand=qty_on_hand-1 WHERE id=@p0",lot);
                    await Insert("inventory_allocations",("id",Id("allocation:"+unit)),("order_item_id",item),("order_item_unit_amount_id",unit),("lot_id",lot),("inventory_unit_id",physical),("quantity",1),("cost_vnd",stock.Price/2),("state","shipped"));
                    await Insert("stock_movements",("id",Id("ship:"+unit)),("variant_id",variant),("lot_id",lot),("unit_id",physical),("quantity_delta",-1),("kind","ship"),("source_event_key","seed:ship:"+unit),("actor_user_id","demo-warehouse"),("created_at",date));
                }
                if(line.Type=="warranty")
                {
                    var covered=Id($"unit:{fixture.No}:{line.Parent}:{ordinal}");
                    var physical=await Scalar("SELECT inventory_unit_id FROM inventory_allocations WHERE order_item_unit_amount_id=@p0 AND state='shipped'",covered);
                    await Insert("warranty_plans",("id",Id("care-plan:"+line.Price)),("code","DEMO-CARE-"+line.Price));
                    await Insert("warranty_plan_versions",("id",Id("care-version:"+line.Price)),("plan_id",Id("care-plan:"+line.Price)),("version_no",1),("term_months",12),("fee_vnd",line.Price),("policy_snapshot",J(new{coverage="hardware",exclusions=new[]{"liquid","accidental"},extendsStandardWarranty=true})));
                    await Insert("warranty_entitlements",("id",Id("entitlement:"+unit)),("order_item_unit_amount_id",covered),("inventory_unit_id",physical),("plan_version_id",Id("care-version:"+line.Price)),("state",fixture.Delivered is null?"pending":"active"),("starts_on",fixture.Delivered is null?null:DateOnly.FromDateTime(At(fixture.Delivered).AddHours(7)).AddYears(1)),("ends_on",fixture.Delivered is null?null:DateOnly.FromDateTime(At(fixture.Delivered).AddHours(7)).AddYears(2)));
                }
            }
        }
        await Execute("UPDATE orders SET order_status=@p0 WHERE id=@p1",fixture.Cancelled?"cancelled":fixture.Draft?"pending":fixture.Delivered is null?"handed_off":"completed",oid);
        if(fixture.Cancelled)return;
        await Insert("invoices",("id",iid),("order_id",oid),("lifecycle","draft"),("customer_id",CustomerId),("responsible_staff_id",Staff(fixture.Staff)),("subtotal_vnd",subtotal),("discount_total_vnd",discount),("grand_total_vnd",total),("customer_snapshot",customer),("staff_snapshot",J(new{name=$"Nhân viên {fixture.Staff:00}",staffCode=$"NV{fixture.Staff:00}"})));
        for(var index=0;index<fixture.Lines.Length;index++)
        {
            var line=fixture.Lines[index];var n=index+1;
            await Execute("INSERT INTO invoice_items(id,invoice_id,order_id,order_item_id,line_no,line_type,product_id,variant_id,parent_invoice_item_id,sku_snapshot,name_snapshot,variant_snapshot,quantity,unit_price_vnd,voucher_discount_vnd,points_discount_vnd,net_amount_vnd) SELECT @p0,@p1,order_id,id,line_no,line_type,product_id,variant_id,@p2,snapshot->>'sku',snapshot->>'name',snapshot->'options',quantity,unit_price_vnd,voucher_discount_vnd,points_discount_vnd,net_amount_vnd FROM order_items WHERE id=@p3",Id($"invoice-item:{fixture.No}:{n}"),iid,line.Parent is int parent?Id($"invoice-item:{fixture.No}:{parent}"):null,Id($"item:{fixture.No}:{n}"));
        }
        if(fixture.Draft)return;
        await Execute("UPDATE invoices SET lifecycle='issued',invoice_no=@p0,issued_at=@p1,issued_by_user_id=@p2 WHERE id=@p3",fixture.No,date,"demo-warehouse",iid);
        await Insert("shipments",("id",Id("shipment:"+fixture.No)),("order_id",oid),("tracking_no","DEMO-SHIP-"+fixture.No),("status",fixture.Delivered is null?"handed_off":"delivered"),("created_at",date));
        await Insert("shipment_events",("id",Id("shipment-event:"+fixture.No)),("shipment_id",Id("shipment:"+fixture.No)),("status","handed_off"),("occurred_at",date),("event_key","seed:handoff:"+fixture.No));
        if(fixture.Delivered is not null)await Insert("shipment_events",("id",Id("delivery-event:"+fixture.No)),("shipment_id",Id("shipment:"+fixture.No)),("status","delivered"),("occurred_at",At(fixture.Delivered)),("event_key","seed:delivery:"+fixture.No));
        if(discount>0)
        {
            await Insert("promotion_redemptions",("id",Id("redemption:"+fixture.No)),("promotion_id",Id("promo")),("promotion_version_id",Id("promo-v1")),("customer_id",CustomerId),("order_id",oid),("status","consumed"),("amount_vnd",discount),("expires_at",date.AddDays(1)),("idempotency_key","seed:promo:"+fixture.No));
            await Insert("order_adjustments",("id",Id("adjustment:"+fixture.No)),("order_item_id",Id($"item:{fixture.No}:1")),("kind","voucher"),("promotion_redemption_id",Id("redemption:"+fixture.No)),("amount_vnd",discount),("source_event_key","seed:voucher:"+fixture.No));
        }
        if(!fixture.Paid)return;
        if(fixture.FailedAttempt)await Insert("payment_attempts",("id",Id("failed-attempt:"+fixture.No)),("order_id",oid),("method","banking"),("status","failed"),("requested_amount_vnd",total),("request_key","seed:failed:"+fixture.No),("created_at",date.AddMinutes(-30)));
        await Insert("payment_attempts",("id",Id("attempt:"+fixture.No)),("order_id",oid),("method","banking"),("status","success"),("requested_amount_vnd",units.Sum(u=>u.Cash)),("request_key","seed:payment:"+fixture.No),("created_at",date.AddMinutes(-10)));
        foreach(var kind in new[]{"cash_demo","store_credit"})
        {
            var amount=units.Sum(u=>kind=="cash_demo"?u.Cash:u.Credit);if(amount==0)continue;var rid=Id($"payment:{fixture.No}:{kind}");
            await Insert("payment_receipts",("id",rid),("order_id",oid),("payment_attempt_id",kind=="cash_demo"?Id("attempt:"+fixture.No):null),("tender_kind",kind),("amount_vnd",amount),("received_at",date),("source_event_key",$"seed:receipt:{fixture.No}:{kind}"));
            foreach(var unit in units){var part=kind=="cash_demo"?unit.Cash:unit.Credit;if(part>0)await Insert("order_tender_allocations",("id",Id("tender:"+rid+":"+unit.Id)),("order_item_unit_amount_id",unit.Id),("receipt_id",rid),("amount_vnd",part));}
        }
        if(fixture.Mixed)
        {
            await Insert("store_credit_entries",("id",Id("spend:"+fixture.No)),("user_id",CustomerId),("delta_vnd",-units.Sum(u=>u.Credit)),("kind","spend"),("order_id",oid),("event_key","seed:spend:"+fixture.No),("created_at",date));
            await Execute("UPDATE store_credit_accounts SET balance_vnd=balance_vnd-@p0 WHERE user_id=@p1",units.Sum(u=>u.Credit),CustomerId);
        }
    }

    private async Task TradeInAsync()
    {
        await Insert("trade_in_requests",("id",Id("trade-in")),("customer_id",CustomerId),("reception_method","store"),("state","completed"),("created_at",At("2026-10-30T09:00:00+07:00")));
        await Insert("trade_in_items",("id",Id("trade-in-item")),("request_id",Id("trade-in")),("product_id",Id("product:P13")),("variant_id",Id("sku:P13")),("qty",1),("identifier","000TRADEINP13001"),("internal_code","DEMO-TI-001"),("condition_snapshot",J(new{screen="used",powersOn=true})),("state","completed"));
        await Insert("trade_in_inspections",("id",Id("trade-in-inspection")),("item_id",Id("trade-in-item")),("inspector_staff_id","demo-warehouse"),("checklist_version",1),("assessed_condition",J(new{activationLock=false,identifierMatches=true,hardware="passed",grade="B"})),("inspected_at",At("2026-10-30T10:00:00+07:00")));
        await Insert("trade_in_quotes",("id",Id("trade-in-quote")),("request_id",Id("trade-in")),("version_no",1),("expires_at",At("2026-11-02T10:00:00+07:00")),("quoted_by",Staff(1)),("approved_by",OwnerId));
        await Insert("trade_in_quote_items",("id",Id("trade-in-quote-item")),("quote_id",Id("trade-in-quote")),("request_id",Id("trade-in")),("item_id",Id("trade-in-item")),("offered_vnd",3_150_000L),("decision","accepted"),("decided_at",At("2026-10-30T11:00:00+07:00")));
        await Insert("store_credit_entries",("id",Id("trade-in-credit")),("user_id",CustomerId),("delta_vnd",3_150_000L),("kind","trade_in"),("trade_in_item_id",Id("trade-in-item")),("quote_item_id",Id("trade-in-quote-item")),("event_key","seed:trade-in-credit"),("created_at",At("2026-10-30T11:01:00+07:00")));
        await Execute("UPDATE store_credit_accounts SET balance_vnd=balance_vnd+3150000 WHERE user_id=@p0",CustomerId);
    }

    private async Task ReturnAsync(string code,string invoice,int line,long amount,string requested,string issued,bool mixed)
    {
        var oid=Id("order:"+invoice);var rr=Id("return:"+code);var ri=Id("return-item:"+code);var item=Id($"item:{invoice}:{line}");var unit=Id($"unit:{invoice}:{line}:1");var cn=Id("credit-note:"+code);var cni=Id("credit-note-item:"+code);var refund=Id("refund:"+code);var date=At(issued);
        await Insert("return_requests",("id",rr),("order_id",oid),("customer_id",CustomerId),("state","credited"),("reason","defective"),("created_at",At(requested)),("inspection_result","Lỗi phần cứng đã kiểm tra; hoàn về nguồn thanh toán."),("inspected_by","demo-warehouse"),("inspected_at",date.AddMinutes(-30)),("fee_vnd",0L));
        await Insert("return_items",("id",ri),("return_request_id",rr),("order_id",oid),("order_item_id",item),("qty",1));
        await Insert("return_item_units",("return_item_id",ri),("order_item_id",item),("order_item_unit_amount_id",unit));
        await Execute("UPDATE inventory_allocations SET state='returned' WHERE order_item_unit_amount_id=@p0",unit);
        await Insert("credit_notes",("id",cn),("credit_note_no",code),("invoice_id",Id("invoice:"+invoice)),("order_id",oid),("status","draft"),("return_request_id",rr),("reason","Đổi trả lỗi phần cứng"),("total_vnd",amount),("approved_by_user_id",OwnerId),("event_key","seed:credit-note:"+code));
        await Insert("credit_note_items",("id",cni),("credit_note_id",cn),("invoice_item_id",Id($"invoice-item:{invoice}:{line}")),("quantity",1),("amount_vnd",amount));
        await Insert("credit_note_item_units",("credit_note_item_id",cni),("order_item_unit_amount_id",unit),("amount_vnd",amount));
        await Execute("UPDATE credit_notes SET status='issued',issued_at=@p0 WHERE id=@p1",date,cn);
        await Insert("refunds",("id",refund),("order_id",oid),("credit_note_id",cn),("status","completed"),("amount_vnd",amount),("approved_by",OwnerId),("completed_at",date.AddMinutes(5)),("idempotency_key","seed:refund:"+code));
        foreach(var kind in new[]{"cash_demo","store_credit"})
        {
            var credit=mixed?amount*3/10:0;var part=kind=="cash_demo"?amount-credit:credit;if(part==0)continue;
            await Insert("refund_allocations",("id",Id($"refund-part:{code}:{kind}")),("refund_id",refund),("payment_receipt_id",Id($"payment:{invoice}:{kind}")),("order_item_unit_amount_id",unit),("amount_vnd",part));
            if(kind=="store_credit")
            {
                await Insert("store_credit_entries",("id",Id("refund-credit:"+code)),("user_id",CustomerId),("delta_vnd",credit),("kind","refund"),("order_id",oid),("refund_id",refund),("event_key","seed:credit-refund:"+code),("created_at",date.AddMinutes(5)));
                await Execute("UPDATE store_credit_accounts SET balance_vnd=balance_vnd+@p0 WHERE user_id=@p1",credit,CustomerId);
            }
        }
    }

    // Every identifier below is supplied only by this source file; all values are parameters.
    private async Task Insert(string table,params (string Name,object? Value)[] fields)
    {
        var sql=$"INSERT INTO \"{table}\" ({string.Join(',',fields.Select(f=>$"\"{f.Name}\""))}) VALUES ({string.Join(',',fields.Select((_,n)=>"@p"+n))}) ON CONFLICT DO NOTHING";
        await Execute(sql,fields.Select(f=>f.Value).ToArray());
    }
    private NpgsqlCommand Command(string sql,object?[] values)
    {
        var cmd=new NpgsqlCommand(sql,Connection,Transaction);
        for(var n=0;n<values.Length;n++)
        {
            if(values[n] is Json json)cmd.Parameters.Add(new NpgsqlParameter("p"+n,NpgsqlDbType.Jsonb){Value=json.Text});
            else cmd.Parameters.AddWithValue("p"+n,values[n]??DBNull.Value);
        }
        return cmd;
    }
    private async Task Execute(string sql,params object?[] values){await using var command=Command(sql,values);await command.ExecuteNonQueryAsync(_ct);}
    private async Task<object?> Scalar(string sql,params object?[] values){await using var command=Command(sql,values);var value=await command.ExecuteScalarAsync(_ct);return value is DBNull?null:value;}
}
