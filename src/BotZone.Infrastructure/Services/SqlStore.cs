using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
namespace BotZone.Infrastructure.Services;

// Uses the single EF-owned connection and transaction. SQL identifiers are server constants.
public sealed class SqlStore(BotZoneDbContext db)
{
 public BotZoneDbContext Db=>db;
 public CancellationToken Token {get;set;}
 public Guid? SessionId {get;set;}
 async Task<DbCommand> Command(string sql,object?[] values){await db.Database.OpenConnectionAsync(Token);var c=db.Database.GetDbConnection().CreateCommand();c.CommandText=System.Text.RegularExpressions.Regex.Replace(sql,@"\$(\d+)","@p$1");c.Transaction=db.Database.CurrentTransaction?.GetDbTransaction();for(int i=0;i<values.Length;i++){var v=values[i];c.Parameters.Add(v is null ? new NpgsqlParameter{ParameterName=$"p{i+1}",NpgsqlDbType=NpgsqlDbType.Unknown,Value=DBNull.Value}:new NpgsqlParameter{ParameterName=$"p{i+1}",Value=v});}return c;}
 public async Task<int> Exec(string sql,params object?[] values){await using var c=await Command(sql,values);return await c.ExecuteNonQueryAsync(Token);}
 public async Task<List<Dictionary<string,object?>>> Rows(string sql,params object?[] values){await using var c=await Command(sql,values);await using var r=await c.ExecuteReaderAsync(Token);var rows=new List<Dictionary<string,object?>>();while(await r.ReadAsync(Token)){var row=new Dictionary<string,object?>();for(int i=0;i<r.FieldCount;i++){object? v=await r.IsDBNullAsync(i,Token)?null:r.GetValue(i);if(v is long l)v=l.ToString(CultureInfo.InvariantCulture);if(v is decimal d)v=d.ToString(CultureInfo.InvariantCulture);if(v is string s&&r.GetDataTypeName(i) is "json" or "jsonb")v=JsonDocument.Parse(s).RootElement.Clone();row[r.GetName(i)]=v;}rows.Add(row);}return rows;}
 public async Task<Dictionary<string,object?>> One(string sql,params object?[] values)=> (await Rows(sql,values)).FirstOrDefault()??throw new BusinessException("NOT_FOUND","Không tìm thấy dữ liệu trong phạm vi được phép.",404);
 public async Task<Dictionary<string,object?>?> Optional(string sql,params object?[] values)=>(await Rows(sql,values)).FirstOrDefault();
 public async Task<T> Transaction<T>(Actor? actor,Func<Task<T>> action,bool serialize=true){await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted,Token);if(serialize)await Exec("SELECT pg_advisory_xact_lock(12648321)");if(actor is not null){var staff=await Optional("SELECT role,active FROM staff_profiles WHERE user_id=$1 FOR SHARE",actor.Id);Access.Ensure(staff is null?actor.Role=="customer":staff.B("active")&&staff.S("role")==actor.Role,"FORBIDDEN","Quyền tài khoản đã thay đổi. Vui lòng đăng nhập lại.",403);if(SessionId.HasValue)Access.Ensure(await Optional("SELECT id FROM auth_sessions WHERE id=$1 AND user_id=$2 AND revoked_at IS NULL AND expires_at>now() FOR SHARE",SessionId.Value,actor.Id) is not null,"UNAUTHENTICATED","Phiên đã bị thu hồi.",401);await Exec("SELECT set_config('botzone.actor_role',$1,true),set_config('botzone.actor_id',$2,true)",actor.Role,actor.Id);}var result=await action();await tx.CommitAsync(Token);return result;}
}
public static class Values
{
 public static string S(this Dictionary<string,object?> r,string key,string fallback="")=>r.TryGetValue(key,out var v)&&v is not null?Convert.ToString(v,CultureInfo.InvariantCulture)??fallback:fallback;
 public static long L(this Dictionary<string,object?> r,string key)=>long.Parse(r.S(key,"0"),CultureInfo.InvariantCulture);
 public static int I(this Dictionary<string,object?> r,string key)=>checked((int)r.L(key));
 public static Guid G(this Dictionary<string,object?> r,string key)=>Guid.Parse(r.S(key));
 public static bool B(this Dictionary<string,object?> r,string key)=>r.TryGetValue(key,out var v)&&v is true;
 public static JsonElement J(this Dictionary<string,object?> r,string key)=>r.TryGetValue(key,out var v)&&v is JsonElement j?j:JsonSerializer.SerializeToElement(new{});
 public static string Text(this JsonElement j,string key,string? fallback=null){if(j.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String)return v.GetString()!;return fallback??throw new BusinessException("VALIDATION_ERROR",$"Thiếu hoặc sai trường {key}.",400);}
 public static Guid Id(this JsonElement j,string key){if(Guid.TryParse(j.Text(key),out var id))return id;throw new BusinessException("VALIDATION_ERROR",$"{key} phải là UUID.",400);}
 public static Guid? MaybeId(this JsonElement j,string key)=>j.TryGetProperty(key,out var v)&&v.ValueKind==JsonValueKind.String&&Guid.TryParse(v.GetString(),out var id)?id:null;
 public static int Int(this JsonElement j,string key,int fallback=0)=>j.TryGetProperty(key,out var v)&&v.TryGetInt32(out var n)?n:fallback;
 public static bool Bool(this JsonElement j,string key,bool fallback=false)=>j.TryGetProperty(key,out var v)&&v.ValueKind is JsonValueKind.True or JsonValueKind.False?v.GetBoolean():fallback;
 public static long Money(this JsonElement j,string key,long fallback=0){if(!j.TryGetProperty(key,out var v))return fallback;var s=v.ValueKind==JsonValueKind.String?v.GetString():null;if(s is not null&&s.Length<=15&&s.All(char.IsAsciiDigit)&&long.TryParse(s,out var n)&&n>=0)return n;throw new BusinessException("VALIDATION_ERROR",$"{key} phải là chuỗi số VND không âm.",400);}
 public static JsonElement Field(this JsonElement j,string key)=>j.TryGetProperty(key,out var v)?v:throw new BusinessException("VALIDATION_ERROR",$"Thiếu {key}.",400);
 public static JsonElement[] Array(this JsonElement j,string key)=>j.Field(key).ValueKind==JsonValueKind.Array?j.GetProperty(key).EnumerateArray().ToArray():throw new BusinessException("VALIDATION_ERROR",$"{key} phải là danh sách.",400);
 public static string Json(object? value)=>JsonSerializer.Serialize(value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
