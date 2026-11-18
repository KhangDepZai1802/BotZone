using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BotZone.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BotZone.Infrastructure.Services;

public sealed record ValidatedImage(string Mime,int Width,int Height);
public sealed record ValidatedModel(bool SupportsFold,bool SupportsColor,string[] Materials,string[] Hotspots);
public sealed record MediaDownload(byte[] Buffer,string ContentType,string Filename,bool Public);

public sealed class MediaService(SqlStore sql,IConfiguration configuration)
{
    public string ProjectRoot {get;}=Path.GetFullPath(configuration["BOTZONE_ROOT"]??Environment.GetEnvironmentVariable("BOTZONE_ROOT")??Directory.GetCurrentDirectory());
    private string UploadRoot=>Path.GetFullPath(configuration["UPLOAD_DIR"]??"uploads",ProjectRoot);
    private const int ImageLimit=5*1024*1024,ModelLimit=20*1024*1024;
    private static void Valid(bool value,string message)=>Access.Ensure(value,"MEDIA_INVALID",message,400);
    public string ResolveStorageKey(string key)
    {
        Valid(!Path.IsPathRooted(key)&&!key.Contains('\\')&&!key.Split('/').Any(part=>part is ".." or "." or ""),"Storage key không hợp lệ.");
        var path=Path.GetFullPath(key,UploadRoot);Valid(path.StartsWith(UploadRoot.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal),"Tệp ngoài thư viện local.");
        return path;
    }

    public async Task<object> Upload(Actor actor,IFormFile file,string visibility,string? referenceType=null,Guid? referenceId=null,string? sourceUrl=null,string? licenseNote=null)
    {
        Valid(visibility is "public" or "private","Visibility chỉ nhận public hoặc private.");
        if(visibility=="public")Access.Require(actor,"content_staff");
        else {Access.Require(actor,"customer","owner","order_staff","warehouse_staff");Access.Ensure(actor.Verified,"FORBIDDEN","Xác minh email trước khi gửi bằng chứng.",403);Valid(referenceType is not null&&referenceId.HasValue,"Bằng chứng cần hồ sơ liên quan.");await AuthorizeReference(actor,referenceType!,referenceId!.Value);}
        Valid(file.Length>0&&file.Length<=ModelLimit,"Tệp rỗng hoặc vượt 20 MB.");
        await using var input=file.OpenReadStream();using var output=new MemoryStream();var buffer=new byte[65536];int count;
        while((count=await input.ReadAsync(buffer,sql.Token))>0){Valid(output.Length+count<=ModelLimit,"Tệp vượt 20 MB.");await output.WriteAsync(buffer.AsMemory(0,count),sql.Token);}
        var bytes=output.ToArray();Valid(bytes.Length==file.Length,"Kích thước tệp không khớp.");string mime,extension;object metadata;
        if(bytes.Length>=4&&Encoding.ASCII.GetString(bytes,0,4)=="glTF")
        {
            Valid(visibility=="public","Bằng chứng phải là ảnh; GLB chỉ dùng cho catalogue.");var model=ValidateGlb(bytes);mime="model/gltf-binary";extension="glb";
            metadata=new {supportsFold=model.SupportsFold,supportsColor=model.SupportsColor,materials=model.Materials,hotspots=model.Hotspots,selfContained=true,mobileBudgetExceeded=bytes.Length>4*1024*1024,laptopBudgetExceeded=bytes.Length>10*1024*1024,referenceType,referenceId};
        }
        else
        {
            var image=ValidateImage(bytes);mime=image.Mime;extension=mime=="image/png"?"png":mime=="image/webp"?"webp":"jpg";metadata=new {width=image.Width,height=image.Height,referenceType,referenceId};
        }
        if(visibility=="public")
        {
            Valid(!string.IsNullOrWhiteSpace(sourceUrl)&&sourceUrl.Length<=1000,"Nguồn tài nguyên là bắt buộc.");
            Valid(sourceUrl!.StartsWith("original:",StringComparison.Ordinal)||Uri.TryCreate(sourceUrl,UriKind.Absolute,out var uri)&&uri.Scheme is "https" or "http","Nguồn phải là URL http(s) hoặc original:tên-tác-giả.");
            Valid(!string.IsNullOrWhiteSpace(licenseNote)&&licenseNote.Length is >=3 and <=2000,"Cần ghi điều kiện sử dụng tài nguyên.");
        }
        else {sourceUrl="original:user-evidence";licenseNote="Private supporting evidence, restricted to the submitting customer and authorized operational staff.";}
        var id=Guid.NewGuid();var storageKey=$"{(visibility=="public"?"catalogue-public":"evidence-private")}/{id:D}.{extension}";var filePath=ResolveStorageKey(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        try
        {
            await using(var stream=new FileStream(filePath,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true))await stream.WriteAsync(bytes,sql.Token);
            await sql.Transaction<object>(actor,async()=>
            {
                if(visibility=="private")
                {
                    await AuthorizeReference(actor,referenceType!,referenceId!.Value);
                }
                await sql.Exec("INSERT INTO media_assets(id,storage_key,visibility,mime,size_bytes,checksum,validation_status,source_url,license_note,owner_user_id,metadata) VALUES($1,$2,$3,$4,$5,$6,'valid',$7,$8,$9,$10::jsonb)",id,storageKey,visibility,mime,(long)bytes.Length,Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),sourceUrl,licenseNote,actor.Id,Values.Json(metadata));
                await sql.Exec("INSERT INTO audit_events(actor_user_id,action,entity_type,entity_id,after_data) VALUES($1,'upload_media','media_asset',$2,$3::jsonb)",actor.Id,id.ToString(),Values.Json(new {id,visibility,mime,sizeBytes=bytes.Length,referenceType,referenceId}));
                return new {};
            });
        }
        catch {if(File.Exists(filePath))File.Delete(filePath);throw;}
        return new {id,previewUrl=$"/api/media/{id:D}",mime,sizeBytes=bytes.Length,metadata};
    }

