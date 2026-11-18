using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BotZone.Contracts;
using Microsoft.Extensions.Configuration;

namespace BotZone.Infrastructure.Services;

public sealed record PreviewSession(string Token, DateTimeOffset ExpiresAt);

/// <summary>CMS commands run inside the shared command transaction/idempotency boundary.</summary>
public sealed class LandingService(SqlStore sql, IConfiguration configuration, MediaService media)
{
    private static readonly HashSet<string> SectionFields = ["id","type","title","subtitle","productSlug","poster","model","colors","preset","camera","tilt","light","speed","fold"];
    private static readonly HashSet<string> TrustedPosters = ["accessories.svg","airpods.svg","display.svg","duo.svg","home.svg","ipad.svg","mac.svg","phone-blue.svg","phone-burgundy.svg","phone-front.svg","vision.svg","watch.svg"];
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string Secret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
    private static void Valid(bool value,string message) => Access.Ensure(value,"VALIDATION_ERROR",message,400);
    private static void Fields(JsonElement value,params string[] allowed) { Valid(value.ValueKind==JsonValueKind.Object,"Dữ liệu phải là object.");Valid(value.EnumerateObject().All(p=>allowed.Contains(p.Name)),"Có trường không được phép trong cấu hình landing."); }
    private static int Revision(JsonElement input) { var value=input.Field("revision");Valid(value.ValueKind==JsonValueKind.Number&&value.TryGetInt32(out var n)&&n>=0,"Revision không hợp lệ.");return value.GetInt32(); }
    private static string Page(JsonElement input) {var page=input.Text("pageSlug","home");Valid(Regex.IsMatch(page,"^[a-z0-9-]{1,80}$"),"Slug trang không hợp lệ.");return page;}

    public async Task<object> AdminLanding(Actor actor)
    {
        Access.Require(actor,"content_staff","owner");
        return new { draft=await sql.One("SELECT d.revision,d.sections,d.updated_at FROM landing_drafts d JOIN landing_pages p ON p.id=d.page_id WHERE p.slug='home'"),
            history=await sql.Rows("SELECT v.id,v.version_no AS \"versionNo\",v.created_at AS \"publishedAt\",u.name AS \"createdBy\" FROM landing_versions v JOIN landing_pages p ON p.id=v.page_id JOIN \"user\" u ON u.id=v.created_by WHERE p.slug='home' ORDER BY v.version_no DESC LIMIT 100") };
    }

    public async Task<object?> Handle(Actor actor,string command,JsonElement input,string key)
    {
        if(command is not ("save_landing" or "publish_landing" or "restore_landing" or "preview_landing" or "revoke_landing_preview"))return null;
        if(command=="preview_landing")return await IssuePreview(actor,input);
        if(command=="revoke_landing_preview")
        {
            Access.Require(actor,"content_staff","owner");Fields(input,"grantId");var id=input.Id("grantId");
            var changed=await sql.Exec("UPDATE landing_preview_grants SET revoked_at=COALESCE(revoked_at,now()) WHERE id=$1 AND issued_by=$2",id,actor.Id);
            Access.Ensure(changed==1,"NOT_FOUND","Không tìm thấy quyền xem trước của bạn.",404);return new {grantId=id,revoked=true};
        }
        Access.Require(actor,"content_staff");
        Fields(input,command=="save_landing"?["pageSlug","revision","sections"]:command=="restore_landing"?["pageSlug","revision","versionId"]:["pageSlug","revision"]);
        var revision=Revision(input);var page=Page(input);
        var draft=await sql.One("SELECT d.* FROM landing_drafts d JOIN landing_pages p ON p.id=d.page_id WHERE p.slug=$1 FOR UPDATE OF d",page);
        Access.Ensure(draft.I("revision")==revision,"STATE_CONFLICT","Bản nháp đã thay đổi. Tải lại trước khi lưu hoặc xuất bản.");
        var sections=command=="save_landing"?input.Field("sections"):draft.J("sections");
        if(command=="restore_landing")sections=(await sql.One("SELECT sections FROM landing_versions WHERE id=$1 AND page_id=$2",input.Id("versionId"),draft.G("page_id"))).J("sections");
        var assetIds=await ValidateSections(sections,command!="save_landing");
        if(command=="save_landing")
        {
            await sql.Exec("UPDATE landing_drafts SET sections=$2::jsonb,revision=revision+1,updated_by=$3,updated_at=now() WHERE page_id=$1",draft.G("page_id"),sections.GetRawText(),actor.Id);
            return new {revision=revision+1,sections};
        }
        var version=await sql.One("INSERT INTO landing_versions(page_id,version_no,sections,created_by) SELECT $1,COALESCE(max(version_no),0)+1,$2::jsonb,$3 FROM landing_versions WHERE page_id=$1 RETURNING id,version_no",draft.G("page_id"),sections.GetRawText(),actor.Id);
        foreach(var id in assetIds)await sql.Exec("INSERT INTO landing_media_refs(landing_version_id,media_id) VALUES($1,$2)",version.G("id"),id);
        await sql.Exec("INSERT INTO landing_publications(page_id,version_id,published_by) VALUES($1,$2,$3)",draft.G("page_id"),version.G("id"),actor.Id);
        await sql.Exec("UPDATE landing_drafts SET sections=$2::jsonb,revision=revision+1,updated_by=$3,updated_at=now() WHERE page_id=$1",draft.G("page_id"),sections.GetRawText(),actor.Id);
        return new {versionId=version.G("id"),version=version.I("version_no"),revision=revision+1};
    }

