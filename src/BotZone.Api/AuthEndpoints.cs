using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Data.Entities;
using BotZone.Infrastructure.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
namespace BotZone.Api;

public static class AuthEndpoints
{
 public const string Scheme="BotZone.Session";
 public static Actor Actor(HttpContext context)=>context.Items["actor"] as Actor??throw new BusinessException("UNAUTHENTICATED","Vui lòng đăng nhập.",401);
 public static async Task Validate(CookieValidatePrincipalContext context){var db=context.HttpContext.RequestServices.GetRequiredService<BotZoneDbContext>();var userId=context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);var sid=context.Principal?.FindFirstValue("sid");var stamp=context.Principal?.FindFirstValue("stamp");if(!Guid.TryParse(sid,out var sessionId)){context.RejectPrincipal();return;}var session=await db.AuthSessions.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==sessionId&&x.UserId==userId,context.HttpContext.RequestAborted);var user=await db.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.Id==userId,context.HttpContext.RequestAborted);var staff=await db.StaffProfiles.AsNoTracking().FirstOrDefaultAsync(x=>x.UserId==userId,context.HttpContext.RequestAborted);if(session is null||session.RevokedAt is not null||session.ExpiresAt<=DateTime.UtcNow||user is null||user.SecurityStamp!=stamp||user.PasswordResetRequired||user.LockoutEnd>DateTimeOffset.UtcNow||staff is {Active:false}){context.RejectPrincipal();return;}context.HttpContext.Items["actor"]=new Actor(user.Id,user.Name,user.Email!,staff?.Role??"customer",user.EmailConfirmed);}
 static async Task Revoke(BotZoneDbContext db,string id)=>await db.AuthSessions.Where(x=>x.UserId==id&&x.RevokedAt==null).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.RevokedAt,DateTime.UtcNow));
 public static void Check(IdentityResult result,string locale="vi")
 {
  if(result.Succeeded)return;
  var codes=result.Errors.Select(error=>error.Code).ToHashSet(StringComparer.Ordinal);
  var (code,vi,en)=codes.Overlaps(new[]{"DuplicateEmail","DuplicateUserName"})
   ?("EMAIL_IN_USE","Email đã được sử dụng.","This email address is already registered. Sign in or reset your password.")
   :codes.Contains("PasswordMismatch")?("VALIDATION_ERROR","Mật khẩu không đúng.","The current password is incorrect.")
   :codes.Contains("InvalidToken")?("TOKEN_INVALID","Liên kết không hợp lệ hoặc đã hết hạn.","This link is invalid or has expired. Request a new email link.")
   :codes.Any(value=>value.StartsWith("Password",StringComparison.Ordinal))
    ?("PASSWORD_POLICY","Mật khẩu phải có ít nhất 6 ký tự.","Use a password with at least 6 characters.")
   :("VALIDATION_ERROR","Thông tin tài khoản không hợp lệ. Vui lòng kiểm tra lại.","Check your account information and try again.");
  throw new BusinessException(code,locale=="en"?en:vi,400);
 }
 static string Encode(string token)=>WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
 static string Decode(string token){try{return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));}catch{throw new BusinessException("TOKEN_INVALID","Token không hợp lệ.",400);}}
 public static Task Mail(SqlStore sql,User user,string kind,string token,IConfiguration config,string locale="vi")=>sql.Exec("INSERT INTO outbox_events(event_key,type,payload) VALUES($1,'email',$2::jsonb)",Guid.NewGuid().ToString(),Values.Json(new{to=user.Email,subject=locale=="en"?(kind=="verify-email"?"Verify your BotZone email":"Reset your BotZone password"):(kind=="verify-email"?"Xác minh email BotZone":"Đặt lại mật khẩu BotZone"),text=$"{config["StorefrontUrl"]??"http://localhost:3000"}/{(locale=="en"?"en":"vi")}/{kind}?userId={Uri.EscapeDataString(user.Id)}&token={Encode(token)}"}));
 public static void Map(WebApplication app){
  app.MapGet("/api/auth/csrf",(HttpContext c,IAntiforgery af)=>Results.Ok(new{token=af.GetAndStoreTokens(c).RequestToken}));
  app.MapGet("/api/session",(HttpContext c)=>Results.Ok(new{actor=c.Items["actor"]}));
  app.MapPost("/api/auth/login",CustomerAuthentication.Login);
  app.MapPost("/api/auth/logout",async(HttpContext c,BotZoneDbContext db)=>{if(Guid.TryParse(c.User.FindFirstValue("sid"),out var sid))await db.AuthSessions.Where(x=>x.Id==sid).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.RevokedAt,DateTime.UtcNow));await c.SignOutAsync(Scheme);return Results.Ok(new{loggedOut=true});});
  app.MapPost("/api/auth/register",CustomerAuthentication.Register);
  app.MapPost("/api/auth/resend-verification",async(JsonElement input,HttpContext c,UserManager<User> users,SqlStore sql,IConfiguration config)=>{
   var actor=Actor(c);Access.Require(actor,"customer");
   var user=await users.FindByIdAsync(actor.Id)??throw new BusinessException("UNAUTHENTICATED","Phiên đã hết hạn.",401);
   if(user.EmailConfirmed)return Results.Ok(new{verified=true});
   await sql.Transaction<object>(actor,async()=>{
    var recent=await sql.Optional("SELECT id FROM outbox_events WHERE type='email' AND event_key LIKE 'verify-resend:%' AND payload->>'to'=$1 AND created_at>now()-interval '60 seconds' LIMIT 1",user.Email);
    if(recent is null){
     var locale=ApiLocalization.Locale(c);var next=input.Text("next",$"/{locale}/account");
     var path=next==$"/{locale}/checkout"?next:$"/{locale}/account";
     var token=await users.GenerateEmailConfirmationTokenAsync(user);
     await sql.Exec("INSERT INTO outbox_events(event_key,type,payload) VALUES($1,'email',$2::jsonb)","verify-resend:"+Guid.NewGuid(),Values.Json(new{to=user.Email,subject=locale=="en"?"Verify your BotZone email":"Xác minh email BotZone",text=$"{config["StorefrontUrl"]??"http://localhost:3000"}/{locale}/verify-email?userId={Uri.EscapeDataString(user.Id)}&token={Encode(token)}&next={Uri.EscapeDataString(path)}"}));
    }
    return new{sent=true};
   });return Results.Ok(new{sent=true});
  });
  app.MapPost("/api/auth/verify-email",async(JsonElement input,UserManager<User> users)=>{var user=await users.FindByIdAsync(input.Text("userId"))??throw new BusinessException("TOKEN_INVALID","Token không hợp lệ.",400);Access.Ensure(!user.EmailConfirmed,"TOKEN_USED","Email đã được xác minh.",409);Check(await users.ConfirmEmailAsync(user,Decode(input.Text("token"))));return Results.Ok(new{verified=true});});
  app.MapPost("/api/auth/forgot-password",async(JsonElement input,HttpContext c,UserManager<User> users,SqlStore sql,IConfiguration config)=>{var user=await users.FindByEmailAsync(input.Text("email").Trim());if(user is not null)await Mail(sql,user,"reset-password",await users.GeneratePasswordResetTokenAsync(user),config,ApiLocalization.Locale(c));return Results.Ok(new{sent=true});});
  app.MapPost("/api/auth/reset-password",async(JsonElement input,UserManager<User> users,BotZoneDbContext db,SqlStore sql)=>{var user=await users.FindByIdAsync(input.Text("userId"))??throw new BusinessException("TOKEN_INVALID","Token không hợp lệ.",400);return await sql.Transaction<object>(null,async()=>{Check(await users.ResetPasswordAsync(user,Decode(input.Text("token")),input.Text("password",input.Text("newPassword",""))));user.PasswordResetRequired=false;Check(await users.UpdateAsync(user));await Revoke(db,user.Id);return new{reset=true,sessionsRevoked=true};});});
  app.MapPost("/api/auth/change-password",async(JsonElement input,HttpContext c,UserManager<User> users,BotZoneDbContext db,SqlStore sql)=>{var actor=Actor(c);var user=await users.FindByIdAsync(actor.Id)??throw new BusinessException("UNAUTHENTICATED","Phiên đã hết hạn.",401);await sql.Transaction<object>(actor,async()=>{Check(await users.ChangePasswordAsync(user,input.Text("currentPassword"),input.Text("newPassword")));await Revoke(db,user.Id);return new{};});await c.SignOutAsync(Scheme);return Results.Ok(new{changed=true,sessionsRevoked=true});});
 }
}