    public async Task<MediaDownload> Download(Guid id,Actor? actor)
    {
        var asset=await sql.One("SELECT * FROM media_assets WHERE id=$1 AND validation_status='valid'",id);
        if(asset.S("visibility")!="public")
        {
            Access.Ensure(actor is not null,"UNAUTHENTICATED","Đăng nhập để đọc bằng chứng riêng tư.",401);
            var reader=actor!;
            Access.Require(reader,"customer","owner","order_staff","warehouse_staff");
            // Visibility follows the case owner, including inspection images uploaded by staff.
            // The reference is server validated at upload and checked again on every read.
            var meta=asset.J("metadata");if(meta.TryGetProperty("referenceType",out var type)&&type.ValueKind==JsonValueKind.String&&meta.TryGetProperty("referenceId",out var reference)&&Guid.TryParse(reference.GetString(),out var referenceId))await AuthorizeReference(reader,type.GetString()!,referenceId);
            else Access.Ensure(reader.Role=="owner","FORBIDDEN","Bằng chứng chưa liên kết hồ sơ hợp lệ.",403);
        }
        var file=ResolveStorageKey(asset.S("storage_key"));Access.Ensure(File.Exists(file),"NOT_FOUND","Tệp media không còn trong thư viện.",404);
        var bytes=await File.ReadAllBytesAsync(file,sql.Token);Access.Ensure(Convert.ToHexString(SHA256.HashData(bytes)).Equals(asset.S("checksum"),StringComparison.OrdinalIgnoreCase),"MEDIA_INVALID","Checksum media không khớp.",409);
        return new MediaDownload(bytes,asset.S("mime"),Path.GetFileName(file),asset.S("visibility")=="public");
    }
    private async Task AuthorizeReference(Actor actor,string kind,Guid id)
    {
        var query=kind switch
        {
            "order"=>"SELECT customer_id FROM orders WHERE id=$1",
            "entitlement"=>"SELECT o.customer_id FROM warranty_entitlements e JOIN order_item_unit_amounts u ON u.id=e.order_item_unit_amount_id JOIN order_items i ON i.id=u.order_item_id JOIN orders o ON o.id=i.order_id WHERE e.id=$1",
            "trade_in"=>"SELECT customer_id FROM trade_in_requests WHERE id=$1",
            "trade_in_inspection"=>"SELECT r.customer_id FROM trade_in_inspections x JOIN trade_in_items i ON i.id=x.item_id JOIN trade_in_requests r ON r.id=i.request_id WHERE x.id=$1",
            "return"=>"SELECT customer_id FROM return_requests WHERE id=$1",
            "warranty"=>"SELECT o.customer_id FROM warranty_claims c JOIN warranty_entitlements e ON e.id=c.entitlement_id JOIN order_item_unit_amounts u ON u.id=e.order_item_unit_amount_id JOIN order_items i ON i.id=u.order_item_id JOIN orders o ON o.id=i.order_id WHERE c.id=$1",
            _=>throw new BusinessException("MEDIA_INVALID","Loại hồ sơ bằng chứng không hợp lệ.",400)
        };
        var reference=await sql.One(query,id);if(actor.Role=="customer")Access.Ensure(reference.S("customer_id")==actor.Id,"FORBIDDEN","Hồ sơ không thuộc tài khoản của bạn.",403);
    }

