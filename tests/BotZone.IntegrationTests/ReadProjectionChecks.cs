using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Seeding;
using BotZone.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

internal static class ReadProjectionChecks
{
    public static async Task RunAsync(string connection,IConfiguration configuration,Func<string,Func<Task>,Task> test)
    {
        await using var db=new BotZoneDbContext(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection).Options);
        var service=new ReadService(new SqlStore(db));
        await test("warehouse order projection keeps packing identifiers and excludes financial fields",async()=>
        {
            static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
            static void NoFinancialFields(JsonElement node)
            {
                if(node.ValueKind==JsonValueKind.Object)foreach(var field in node.EnumerateObject())
                {
                    Check(!field.Name.EndsWith("_vnd",StringComparison.Ordinal)&&field.Name is not ("payment_method" or "payment_status" or "policy_snapshot" or "attempts" or "receipts" or "invoice" or "redeemed_points"),"Warehouse payload leaked financial field: "+field.Name);
                    NoFinancialFields(field.Value);
                }
                else if(node.ValueKind==JsonValueKind.Array)foreach(var item in node.EnumerateArray())NoFinancialFields(item);
            }
            var actor=new Actor("demo-warehouse","Warehouse","warehouse@botzone.local","warehouse_staff",true);
            var list=JsonSerializer.SerializeToElement(await service.Orders(actor));NoFinancialFields(list);
            Check(list.GetProperty("orders").GetArrayLength()>0,"Real order rows exist.");
            var detail=JsonSerializer.SerializeToElement(await service.OrderDetail(actor,DemoSeeder.Id("order:G01")));NoFinancialFields(detail);
            Check(detail.GetProperty("items").EnumerateArray().All(i=>i.TryGetProperty("quantity",out _)&&i.TryGetProperty("sku",out _)),"Packing rows retain SKU and quantity.");
            Check(detail.GetProperty("units").EnumerateArray().Any(u=>u.GetProperty("identifiers").GetArrayLength()>0),"Allocated serial identifiers remain available for packing.");
            Check(detail.GetProperty("order").TryGetProperty("address_snapshot",out _),"Shipping address remains available.");
        });
        await test("BA query-catalog SQL executes read-only and returns expected golden sets",async()=>
        {
            await using var pg=new NpgsqlConnection(connection);await pg.OpenAsync();
            await using var command=new NpgsqlCommand(await File.ReadAllTextAsync(Path.Combine(configuration["BOTZONE_ROOT"]!,"docs/database/query-catalog.sql")),pg);
            await using var rows=await command.ExecuteReaderAsync();var sets=new List<object>();var counts=new List<int>();
            do
            {
                if(rows.FieldCount==0)continue;
                var columns=Enumerable.Range(0,rows.FieldCount).Select(rows.GetName).ToArray();int count=0;
                while(await rows.ReadAsync())
                {
                    count++;
                    if(columns.SequenceEqual(new[]{"invoice_count","total_vnd"})&&(rows.GetInt64(0)!=8||rows.GetDecimal(1)!=97_130_000))throw new Exception("Golden SQL header mismatch.");
                }
                counts.Add(count);sets.Add(new{columns,rowCount=count});
            }while(await rows.NextResultAsync());
            if(counts.Count!=9||counts[0]!=1||counts[1]!=1||counts[2]!=8||counts[3]!=36||counts[4]!=3||counts[5]!=5||counts[6]!=3||counts[8]!=2)throw new Exception("Unexpected query-catalog result counts: "+string.Join(',',counts));
            await File.WriteAllTextAsync(Path.Combine(configuration["BOTZONE_ROOT"]!,"tests/BotZone.IntegrationTests/query-catalog-result.json"),JsonSerializer.Serialize(new{checkedAt=DateTimeOffset.UtcNow,database=pg.Database,readOnly=true,sets},new JsonSerializerOptions{WriteIndented=true}));
        });
    }
}
