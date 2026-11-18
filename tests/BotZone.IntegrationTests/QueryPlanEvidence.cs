using System.Data.Common;
using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Invoices;
using BotZone.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;

internal static class QueryPlanEvidence
{
    private sealed record Parameter(string Name,NpgsqlDbType Type,object Value);
    private sealed record Captured(string Name,string Sql,List<Parameter> Parameters);
    private sealed class Capture : DbCommandInterceptor
    {
        public string Name="";
        public List<Captured> Queries=[];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
        {
            if(command.CommandText.Contains("periods AS (")||Name=="invoice-list"&&command.CommandText.Contains("AS \"CustomerName\""))
                Queries.Add(new(Name,command.CommandText,command.Parameters.Cast<NpgsqlParameter>().Select(p=>new Parameter(p.ParameterName,p.NpgsqlDbType,p.Value??DBNull.Value)).ToList()));
            return base.ReaderExecutingAsync(command,eventData,result,cancellationToken);
        }
    }
    public static async Task RunAsync(string connection,IConfiguration configuration,Func<string,Func<Task>,Task> test)
    {
        await test("EXPLAIN ANALYZE BUFFERS actual list and three report queries",async()=>
        {
            var capture=new Capture();await using var db=new BotZoneDbContext(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection).AddInterceptors(capture).Options);var service=new InvoiceQueryService(db,configuration);var owner=new Actor(DemoSeeder.OwnerId,"Owner","owner@botzone.local","owner",true);
            capture.Name="invoice-list";await service.ListInvoicesAsync(new(){FromInclusive="2024-01-01",ToExclusive="2027-01-01",PageSize=20},owner);
            foreach(var group in new[]{"period","staff","product"}){capture.Name="report-"+group;await service.GetInvoiceStatisticsAsync(new(){Query=new(){FromInclusive="2024-01-01",ToExclusive="2027-01-01"},GroupBy=group,Granularity="month",Metric="net"},owner);}
            if(capture.Queries.Count!=4)throw new Exception("Expected four actual executed queries to explain.");
            await using var pg=new NpgsqlConnection(connection);await pg.OpenAsync();var cases=new List<object>();
            foreach(var query in capture.Queries)
            {
                await using var cmd=new NpgsqlCommand("EXPLAIN (ANALYZE,BUFFERS,FORMAT JSON) "+query.Sql,pg);foreach(var parameter in query.Parameters)cmd.Parameters.Add(new NpgsqlParameter(parameter.Name,parameter.Type){Value=parameter.Value});var json=(string)(await cmd.ExecuteScalarAsync())!;var plan=JsonDocument.Parse(json).RootElement.Clone();
                cases.Add(new{name=query.Name,sql=query.Sql,parameters=query.Parameters.Select(p=>new{name=p.Name,type=p.Type.ToString(),value=p.Value is DBNull?null:p.Value}).ToArray(),plan});
            }
            var counts=await db.Database.SqlQueryRaw<string>("SELECT json_build_object('invoices',count(*),'issued',count(*) FILTER(WHERE lifecycle='issued'))::text AS \"Value\" FROM invoices").SingleAsync();var path=Path.Combine(configuration["BOTZONE_ROOT"]!,"tests/BotZone.IntegrationTests/query-plans.json");await File.WriteAllTextAsync(path,JsonSerializer.Serialize(new{capturedAt=DateTimeOffset.UtcNow,database=new NpgsqlConnectionStringBuilder(connection).Database,counts=JsonDocument.Parse(counts).RootElement,queries=cases},new JsonSerializerOptions{WriteIndented=true}));
        });
    }
}
