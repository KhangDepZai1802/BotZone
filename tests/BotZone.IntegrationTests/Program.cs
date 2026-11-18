using System.Text;
using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Data.Entities;
using BotZone.Infrastructure.Invoices;
using BotZone.Infrastructure.Seeding;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PdfSharp.Pdf.IO;

var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
var database=Environment.GetEnvironmentVariable("BOTZONE_TEST_DATABASE")??"botzone_baseline_test_20261113135743_c0d401c6";
if(!database.StartsWith("botzone_baseline_test_",StringComparison.Ordinal)&&!database.StartsWith("botzone_integration_test_",StringComparison.Ordinal))throw new InvalidOperationException("Integration runner only permits explicitly named test clone databases.");
var connection=new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BOTZONE_TEST_CONNECTION")??"Host=127.0.0.1;Port=54329;Username=botzone_app;Password=botzone_app_local"){Database=database};
await using var db=new BotZoneDbContext(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection.ConnectionString).Options);
var configuration=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"BOTZONE_ROOT",root},{"UPLOAD_DIR","uploads/integration-tests"}}).Build();
var service=new InvoiceQueryService(db,configuration);
var owner=new Actor(DemoSeeder.OwnerId,"Owner","owner@botzone.local","owner",true);
var staff=owner with{Id="demo-nv01",Role="order_staff"};
var customer=owner with{Id=DemoSeeder.CustomerId,Role="customer"};
var results=new List<object>();var started=DateTimeOffset.UtcNow;
void Equal<T>(T expected,T actual){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"Expected {expected}, got {actual}.");}
void True(bool condition,string message="Assertion failed"){if(!condition)throw new Exception(message);}
void Throws(Action action){try{action();}catch(BusinessException){return;}throw new Exception("Expected BusinessException.");}
async Task Denied(Func<Task> action){try{await action();}catch(BusinessException e)when(e.Status is 403 or 404){return;}throw new Exception("Expected 403/404.");}
async Task Test(string name,Func<Task> action)
{
    var time=DateTimeOffset.UtcNow;
    try{await action();results.Add(new{name,passed=true,milliseconds=(DateTimeOffset.UtcNow-time).TotalMilliseconds});Console.WriteLine("PASS "+name);}
    catch(Exception ex){results.Add(new{name,passed=false,error=ex.ToString()});Console.Error.WriteLine("FAIL "+name+": "+ex);throw;}
}
InvoiceRuleFilter Rule(string field,string op,object? value)=>new(field,op,JsonSerializer.SerializeToElement(value));
var goldenFilter=new InvoiceGroupFilter("OR",Enumerable.Range(1,8).Select(n=>(FilterNode)Rule("invoiceNo","=",$"G{n:00}")).ToList());
var golden=new InvoiceQuery{Filter=goldenFilter,Sort=[new("invoiceNo","asc")]};
ReportRequest Report(string metric="issued",string group="period",string granularity="year",InvoiceQuery? query=null)=>new(){Query=query??golden with{FromInclusive="2024-01-01",ToExclusive="2027-01-01"},GroupBy=group,Granularity=granularity,Metric=metric};
try
{
    var seedProfile=args.Contains("--catalog-only")?"catalog":"golden";
    await Test("seed "+seedProfile,async()=>{var result=await new DemoSeeder(db).SeedAsync(seedProfile);Console.WriteLine($"Seed {seedProfile}: {(result.AlreadyApplied?"already applied":"applied")}");});
    if(args.Contains("--catalog-only"))return;
    if(args.Contains("--seed-only"))return;
    await Test("seed idempotent; Identity hashes verify",async()=>
    {
        var before=await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM invoices").SingleAsync();
        True((await new DemoSeeder(db).SeedAsync("golden")).AlreadyApplied);
        Equal(before,await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM invoices").SingleAsync());
        var user=await db.Users.SingleAsync(u=>u.Id==DemoSeeder.OwnerId);True(user.EmailConfirmed);True(new PasswordHasher<User>().VerifyHashedPassword(user,user.PasswordHash!,DemoSeeder.Password)!=PasswordVerificationResult.Failed);
    });
    await Test("TS01 parameterization and field whitelist",()=>
    {
        const string payload="' OR 1=1; DROP TABLE invoices; --";
        var built=InvoicePredicateBuilder.Build(new(){Filter=Rule("invoiceNo","=",payload)},owner);
        True(!built.Where.Contains(payload));Equal(payload,built.Parameters.Values.Last());
        Throws(()=>InvoicePredicateBuilder.Validate(new(){Sort=[new("issued_at; DROP TABLE invoices","asc")]}));
        Throws(()=>InvoicePredicateBuilder.Validate(new(){Filter=Rule("__proto__","=","x")}));return Task.CompletedTask;
    });
    await Test("TS02 report permission outside OR and NOT",()=>
    {
        var filter=new InvoiceGroupFilter("OR",[Rule("responsibleStaffId","=","demo-nv02"),new InvoiceNotFilter(Rule("grandTotalVnd",">","1000000"))]);
        var built=InvoicePredicateBuilder.Build(new(){Filter=filter},staff,report:true);
        True(built.Where.StartsWith("(i.responsible_staff_id=@p0) AND"));Equal(staff.Id,built.Parameters.Values[0]);True(built.Where.Contains(" OR "));return Task.CompletedTask;
    });
    await Test("TS03 customer detail only; staff role denies",()=>
    {
        Throws(()=>InvoicePredicateBuilder.Build(new(),customer));True(InvoicePredicateBuilder.Build(new(),customer,allowCustomer:true).Where.Contains("i.customer_id=@p0"));
        foreach(var role in new[]{"content_staff","warehouse_staff"})Throws(()=>InvoicePredicateBuilder.Build(new(),owner with{Role=role}));return Task.CompletedTask;
    });
    await Test("TS04 not_contains uses NOT EXISTS",()=>
    {
        var built=InvoicePredicateBuilder.Build(new(){Filter=new InvoiceProductFilter(DemoSeeder.Id("product:AP2"),"not_contains")},owner);
        True(built.Where.Contains("NOT EXISTS (SELECT 1 FROM invoice_items"));Equal(DemoSeeder.Id("product:AP2"),built.Parameters.Values.Last());return Task.CompletedTask;
    });
    await Test("TS05 Vietnam day equality half-open",()=>
    {
        var built=InvoicePredicateBuilder.Build(new(){Filter=Rule("issuedAt","=","2026-01-01")},owner);
        Equal(DateTimeOffset.Parse("2025-12-31T17:00:00Z"),built.Parameters.Values[1]);Equal(DateTimeOffset.Parse("2026-01-01T17:00:00Z"),built.Parameters.Values[2]);return Task.CompletedTask;
    });
    await Test("TS06 Vietnam date operator boundaries",()=>
    {
        foreach(var op in new[]{">","<",">=","<="})
        {
            var built=InvoicePredicateBuilder.Build(new(){Filter=Rule("issuedAt",op,"2026-01-01")},owner);
            Equal(DateTimeOffset.Parse(op is ">" or "<="?"2026-01-01T17:00:00Z":"2025-12-31T17:00:00Z"),built.Parameters.Values[1]);
        }
        Equal(DateTimeOffset.Parse("2026-01-01T00:00:00.1234560Z"),InvoicePredicateBuilder.ParseInstant("2026-01-01T00:00:00.1234567Z"));
        Throws(()=>InvoicePredicateBuilder.ParseInstant("2026-02-30"));Throws(()=>InvoicePredicateBuilder.ParseInstant("2026-01-01T00:00:00"));return Task.CompletedTask;
    });
    await Test("TS07 BIGINT exact; unsafe JSON number rejected",()=>
    {
        Equal(long.MaxValue,InvoicePredicateBuilder.ParseMoney(JsonSerializer.SerializeToElement("9223372036854775807")));
        Throws(()=>InvoicePredicateBuilder.ParseMoney(JsonSerializer.SerializeToElement(9007199254740992L)));
        Throws(()=>InvoicePredicateBuilder.ParseMoney(JsonSerializer.SerializeToElement("9223372036854775808")));return Task.CompletedTask;
    });
    await Test("TS08 explicit null and NOT semantics",()=>
    {
        var built=InvoicePredicateBuilder.Build(new(){Filter=new InvoiceNullFilter("responsibleStaffId",true)},owner,lifecycle:"any");True(built.Where.Contains("i.responsible_staff_id IS NULL"));Equal(0,built.Parameters.Values.Count);
        Throws(()=>InvoicePredicateBuilder.Validate(new(){Filter=Rule("invoiceNo","=",null)}));return Task.CompletedTask;
    });
    await Test("TS09 bounds depth and leaves",()=>
    {
        FilterNode filter=Rule("grandTotalVnd","=","0");for(var n=0;n<4;n++)filter=new InvoiceNotFilter(filter);
        Throws(()=>InvoicePredicateBuilder.Validate(new(){Filter=filter}));Throws(()=>InvoicePredicateBuilder.Validate(new(){Filter=new InvoiceGroupFilter("AND",Enumerable.Range(0,31).Select(_=>(FilterNode)Rule("grandTotalVnd","=","0")).ToList())}));return Task.CompletedTask;
    });
    await Test("TS10 stable sort and report event window",()=>
    {
        var built=InvoicePredicateBuilder.Build(new(){FromInclusive="2025-05-01",ToExclusive="2025-06-01",Sort=[new("grandTotalVnd","asc")]},owner,report:true,includeWindow:false);
        Equal("i.grand_total_vnd ASC NULLS LAST, i.id ASC",built.OrderBy);True(!built.Where.Contains("issued_at"));return Task.CompletedTask;
    });
    await Test("golden 8 issued / 97,130,000 and draft exclusions",async()=>
    {
        var list=await service.ListInvoicesAsync(golden,owner);Equal(8L,list.MatchedCount);Equal("97130000",list.MatchedTotalVnd);Equal(8,list.Items.Count);
        Equal("unpaid",list.Items.Single(i=>i.InvoiceNo=="G02").PaymentStatus);Equal("1830000",list.Items.Single(i=>i.InvoiceNo=="G06").GrandTotalVnd);
        var draft=await service.GetInvoiceDetailAsync(DemoSeeder.Id("invoice:D01"),owner);Equal("99000000",draft.Invoice.GrandTotalVnd);Equal("draft",draft.Invoice.Lifecycle);
    });
    await Test("golden exact logical filter 46,800,000",async()=>
    {
        var filter=new InvoiceGroupFilter("AND",[Rule("grandTotalVnd",">","10000000"),Rule("grandTotalVnd","<","20000000"),new InvoiceGroupFilter("OR",[Rule("responsibleStaffId","=","demo-nv01"),Rule("responsibleStaffId","=","demo-nv02")]),new InvoiceProductFilter(DemoSeeder.Id("product:AP2"),"not_contains")]);
        var result=await service.ListInvoicesAsync(new(){Filter=filter},owner);Equal(3L,result.MatchedCount);Equal("46800000",result.MatchedTotalVnd);Equal("G02,G03,G07",string.Join(',',result.Items.Select(i=>i.InvoiceNo).Order()));
    });
    await Test("issued year totals and Vietnam boundary",async()=>
    {
        var result=await service.GetInvoiceStatisticsAsync(Report(),owner);Equal("97130000",result.TotalVnd);Equal("28000000,44000000,25130000",string.Join(',',result.Rows.Select(r=>r.TotalVnd)));
        var day=await service.ListInvoicesAsync(golden with{Filter=Rule("issuedAt","=","2026-01-01")},owner);Equal(1L,day.MatchedCount);Equal("G06",day.Items.Single().InvoiceNo);
    });
    await Test("staff totals and permission scope",async()=>
    {
        var report=await service.GetInvoiceStatisticsAsync(Report(group:"staff"),owner);
        Equal(42_800_000L,report.Rows.Where(r=>r.DimensionId=="demo-nv01").Sum(r=>long.Parse(r.TotalVnd)));Equal(44_500_000L,report.Rows.Where(r=>r.DimensionId=="demo-nv02").Sum(r=>long.Parse(r.TotalVnd)));Equal(9_830_000L,report.Rows.Where(r=>r.DimensionId=="demo-nv03").Sum(r=>long.Parse(r.TotalVnd)));
        Equal("42800000",(await service.GetInvoiceStatisticsAsync(Report(),staff)).TotalVnd);
    });
    await Test("product totals exclude Care and shipping",async()=>
    {
        var result=await service.GetInvoiceStatisticsAsync(Report(group:"product"),owner);
        foreach(var pair in new[]{("P13",43_000_000L),("P14",41_000_000L),("AP2",10_800_000L)})Equal(pair.Item2,result.Rows.Where(r=>r.DimensionId==DemoSeeder.Id("product:"+pair.Item1).ToString()).Sum(r=>long.Parse(r.TotalVnd)));
        Equal("94800000",result.TotalVnd);
    });
    await Test("credit, net, receipts and original-source refunds",async()=>
    {
        Equal("3500000",(await service.GetInvoiceStatisticsAsync(Report("adjustments"),owner)).TotalVnd);Equal("93630000",(await service.GetInvoiceStatisticsAsync(Report("net"),owner)).TotalVnd);
        Equal("81130000",(await service.GetInvoiceStatisticsAsync(Report("receipts"),owner)).TotalVnd);
        var refunds=await service.GetInvoiceStatisticsAsync(Report("refunds"),owner);Equal("3500000",refunds.TotalVnd);Equal(450_000L,refunds.Rows.Sum(r=>long.Parse(r.StoreCreditVnd)));Equal(3_050_000L,refunds.Rows.Sum(r=>long.Parse(r.CashDemoVnd)));
        var detail=await service.GetInvoiceDetailAsync(DemoSeeder.Id("invoice:G08"),owner);Equal("3150000",detail.Receipts.Single(r=>r.GetProperty("tender_kind").GetString()=="store_credit").GetProperty("amount_vnd").GetString());Equal("1500000",detail.Invoice.RefundedVnd);Equal(3,detail.Items.Count);
    });
    await Test("adjustment event date; zero months",async()=>
    {
        var may=golden with{FromInclusive="2025-05-01",ToExclusive="2025-06-01"};var result=await service.GetInvoiceStatisticsAsync(Report("adjustments",granularity:"month",query:may),owner);Equal("2000000",result.TotalVnd);Equal(1,result.InvoiceIds.Count);
        var annual=await service.GetInvoiceStatisticsAsync(Report(granularity:"month",query:golden with{FromInclusive="2024-01-01",ToExclusive="2025-01-01"}),owner);Equal(12,annual.Rows.Count);Equal(10,annual.Rows.Count(r=>r.TotalVnd=="0"));
    });
    await Test("customer ownership and non-financial roles",async()=>
    {
        await service.GetInvoiceDetailAsync(DemoSeeder.Id("invoice:G01"),customer);
        await Denied(async()=>await service.GetInvoiceDetailAsync(DemoSeeder.Id("invoice:G01"),customer with{Id="demo-customer-other"}));
        await Denied(async()=>await service.ListInvoicesAsync(golden,customer));
        foreach(var role in new[]{"content_staff","warehouse_staff"})await Denied(async()=>await service.ListInvoicesAsync(golden,owner with{Role=role}));
        await Denied(async()=>await service.GetInvoiceDetailAsync(DemoSeeder.Id("invoice:D01"),customer));
    });
    await Test("75 invoice pagination fixture; exact sum across pages",async()=>
    {
        await new DemoSeeder(db).SeedAsync("pagination");var ids=new HashSet<Guid>();long sum=0;
        for(var page=1;page<=4;page++){var list=await service.ListInvoicesAsync(new(){QuickSearch="PAG",Page=page,PageSize=20,Sort=[new("issuedAt","asc")]},owner);Equal(75L,list.MatchedCount);Equal("7502850",list.MatchedTotalVnd);foreach(var row in list.Items){True(ids.Add(row.Id));sum+=long.Parse(row.GrandTotalVnd);}}
        Equal(75,ids.Count);Equal(7_502_850L,sum);Equal("97130000",(await service.ListInvoicesAsync(golden,owner)).MatchedTotalVnd);
    });
    await Test("list XLSX/PDF same snapshot, 75 rows not current page",async()=>
    {
        var export=await service.CreateInvoiceExportAsync(new(){Kind="list",Query=new(){QuickSearch="PAG",Page=2,PageSize=20}},owner);Equal(75L,export.RowCount);Equal("7502850",export.TotalVnd);
        var excel=await service.DownloadExportAsync(export.ExportId,"xlsx",owner);using var workbook=new XLWorkbook(new MemoryStream(excel.Buffer));True(workbook.Worksheets.Any(s=>s.Name=="HoaDon"));True(workbook.Worksheet("HoaDon").LastRowUsed()!.RowNumber()>=76);
        var pdf=await service.DownloadExportAsync(export.ExportId,"pdf",owner);using var doc=PdfReader.Open(new MemoryStream(pdf.Buffer),PdfDocumentOpenMode.Import);True(doc.PageCount>=2);Equal("%PDF",Encoding.ASCII.GetString(pdf.Buffer,0,4));
        await Denied(async()=>await service.DownloadExportAsync(export.ExportId,"xlsx",owner with{Role="content_staff"}));
    });
    await Test("detail and report exports contain actual rows and serial text",async()=>
    {
        var detail=await service.CreateInvoiceExportAsync(new(){Kind="detail",InvoiceId=DemoSeeder.Id("invoice:G01")},customer);
        var file=await service.DownloadExportAsync(detail.ExportId,"xlsx",customer);using var wb=new XLWorkbook(new MemoryStream(file.Buffer));True(wb.Worksheets.SelectMany(s=>s.CellsUsed()).Any(c=>c.GetString().Contains("000P13")));
        await Denied(async()=>await service.DownloadExportAsync(detail.ExportId,"pdf",customer with{Id="demo-customer-other"}));
        var report=await service.CreateInvoiceExportAsync(new(){Kind="report",Query=golden with{FromInclusive="2024-01-01",ToExclusive="2027-01-01"},Metric="net",Granularity="year"},owner);Equal("93630000",report.TotalVnd);Equal(8L,report.RowCount);var reportPdf=await service.DownloadExportAsync(report.ExportId,"pdf",owner);True(reportPdf.Buffer.Length>1000);
    });
    await Test("legacy null variant snapshots render detail PDF",async()=>
    {
        var legacy=await db.Database.SqlQueryRaw<Guid>("SELECT ii.invoice_id AS \"Value\" FROM invoice_items ii WHERE ii.variant_snapshot='null'::jsonb AND ii.line_type='shipping' LIMIT 1").ToListAsync();
        var id=legacy.Count>0?legacy[0]:DemoSeeder.Id("invoice:D01");
        string? original=null;
        if(legacy.Count==0)
        {
            original=await db.Database.SqlQueryRaw<string>("SELECT variant_snapshot::text AS \"Value\" FROM invoice_items WHERE invoice_id=@id",new NpgsqlParameter("id",id)).SingleAsync();
            await db.Database.ExecuteSqlRawAsync("UPDATE invoice_items SET variant_snapshot='null'::jsonb WHERE invoice_id=@id",new NpgsqlParameter("id",id));
        }
        try
        {
            var export=await service.CreateInvoiceExportAsync(new(){Kind="detail",InvoiceId=id},owner);var pdf=await service.DownloadExportAsync(export.ExportId,"pdf",owner);using var document=PdfReader.Open(new MemoryStream(pdf.Buffer),PdfDocumentOpenMode.Import);True(document.PageCount>=1);
        }
        finally
        {
            if(original is not null)await db.Database.ExecuteSqlRawAsync("UPDATE invoice_items SET variant_snapshot=@value::jsonb WHERE invoice_id=@id",new NpgsqlParameter("value",original),new NpgsqlParameter("id",id));
        }
    });
    await Test("PDF pagination review artifacts include complete list report and detail",async()=>
    {
        var directory=Path.Combine(root,"artifacts/pdf-review-fixed");Directory.CreateDirectory(directory);var files=new List<object>();
        foreach(var sample in new (string Name,ExportRequest Request)[]{("invoices",new(){Kind="list",Formats=["pdf"]}),("report",new(){Kind="report",Formats=["pdf"],Query=golden with{FromInclusive="2024-01-01",ToExclusive="2027-01-01"},Metric="net",Granularity="month"}),("detail",new(){Kind="detail",Formats=["pdf"],InvoiceId=DemoSeeder.Id("invoice:G08")})})
        {
            var export=await service.CreateInvoiceExportAsync(sample.Request,owner);var file=await service.DownloadExportAsync(export.ExportId,"pdf",owner);var path=Path.Combine(directory,sample.Name+".pdf");await File.WriteAllBytesAsync(path,file.Buffer);using var pdf=PdfReader.Open(new MemoryStream(file.Buffer),PdfDocumentOpenMode.Import);True(pdf.PageCount>0);files.Add(new{sample.Name,export.ExportId,export.ReadAt,export.RowCount,export.TotalVnd,pages=pdf.PageCount,bytes=file.Buffer.Length});
        }
        await File.WriteAllTextAsync(Path.Combine(directory,"export-results.json"),JsonSerializer.Serialize(new{checkedAt=DateTimeOffset.UtcNow,files},new JsonSerializerOptions{WriteIndented=true}));
    });
    await DatabaseGuardChecks.RunAsync(connection.ConnectionString,Test);
    await AfterSalesChecks.RunAsync(db,configuration,Test);
    await ConcurrencyChecks.RunAsync(connection.ConnectionString,configuration,Test);
    await InvoiceSnapshotChecks.RunAsync(connection.ConnectionString,configuration,Test);
    await ReadProjectionChecks.RunAsync(connection.ConnectionString,configuration,Test);
    await PaymentDispatchChecks.RunAsync(connection.ConnectionString,configuration,Test);
    await BaRequirementChecks.RunAsync(connection.ConnectionString,configuration,Test);
    await InvoiceIssueChecks.RunAsync(connection.ConnectionString,configuration,Test);
    await QueryPlanEvidence.RunAsync(connection.ConnectionString,configuration,Test);
}
catch(Exception)
{
    Environment.ExitCode=1;
}
finally
{
    var evidence=new{database,startedAt=started,finishedAt=DateTimeOffset.UtcNow,tests=results};
    var path=Path.Combine(root,"tests/BotZone.IntegrationTests/last-result.json");Directory.CreateDirectory(Path.GetDirectoryName(path)!);await File.WriteAllTextAsync(path,JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Evidence: "+path);
}