    private async Task<HashSet<Guid>> ValidateSections(JsonElement sections,bool publishing)
    {
        Valid(sections.ValueKind==JsonValueKind.Array&&sections.GetArrayLength() is >=1 and <=20,"Landing cần từ 1 đến 20 section.");
        Valid(sections.GetRawText().Length<=100000,"Cấu hình landing quá lớn.");var ids=new HashSet<string>();var assets=new HashSet<Guid>();
        foreach(var section in sections.EnumerateArray())
        {
            Fields(section,SectionFields.ToArray());var id=section.Text("id");Valid(id.Length is >=1 and <=80&&ids.Add(id),"Mã section rỗng, quá dài hoặc trùng.");
            var type=section.Text("type");Valid(type is "hero" or "duo" or "collection" or "benefits","Loại section không hợp lệ.");
            foreach(var field in new[]{"title","subtitle"}) {var text=section.Field(field);Fields(text,"vi","en");foreach(var locale in new[]{"vi","en"})Valid(text.Text(locale).Trim().Length is >=1 and <=500,"Mỗi section cần nội dung VI/EN dài 1–500 ký tự.");}
            var slug=section.Text("productSlug");Valid(slug.Length<=160&&(!string.IsNullOrWhiteSpace(slug)||type is not("hero" or "duo")),"Hero cần chọn sản phẩm.");
            if(slug.Length>0)
            {
                var product=await sql.One("SELECT p.id,p.business_status,(SELECT count(*)::int FROM product_translations t WHERE t.product_id=p.id AND t.locale IN ('vi','en') AND t.content_status='published') locales FROM products p WHERE slug=$1",slug);
                if(publishing)Valid(product.S("business_status")=="active"&&product.I("locales")==2,"Sản phẩm CTA cần đang kinh doanh và có bản dịch VI/EN đã xuất bản.");
            }
            var colors=section.Array("colors");Valid(colors.Length is >=1 and <=8&&colors.All(c=>c.ValueKind==JsonValueKind.String&&Regex.IsMatch(c.GetString()!,"^#[0-9a-fA-F]{6}$")),"Màu phải là 1–8 mã #RRGGBB.");
            var preset=section.Text("preset");Valid(preset is "float" or "rotate" or "fold","Preset không hợp lệ.");
            foreach(var (field,min,max) in new[]{("camera",2d,12d),("tilt",0d,30d),("light",.2d,5d),("speed",0d,3d),("fold",0d,180d)})
            {var value=section.Field(field);Valid(value.ValueKind==JsonValueKind.Number&&value.TryGetDouble(out var number)&&double.IsFinite(number)&&number>=min&&number<=max,$"{field} ngoài khoảng cho phép.");}
            var model=section.Text("model");var builtin=model is "procedural:phone" or "procedural:duo";
            if(BundledHeroAssets.IsModel(model))
            {
                Valid(type!="duo"||model==BundledHeroAssets.Duo,"Duo cần model có animation mở/gập.");
                Valid(preset!="fold"||model==BundledHeroAssets.Duo,"Preset mở/gập cần model Duo.");
                Valid(type is not ("hero" or "duo")||slug==model[5..],"Model v003 phải khớp sản phẩm của section.");
                await BundledHeroAssets.ValidateModel(media.ProjectRoot,model,sql.Token);
            }
            else if(builtin)
            {
                Valid(type!="duo"||model=="procedural:duo","Duo cần model có bản lề.");Valid(preset!="fold"||model=="procedural:duo","Preset mở/gập cần model Duo.");
                var file=Path.Combine(media.ProjectRoot,"public","models",model.EndsWith("duo")?"duo-illustration.glb":"phone-illustration.glb");
                Valid(File.Exists(file),"Thiếu model local. Chạy chuẩn bị tài nguyên trước khi xuất bản.");
                MediaService.ValidateGlb(await File.ReadAllBytesAsync(file,sql.Token));
            }
            else
            {
                var asset=await Asset(model,assets);Valid(asset.S("mime")=="model/gltf-binary","Model phải là GLB đã kiểm tra.");
                var metadata=MediaService.ValidateGlb(await File.ReadAllBytesAsync(media.ResolveStorageKey(asset.S("storage_key")),sql.Token));
                Valid((type!="duo"&&preset!="fold")||metadata.SupportsFold,"Model không có node Hinge để mở/gập.");Valid(colors.Length<2||metadata.SupportsColor,"Model không có vật liệu BodyColorable để đổi màu.");
            }
            var poster=section.Text("poster");
            if(BundledHeroAssets.IsPoster(poster))await BundledHeroAssets.ValidatePoster(media.ProjectRoot,poster,sql.Token);
            else if(poster.StartsWith("/media/",StringComparison.Ordinal)&&TrustedPosters.Contains(poster[7..]))Valid(File.Exists(Path.Combine(media.ProjectRoot,"public","media",poster[7..])),"Thiếu poster local.");
            else {var asset=await Asset(poster,assets);Valid(asset.S("mime") is "image/jpeg" or "image/png" or "image/webp","Poster cần ảnh hợp lệ.");MediaService.ValidateImage(await File.ReadAllBytesAsync(media.ResolveStorageKey(asset.S("storage_key")),sql.Token));}
        }
        return assets;
    }
    private async Task<Dictionary<string,object?>> Asset(string value,HashSet<Guid> assets)
    {
        Valid(value.StartsWith("/api/media/",StringComparison.Ordinal)&&Guid.TryParse(value[11..],out _),"Chọn tài nguyên local đã kiểm tra trong thư viện.");
        var id=Guid.Parse(value[11..]);var asset=await sql.One("SELECT * FROM media_assets WHERE id=$1 AND visibility='public' AND validation_status='valid'",id);
        var file=media.ResolveStorageKey(asset.S("storage_key"));Valid(File.Exists(file),"Tệp media không còn trên máy.");
        var bytes=await File.ReadAllBytesAsync(file,sql.Token);Valid(Convert.ToHexString(SHA256.HashData(bytes)).Equals(asset.S("checksum"),StringComparison.OrdinalIgnoreCase),"Checksum media đã thay đổi.");assets.Add(id);return asset;
    }
    private async Task<object> IssuePreview(Actor actor,JsonElement input)
    {
        Access.Require(actor,"content_staff","owner");Fields(input,"revision","viewport","pageSlug");var revision=Revision(input);var viewport=input.Text("viewport","laptop");Valid(viewport is "mobile" or "laptop","Viewport không hợp lệ.");
        var draft=await sql.One("SELECT d.* FROM landing_drafts d JOIN landing_pages p ON p.id=d.page_id WHERE p.slug=$1 FOR SHARE OF d",Page(input));
        Access.Ensure(draft.I("revision")==revision,"STATE_CONFLICT","Lưu và tải đúng revision trước khi xem trước.");await ValidateSections(draft.J("sections"),false);
        var code=Secret();var expiry=DateTimeOffset.UtcNow.AddMinutes(10);var id=Guid.NewGuid();
        await sql.Exec("INSERT INTO landing_preview_grants(id,page_id,draft_revision,sections_snapshot,issued_by,code_hash,expires_at) VALUES($1,$2,$3,$4::jsonb,$5,$6,$7)",id,draft.G("page_id"),revision,draft.J("sections").GetRawText(),actor.Id,Hash(code),expiry);
        var origin=(configuration["StorefrontUrl"]??"http://localhost:3000").TrimEnd('/');Valid(Uri.TryCreate(origin,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https","StorefrontUrl không hợp lệ.");
        return new {grantId=id,revision,expiresAt=expiry,url=$"{origin}/vi/preview?code={Uri.EscapeDataString(code)}&viewport={viewport}"};
    }
    public async Task<PreviewSession> RedeemPreview(string code)
    {
        Valid(code.Length is >=32 and <=128,"Mã xem trước không hợp lệ.");
        return await sql.Transaction<PreviewSession>(null,async()=>
        {
            var grant=await sql.One("SELECT * FROM landing_preview_grants WHERE code_hash=$1 AND expires_at>now() AND redeemed_at IS NULL AND revoked_at IS NULL FOR UPDATE",Hash(code));
            await CheckIssuer(grant.S("issued_by"));var token=Secret();
            await sql.Exec("UPDATE landing_preview_grants SET redeemed_at=now(),session_hash=$2 WHERE id=$1",grant.G("id"),Hash(token));
            // Npgsql returns timestamptz as UTC DateTime. String roundtripping drops
            // the timezone and would expire cookies seven hours early in Vietnam.
            return new PreviewSession(token,new DateTimeOffset((DateTime)grant["expires_at"]!));
        });
    }
    public async Task<object> ReadPreview(string token)
    {
        Valid(token.Length is >=32 and <=128,"Phiên xem trước không hợp lệ.");
        var grant=await sql.One("SELECT * FROM landing_preview_grants WHERE session_hash=$1 AND expires_at>now() AND redeemed_at IS NOT NULL AND revoked_at IS NULL",Hash(token));
        await CheckIssuer(grant.S("issued_by"));return new {revision=grant.I("draft_revision"),sections=grant.J("sections_snapshot"),expiresAt=grant["expires_at"]};
    }
    private async Task CheckIssuer(string issuer)
    {
        var staff=await sql.Optional("SELECT s.role FROM staff_profiles s JOIN \"user\" u ON u.id=s.user_id WHERE s.user_id=$1 AND s.active AND s.role IN ('owner','content_staff') AND (u.lockout_end IS NULL OR u.lockout_end<=now())",issuer);
        Access.Ensure(staff is not null,"FORBIDDEN","Quyền xem trước đã hết hiệu lực.",403);
    }
}
