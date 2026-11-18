#:project ../../src/BotZone.Infrastructure/BotZone.Infrastructure.csproj
using System.Buffers.Binary;
using System.Text;
using System.Text.Json.Nodes;
using BotZone.Contracts;
using BotZone.Infrastructure.Services;

int passed=0;
void Check(bool value,string name){if(!value)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
void Rejected(Action action,string name){try{action();throw new Exception("Accepted invalid fixture: "+name);}catch(BusinessException e){Check(e.Status==400,name);}}
byte[] Modify(byte[] glb,Action<JsonObject> edit){var oldLength=(int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12,4));var json=JsonNode.Parse(glb.AsSpan(20,oldLength))!.AsObject();edit(json);var raw=Encoding.UTF8.GetBytes(json.ToJsonString());var padded=(raw.Length+3)/4*4;var result=new byte[20+padded+glb.Length-20-oldLength];glb.AsSpan(0,20).CopyTo(result);BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8,4),result.Length);BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12,4),padded);result.AsSpan(20,padded).Fill(32);raw.CopyTo(result,20);glb.AsSpan(20+oldLength).CopyTo(result.AsSpan(20+padded));return result;}
var phone=File.ReadAllBytes("public/models/phone-illustration.glb");var duo=File.ReadAllBytes("public/models/duo-illustration.glb");
var p=MediaService.ValidateGlb(phone);Check(p.SupportsColor&&!p.SupportsFold,"phone model structure and color material");
var d=MediaService.ValidateGlb(duo);Check(d.SupportsColor&&d.SupportsFold,"duo Hinge and color material derived");
Rejected(()=>MediaService.ValidateGlb(Modify(phone,j=>j["buffers"]![0]!["uri"]="https://example.com/data.bin")),"external buffer URI rejected");
Rejected(()=>MediaService.ValidateGlb(Modify(phone,j=>j["nodes"]![0]!["children"]=new JsonArray(0))),"cyclic nodes rejected");
Rejected(()=>MediaService.ValidateGlb(Modify(phone,j=>j["accessors"]![0]!["count"]=int.MaxValue)),"accessor allocation overflow rejected");
Rejected(()=>MediaService.ValidateGlb(phone[..^8]),"truncated GLB rejected");
Rejected(()=>MediaService.ValidateGlb(Modify(phone,j=>j["extensionsUsed"]=new JsonArray("KHR_draco_mesh_compression"))),"external decoder dependency rejected");
var png=File.ReadAllBytes("artifacts/web/login-mobile.png");var image=MediaService.ValidateImage(png);Check(image.Mime=="image/png"&&image.Width==390,"real browser PNG signature dimensions CRC");
var broken=(byte[])png.Clone();broken[30]^=0xff;Rejected(()=>MediaService.ValidateImage(broken),"PNG CRC mismatch rejected");
var overflow=(byte[])png.Clone();BinaryPrimitives.WriteUInt32BigEndian(overflow.AsSpan(8,4),uint.MaxValue);Rejected(()=>MediaService.ValidateImage(overflow),"malformed chunk overflow returns validation error");
Rejected(()=>MediaService.ValidateImage(Encoding.UTF8.GetBytes("<svg onload='alert(1)'/>")),"arbitrary SVG rejected");
Console.WriteLine($"Passed {passed} media validation checks.");
