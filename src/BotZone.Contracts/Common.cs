using System.Text.Json;
namespace BotZone.Contracts;
public sealed record Actor(string Id,string Name,string Email,string Role,bool Verified);
public sealed record CommandRequest(string Command,string Key,JsonElement Input);
public sealed record LoginRequest(string Email,string Password,string ClientType="web");
public sealed record RegisterRequest(string Name,string Email,string Password);
public sealed class BusinessException(string code,string message,int status=400):Exception(message){public string Code{get;}=code;public int Status{get;}=status;}
public static class Access{
 public static void Require(Actor actor,params string[] roles){if(!roles.Contains(actor.Role))throw new BusinessException("FORBIDDEN","Bạn không có quyền thực hiện thao tác này.",403);}
 public static void Ensure(bool condition,string code,string message,int status=409){if(!condition)throw new BusinessException(code,message,status);}
}
