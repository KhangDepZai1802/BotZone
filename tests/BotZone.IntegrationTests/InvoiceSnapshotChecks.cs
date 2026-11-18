using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using BotZone.Infrastructure.Invoices;
using BotZone.Infrastructure.Seeding;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PdfSharp.Pdf.IO;

internal static class InvoiceSnapshotChecks
{
    private sealed class ItemBarrier : DbCommandInterceptor
    {
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IsolationLevel? Isolation { get; private set; }
        private int intercepted;
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
        {
            // ReadDetail has already read totals and header in the real export transaction.
            // Pause exactly before its item query, not before the snapshot is established.
            if(command.CommandText.Contains("ii.variant_snapshot::text AS \"Variant\"") && Interlocked.Exchange(ref intercepted,1)==0)
            {
                Isolation=command.Transaction?.IsolationLevel;
                Reached.TrySetResult();
                await Continue.Task.WaitAsync(TimeSpan.FromSeconds(20),cancellationToken);
            }
            return result;
        }
    }

    public static async Task RunAsync(string connection,IConfiguration configuration,Func<string,Func<Task>,Task> test)
    {
        await test("DB-T14 export keeps one snapshot across an overlapping committed writer",async()=>
        {
            static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
            static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            var barrier=new ItemBarrier();
            await using var reader=new BotZoneDbContext(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection).AddInterceptors(barrier).Options);
            await using var writer=new BotZoneDbContext(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(connection).Options);
            await reader.Database.OpenConnectionAsync();await writer.Database.OpenConnectionAsync();
            var readerPid=await reader.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
            var writerPid=await writer.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
            Check(readerPid!=writerPid,"Reader and writer must use different PostgreSQL connections.");
            var invoiceId=DemoSeeder.Id("invoice:D01");
            var before=await writer.Database.SqlQueryRaw<DraftItem>("SELECT ii.id AS \"Id\",ii.variant_snapshot::text AS \"Variant\" FROM invoice_items ii JOIN invoices i ON i.id=ii.invoice_id WHERE i.id=@id AND i.lifecycle='draft'",new NpgsqlParameter("id",invoiceId)).SingleAsync();
            var marker="snapshot-writer-"+Guid.NewGuid().ToString("N");
            var owner=new Actor(DemoSeeder.OwnerId,"Owner","owner@botzone.local","owner",true);
            var service=new InvoiceQueryService(reader,configuration);
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var exportTask=service.CreateInvoiceExportAsync(new(){Kind="detail",InvoiceId=invoiceId},owner,timeout.Token);
            object? evidence=null;
            try
            {
                await barrier.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20));
                Check(barrier.Isolation==IsolationLevel.RepeatableRead,"The real export must hold RepeatableRead while the writer commits.");
                // D01 is the dedicated mutable draft fixture. Do not alter an issued snapshot.
                await writer.Database.ExecuteSqlRawAsync("UPDATE invoice_items SET variant_snapshot=COALESCE(NULLIF(variant_snapshot,'null'::jsonb),jsonb_build_object()) || jsonb_build_object('snapshotProbe',@marker::text) WHERE id=@id",new NpgsqlParameter("marker",marker),new NpgsqlParameter("id",before.Id));
                var committedAt=DateTimeOffset.UtcNow;
                var changed=await writer.Database.SqlQueryRaw<string>("SELECT variant_snapshot->>'snapshotProbe' AS \"Value\" FROM invoice_items WHERE id=@id",new NpgsqlParameter("id",before.Id)).SingleAsync();
                Check(changed==marker&&!exportTask.IsCompleted,"Writer committed while the export item query remained blocked.");
                barrier.Continue.TrySetResult();
                var export=await exportTask;
                Check(export.RowCount==1&&export.TotalVnd=="99000000"&&export.ReadAt<committedAt,"Export count, amount and readAt must describe the original snapshot.");
                var excel=await service.DownloadExportAsync(export.ExportId,"xlsx",owner);
                var pdf=await service.DownloadExportAsync(export.ExportId,"pdf",owner);
                using(var workbook=new XLWorkbook(new MemoryStream(excel.Buffer)))
                {
                    var items=workbook.Worksheet("ChiTietHoaDon");
                    Check(items.LastRowUsed()!.RowNumber()==2,"The XLSX contains exactly the original one item.");
                    using var actual=JsonDocument.Parse(items.Cell(2,6).GetString());using var original=JsonDocument.Parse(before.Variant);
                    Check(JsonElement.DeepEquals(actual.RootElement,original.RootElement),"XLSX item query must see old JSON despite committed writer.");
                    Check(!workbook.Worksheets.SelectMany(s=>s.CellsUsed()).Any(c=>c.GetString().Contains(marker)),"Writer marker must not leak into the snapshot file.");
                    var conditions=workbook.Worksheet("DieuKienLoc");
                    Check(conditions.RowsUsed().Single(r=>r.Cell(1).GetString()=="invoiceCount").Cell(2).GetString()=="1","XLSX metadata count agrees.");
                    Check(conditions.RowsUsed().Single(r=>r.Cell(1).GetString()=="reportOrListTotalVnd").Cell(2).GetValue<long>()==99_000_000,"XLSX metadata amount agrees.");
                }
                using(var document=PdfReader.Open(new MemoryStream(pdf.Buffer),PdfDocumentOpenMode.Import))Check(document.PageCount>=1,"The PDF opens as a real document.");
                var saved=await writer.Database.SqlQueryRaw<string>("SELECT file_manifest::text AS \"Value\" FROM invoice_exports WHERE id=@id AND status='ready'",new NpgsqlParameter("id",export.ExportId)).SingleAsync();
                using var manifest=JsonDocument.Parse(saved);var files=manifest.RootElement.GetProperty("files");
                Check(files.GetProperty("xlsx").GetProperty("checksum").GetString()==Hash(excel.Buffer)&&files.GetProperty("pdf").GetProperty("checksum").GetString()==Hash(pdf.Buffer),"Both actual files match the same saved export manifest.");
                Check(manifest.RootElement.GetProperty("datasetChecksum").GetString()!.Length==64&&manifest.RootElement.GetProperty("invoiceIds").EnumerateArray().Single().GetGuid()==invoiceId,"One dataset checksum and one invoice set govern both formats.");
                var fresh=await new InvoiceQueryService(writer,configuration).GetInvoiceDetailAsync(invoiceId,owner);
                Check(fresh.Items.Single().Variant.GetProperty("snapshotProbe").GetString()==marker,"A new snapshot sees the committed change.");
                evidence=new{export.ExportId,export.ReadAt,writerCommittedAt=committedAt,readerBackendPid=readerPid,writerBackendPid=writerPid,isolation=barrier.Isolation.ToString(),writerOverlapped=true,export.RowCount,export.TotalVnd,manifest=manifest.RootElement.Clone(),draftFixtureRestoredInFinally=true};
            }
            finally
            {
                barrier.Continue.TrySetResult();
                try{await exportTask;}catch{ /* Preserve the first assertion or command failure. */ }
                await writer.Database.ExecuteSqlRawAsync("UPDATE invoice_items SET variant_snapshot=@value::jsonb WHERE id=@id",new NpgsqlParameter("value",before.Variant),new NpgsqlParameter("id",before.Id));
            }
            await File.WriteAllTextAsync(Path.Combine(configuration["BOTZONE_ROOT"]!,"tests/BotZone.IntegrationTests/export-snapshot-result.json"),JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));
        });
    }
    private sealed class DraftItem{public Guid Id{get;set;}public string Variant{get;set;}="{}";}
}