    public static ValidatedImage ValidateImage(byte[] bytes)
    {
        try{return ValidateImageCore(bytes);}
        catch(Exception error)when(error is OverflowException or ArgumentOutOfRangeException or InvalidOperationException or FormatException){throw new BusinessException("MEDIA_INVALID","Cấu trúc ảnh không hợp lệ.",400);}
    }
    private static ValidatedImage ValidateImageCore(byte[] bytes)
    {
        Valid(bytes.Length is >0 and <=ImageLimit,"Ảnh tối đa 5 MB.");int width=0,height=0;string mime;
        if(bytes.Length>=33&&bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))
        {
            mime="image/png";int offset=8;bool header=false,data=false,end=false;
            while(offset+12<=bytes.Length)
            {
                var size=checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset,4)));Valid(size>=0&&size<=bytes.Length-offset-12,"PNG chunk bị cắt.");var type=Encoding.ASCII.GetString(bytes,offset+4,4);
                Valid(Crc32(bytes.AsSpan(offset+4,size+4))==BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset+size+8,4)),"PNG CRC không hợp lệ.");
                if(!header){Valid(type=="IHDR"&&size==13,"PNG thiếu IHDR.");width=checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset+8,4)));height=checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset+12,4)));header=true;}
                else Valid(type!="IHDR","PNG có nhiều IHDR.");
                if(type=="IDAT")data=true;if(type=="IEND"){Valid(size==0&&offset+12==bytes.Length,"PNG kết thúc không hợp lệ.");end=true;break;}offset+=size+12;
            }
            Valid(header&&data&&end,"PNG thiếu dữ liệu hoặc bị cắt.");
        }
        else if(bytes.Length>=12&&bytes[0]==0xff&&bytes[1]==0xd8&&bytes[^2]==0xff&&bytes[^1]==0xd9)
        {
            mime="image/jpeg";int offset=2;bool scan=false;
            while(offset+3<bytes.Length)
            {
                Valid(bytes[offset++]==0xff,"JPEG marker không hợp lệ.");while(offset<bytes.Length&&bytes[offset]==0xff)offset++;Valid(offset<bytes.Length,"JPEG bị cắt.");var marker=bytes[offset++];if(marker==0xd9)break;
                if(marker is 0x01 or >=0xd0 and <=0xd7)continue;
                Valid(offset+2<=bytes.Length,"JPEG segment bị cắt.");var size=BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset,2));Valid(size>=2&&offset+size<=bytes.Length,"JPEG segment không hợp lệ.");
                if(marker is 0xc0 or 0xc1 or 0xc2){Valid(size>=8,"JPEG frame bị cắt.");height=BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset+3,2));width=BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset+5,2));}
                if(marker==0xda){scan=true;break;}offset+=size;
            }
            Valid(scan&&width>0&&height>0,"JPEG thiếu frame hoặc scan được hỗ trợ.");
        }
        else if(bytes.Length>=30&&Encoding.ASCII.GetString(bytes,0,4)=="RIFF"&&Encoding.ASCII.GetString(bytes,8,4)=="WEBP")
        {
            mime="image/webp";Valid(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4,4))==bytes.Length-8,"WebP RIFF length không khớp.");int offset=12;bool image=false;
            while(offset+8<=bytes.Length)
            {
                var type=Encoding.ASCII.GetString(bytes,offset,4);var size=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset+4,4)));var start=offset+8;Valid(size>=0&&size<=bytes.Length-start,"WebP chunk bị cắt.");
                if(type=="VP8 "){Valid(size>=10&&bytes[start+3]==0x9d&&bytes[start+4]==1&&bytes[start+5]==0x2a,"WebP VP8 không hợp lệ.");width=BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(start+6,2))&0x3fff;height=BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(start+8,2))&0x3fff;image=true;}
                if(type=="VP8L"){Valid(size>=5&&bytes[start]==0x2f,"WebP lossless không hợp lệ.");width=1+bytes[start+1]+((bytes[start+2]&0x3f)<<8);height=1+(bytes[start+2]>>6)+(bytes[start+3]<<2)+((bytes[start+4]&0x0f)<<10);image=true;}
                Valid(type is not ("ANIM" or "ANMF"),"Dùng ảnh WebP tĩnh.");offset=start+size+(size&1);
            }
            Valid(image&&offset==bytes.Length,"WebP thiếu ảnh hoặc bị cắt.");
        }
        else throw new BusinessException("MEDIA_INVALID","Chỉ nhận nội dung JPEG, PNG, WebP hoặc GLB hợp lệ; không nhận SVG/HTML.",400);
        Valid(width>0&&height>0&&(long)width*height<=20_000_000,"Ảnh tối đa 20 megapixel.");return new(mime,width,height);
    }
    private static uint Crc32(ReadOnlySpan<byte> bytes){uint crc=0xffffffff;foreach(var b in bytes){crc^=b;for(var bit=0;bit<8;bit++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}return ~crc;}

    public static ValidatedModel ValidateGlb(byte[] bytes)
    {
        try{return ValidateGlbCore(bytes);}
        catch(Exception error)when(error is OverflowException or ArgumentOutOfRangeException or InvalidOperationException or FormatException){throw new BusinessException("MEDIA_INVALID","Cấu trúc GLB không hợp lệ.",400);}
    }
    private static ValidatedModel ValidateGlbCore(byte[] bytes)
    {
        Valid(bytes.Length is >=28 and <=ModelLimit&&Encoding.ASCII.GetString(bytes,0,4)=="glTF","GLB không hợp lệ hoặc vượt 20 MB.");
        Valid(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4,4))==2&&BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8,4))==bytes.Length,"GLB header/length không hợp lệ.");
        var jsonLength=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12,4)));Valid(jsonLength>0&&jsonLength%4==0&&jsonLength<=bytes.Length-20&&BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16,4))==0x4e4f534a,"GLB thiếu JSON chunk.");
        JsonDocument document;try{document=JsonDocument.Parse(bytes.AsMemory(20,jsonLength),new JsonDocumentOptions{MaxDepth=64});}catch(JsonException){throw new BusinessException("MEDIA_INVALID","GLB JSON không hợp lệ.",400);}
        using(document)
        {
            var root=document.RootElement;Valid(root.ValueKind==JsonValueKind.Object&&root.TryGetProperty("asset",out var asset)&&asset.TryGetProperty("version",out var version)&&version.GetString()=="2.0","GLB cần glTF2.0.");
            ValidateUris(root);int binaryLength=0;var offset=20+jsonLength;
            if(offset<bytes.Length){Valid(offset+8<=bytes.Length&&BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset+4,4))==0x004e4942,"GLB chỉ nhận JSON và BIN tự chứa.");binaryLength=checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset,4)));Valid(binaryLength>=0&&binaryLength%4==0&&offset+8+binaryLength==bytes.Length,"GLB BIN length không khớp.");}
            var buffers=Array(root,"buffers");Valid(buffers.Length==1,"GLB cần đúng một buffer nhúng.");var declared=Number(buffers[0],"byteLength");Valid(declared>0&&declared<=binaryLength&&binaryLength-declared<=3,"GLB buffer length không khớp.");
            var views=Array(root,"bufferViews");foreach(var view in views){Valid(Number(view,"buffer")==0,"GLB buffer ngoài không hợp lệ.");var start=Number(view,"byteOffset",0);var length=Number(view,"byteLength");Valid(start>=0&&length>0&&(long)start+length<=declared,"GLB bufferView vượt dữ liệu.");}
            foreach(var accessor in Array(root,"accessors"))
            {
                var count=Number(accessor,"count");Valid(count is >0 and <=2_000_000,"GLB accessor count vượt ngân sách.");Valid(!accessor.TryGetProperty("sparse",out _),"GLB sparse accessor chưa được hỗ trợ; xuất bản geometry đầy đủ.");var view=Number(accessor,"bufferView");Valid(view>=0&&view<views.Length,"GLB accessor thiếu bufferView.");
                var component=Number(accessor,"componentType");var size=component switch{5120 or 5121=>1,5122 or 5123=>2,5125 or 5126=>4,_=>0};var dimensions=accessor.Text("type") switch{"SCALAR"=>1,"VEC2"=>2,"VEC3"=>3,"VEC4" or "MAT2"=>4,"MAT3"=>9,"MAT4"=>16,_=>0};Valid(size>0&&dimensions>0,"GLB accessor type không hợp lệ.");var stride=Number(views[view],"byteStride",size*dimensions);Valid(stride>=size*dimensions&&stride<=252,"GLB stride không hợp lệ.");Valid((long)Number(accessor,"byteOffset",0)+(long)(count-1)*stride+size*dimensions<=Number(views[view],"byteLength"),"GLB accessor vượt bufferView.");
            }
            foreach(var extension in Array(root,"extensionsUsed").Concat(Array(root,"extensionsRequired")))Valid(extension.ValueKind==JsonValueKind.String&&extension.GetString() is not("KHR_draco_mesh_compression" or "EXT_meshopt_compression" or "KHR_texture_basisu"),"Model cần xuất không nén Draco/Meshopt/Basis để chạy local không decoder ngoài.");
            var images=Array(root,"images");Valid(images.Length<=16,"GLB tối đa 16 texture.");foreach(var image in images){var view=Number(image,"bufferView");Valid(view>=0&&view<views.Length,"Texture phải nhúng vào GLB.");var start=Number(views[view],"byteOffset",0)+offset+8;var length=Number(views[view],"byteLength");var texture=ValidateImage(bytes.AsSpan(start,length).ToArray());Valid(image.Text("mimeType")==texture.Mime,"MIME texture không khớp.");}
            var nodes=Array(root,"nodes");Valid(nodes.Length is >0 and <=10000,"Model cần 1–10000 node.");var materials=Array(root,"materials").Select(m=>m.Text("name","")).Where(n=>n.Length>0).ToArray();
            var meshes=Array(root,"meshes");Valid(meshes.Length is >0 and <=5000,"Model cần mesh hiển thị.");var accessors=Array(root,"accessors");
            foreach(var mesh in meshes)
            {
                var primitives=Array(mesh,"primitives");Valid(primitives.Length is >0 and <=1000,"Mesh cần primitive hợp lệ.");
                foreach(var primitive in primitives)
                {
                    var attributes=primitive.Field("attributes");Valid(attributes.ValueKind==JsonValueKind.Object&&attributes.TryGetProperty("POSITION",out _),"Primitive thiếu thuộc tính POSITION.");
                    foreach(var attribute in attributes.EnumerateObject())Valid(attribute.Value.ValueKind==JsonValueKind.Number&&attribute.Value.TryGetInt32(out var index)&&index>=0&&index<accessors.Length,"Primitive tham chiếu accessor không tồn tại.");
                    if(primitive.TryGetProperty("indices",out _)){var index=Number(primitive,"indices");Valid(index>=0&&index<accessors.Length,"Indices accessor không tồn tại.");}
                    if(primitive.TryGetProperty("material",out _)){var index=Number(primitive,"material");Valid(index>=0&&index<Array(root,"materials").Length,"Material không tồn tại.");}
                }
            }
            var incoming=new int[nodes.Length];var edges=new List<int>[nodes.Length];
            for(var index=0;index<nodes.Length;index++)
            {
                var node=nodes[index];if(node.TryGetProperty("mesh",out _))Valid(Number(node,"mesh")>=0&&Number(node,"mesh")<meshes.Length,"Node tham chiếu mesh không tồn tại.");
                edges[index]=[];foreach(var child in Array(node,"children")){Valid(child.ValueKind==JsonValueKind.Number&&child.TryGetInt32(out var childIndex)&&childIndex>=0&&childIndex<nodes.Length,"Node con không hợp lệ.");var childId=child.GetInt32();Valid(++incoming[childId]<=1,"Node không được có nhiều cha.");edges[index].Add(childId);}
            }
            var roots=new Queue<int>(Enumerable.Range(0,nodes.Length).Where(index=>incoming[index]==0));int visited=0;while(roots.TryDequeue(out var index)){visited++;foreach(var child in edges[index])if(--incoming[child]==0)roots.Enqueue(child);}Valid(visited==nodes.Length,"GLB có vòng lặp node.");
            var scenes=Array(root,"scenes");Valid(scenes.Length>0,"GLB thiếu scene.");var sceneIndex=Number(root,"scene",0);Valid(sceneIndex>=0&&sceneIndex<scenes.Length,"Scene mặc định không tồn tại.");
            foreach(var scene in scenes)foreach(var node in Array(scene,"nodes"))Valid(node.ValueKind==JsonValueKind.Number&&node.TryGetInt32(out var index)&&index>=0&&index<nodes.Length,"Scene tham chiếu node không tồn tại.");
            var hotspots=nodes.Select(n=>n.Text("name","")).Where(n=>n.StartsWith("Hotspot_",StringComparison.Ordinal)).ToArray();
            return new(nodes.Any(n=>n.Text("name","")=="Hinge"),materials.Any(n=>n.Contains("body",StringComparison.OrdinalIgnoreCase)||n.Contains("colorable",StringComparison.OrdinalIgnoreCase)),materials,hotspots);
        }
    }
    private static JsonElement[] Array(JsonElement value,string name){if(!value.TryGetProperty(name,out var array))return [];Valid(array.ValueKind==JsonValueKind.Array,$"GLB {name} phải là danh sách.");return array.EnumerateArray().ToArray();}
    private static int Number(JsonElement value,string name,int? fallback=null){if(value.TryGetProperty(name,out var number)&&number.ValueKind==JsonValueKind.Number&&number.TryGetInt32(out var result))return result;if(fallback.HasValue)return fallback.Value;throw new BusinessException("MEDIA_INVALID",$"GLB thiếu hoặc sai {name}.",400);}
    private static void ValidateUris(JsonElement value)
    {
        if(value.ValueKind==JsonValueKind.Object)foreach(var property in value.EnumerateObject()){Valid(property.Name!="uri","GLB phải nhúng tài nguyên; không được tham chiếu URI, URL hoặc đường dẫn ngoài.");ValidateUris(property.Value);}
        else if(value.ValueKind==JsonValueKind.Array)foreach(var child in value.EnumerateArray())ValidateUris(child);
    }
}
