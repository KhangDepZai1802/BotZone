using System.Reflection;
using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Invoices;
using BotZone.Infrastructure.Seeding;
using BotZone.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

internal static class BaRequirementChecks
{
    public static async Task RunAsync(string connection,IConfiguration config,Func<string,Func<Task>,Task> test)
    {
        await using var db=new BotZoneDbContext(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection).Options);
        var sql=new SqlStore(db);var invoices=new InvoiceQueryService(db,config);
        var owner=new Actor(DemoSeeder.OwnerId,"Owner","owner@botzone.local","owner",true);var customer=owner with{Id=DemoSeeder.CustomerId,Role="customer"};
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        static InvoiceRuleFilter Rule(string field,string op,string value)=>new(field,op,JsonSerializer.SerializeToElement(value));
        var golden=new InvoiceGroupFilter("OR",Enumerable.Range(1,8).Select(n=>(FilterNode)Rule("invoiceNo","=",$"G{n:00}")).ToList());
        var query=new InvoiceQuery{Filter=golden,FromInclusive="2024-01-01",ToExclusive="2027-01-01"};
        await test("DB-T02 expired minimum and wrong-target vouchers rejected against real catalog",async()=>
        {
            await using var tx=await db.Database.BeginTransactionAsync();
            try
            {
                await sql.Exec("SELECT pg_advisory_xact_lock(12648321),set_config('botzone.actor_role','owner',true)");
                var commerce=new CommerceService(sql,config);
                foreach(var kind in new[]{"expired","minimum","target","valid"})
                {
                    var code="RULE"+Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
                    var promotion=(await sql.One("INSERT INTO promotions(code_normalized,max_uses,max_uses_per_customer) VALUES($1,10,10) RETURNING id",code)).G("id");
                    var version=(await sql.One("INSERT INTO promotion_versions(promotion_id,version_no,discount_type,value,min_purchase_vnd,starts_at,ends_at,all_products) VALUES($1,1,'fixed',1000,$2,$3,$4,false) RETURNING id",promotion,kind=="minimum"?999_000_000L:0L,DateTime.UtcNow.AddDays(-2),kind=="expired"?DateTime.UtcNow.AddDays(-1):DateTime.UtcNow.AddDays(1))).G("id");
                    await sql.Exec("INSERT INTO promotion_targets(promotion_version_id,product_id) VALUES($1,$2)",version,DemoSeeder.Id("product:"+(kind=="target"?"P13":"AP2")));
                    var input=JsonSerializer.SerializeToElement(new{items=new[]{new{variantId=DemoSeeder.Id("sku:AP2"),quantity=1,care=false}},promotionCode=code,points=0,creditVnd="0"});
                    if(kind=="valid")
                    {
                        var quote=JsonSerializer.SerializeToElement(await commerce.Quote(customer,input));Check(quote.GetProperty("discountVnd").GetString()=="1000","Control voucher proves the same fixture can successfully apply.");
                    }
                    else
                    {
                        var rejected=false;try{await commerce.Quote(customer,input);}catch(BusinessException e)when(e.Code=="PROMO_INVALID"){rejected=true;}
                        Check(rejected,"Invalid voucher must be rejected for "+kind);
                    }
                }
                await sql.Exec("SET CONSTRAINTS ALL IMMEDIATE");
            }
            finally{await tx.RollbackAsync();}
        });
        await test("DB-T05 every golden month and quarter issued adjustment and net including zeros",async()=>
        {
            var issued=new Dictionary<string,long>{{"2024-01-01",12_000_000},{"2024-11-01",16_000_000},{"2025-01-01",18_000_000},{"2025-04-01",18_000_000},{"2025-12-01",8_000_000},{"2026-01-01",1_830_000},{"2026-04-01",12_800_000},{"2026-11-01",10_500_000}};
            var adjustments=new Dictionary<string,long>{{"2025-05-01",2_000_000},{"2026-11-01",1_500_000}};
            foreach(var granularity in new[]{"month","quarter"})foreach(var metric in new[]{"issued","adjustments","net"})
            {
                var report=await invoices.GetInvoiceStatisticsAsync(new(){Query=query,Granularity=granularity,Metric=metric},owner);
                Check(report.Rows.Count==(granularity=="month"?36:12),"All periods are returned, including zero periods.");
                var expected=new Dictionary<string,long>();
                foreach(var source in new[]{("issued",issued),("adjustments",adjustments)})
                {
                    if(metric!="net"&&metric!=source.Item1)continue;
                    foreach(var entry in source.Item2)
                    {
                        var day=DateOnly.ParseExact(entry.Key,"yyyy-MM-dd");var period=granularity=="month"?day:new DateOnly(day.Year,(day.Month-1)/3*3+1,1);var key=period.ToString("yyyy-MM-dd");
                        expected[key]=expected.GetValueOrDefault(key)+entry.Value*(metric=="net"&&source.Item1=="adjustments"?-1:1);
                    }
                }
                foreach(var row in report.Rows)Check(long.Parse(row.TotalVnd)==expected.GetValueOrDefault(row.Period),$"Mismatch {granularity}/{metric}/{row.Period}.");
            }
        });
        await test("DB-T06 current staff rename and order reassignment do not move invoice snapshot revenue",async()=>
        {
            await using var tx=await db.Database.BeginTransactionAsync();
            try
            {
                await sql.Exec("SELECT pg_advisory_xact_lock(12648321);UPDATE \"user\" SET name='Changed current staff name' WHERE id='demo-nv01';UPDATE orders SET responsible_staff_id='demo-nv02' WHERE id=$1",DemoSeeder.Id("order:G03"));
                Check((await sql.One("SELECT responsible_staff_id FROM orders WHERE id=$1",DemoSeeder.Id("order:G03"))).S("responsible_staff_id")=="demo-nv02","Current order reassignment really occurred inside fixture transaction.");
                // Invoke the actual internal query body to keep a test-owned rollback
                // transaction. Its public wrapper starts a new read-only transaction.
                var method=typeof(InvoiceQueryService).GetMethod("ReadStatistics",BindingFlags.Instance|BindingFlags.NonPublic)??throw new Exception("Report query entry missing.");
                var report=await (Task<InvoiceStatisticsResult>)method.Invoke(invoices,[new ReportRequest{Query=query,GroupBy="staff",Granularity="year"},owner,CancellationToken.None])!;
                Check(report.Rows.Where(r=>r.DimensionId=="demo-nv01").Sum(r=>long.Parse(r.TotalVnd))==42_800_000,"Historical NV01 credit does not move.");
                Check(report.Rows.Where(r=>r.DimensionId=="demo-nv02").Sum(r=>long.Parse(r.TotalVnd))==44_500_000,"Current handler does not inherit historical sales.");
                Check(report.Rows.Where(r=>r.DimensionId=="demo-nv01").All(r=>r.DimensionName=="Nhân viên 01"),"Historical staff labels come from issued snapshots.");
                await sql.Exec("SET CONSTRAINTS ALL IMMEDIATE");
            }
            finally{await tx.RollbackAsync();}
        });
        await test("DB-T09 nested precedence and equality inequality produce exact distinct golden IDs",async()=>
        {
            var a=Rule("grandTotalVnd",">","10000000");var b=Rule("responsibleStaffId","=","demo-nv01");var c=Rule("grandTotalVnd","<","9000000");
            async Task Assert(FilterNode expression,string expected)
            {
                var result=await invoices.ListInvoicesAsync(query with{Filter=new InvoiceGroupFilter("AND",[golden,expression])},owner);
                Check(string.Join(',',result.Items.Select(i=>i.InvoiceNo).Order())==expected,"AST returned unexpected invoice set.");
            }
            await Assert(new InvoiceGroupFilter("AND",[a,new InvoiceGroupFilter("OR",[b,c])]),"G01,G03,G07");
            await Assert(new InvoiceGroupFilter("OR",[new InvoiceGroupFilter("AND",[a,b]),c]),"G01,G03,G05,G06,G07");
            await Assert(Rule("grandTotalVnd","=","18000000"),"G03,G04");
            await Assert(Rule("grandTotalVnd","<>","18000000"),"G01,G02,G05,G06,G07,G08");
        });
    }
}
