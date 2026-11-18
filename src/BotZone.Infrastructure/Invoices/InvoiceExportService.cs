using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BotZone.Contracts;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace BotZone.Infrastructure.Invoices;

internal sealed record InvoiceExportDataset(string Kind,InvoiceQuery Query,DateTimeOffset ReadAt,long RowCount,string TotalVnd,
    List<InvoiceListItemDto> Invoices,List<InvoiceDetailItemDto> Items,InvoiceDetailDto? Detail,InvoiceStatisticsResult? Statistics,string Locale);
internal sealed record ExportFileEntry(string Filename,long Bytes,string Checksum,string ContentType);
internal sealed record ExportFileManifest(Dictionary<string,ExportFileEntry> Files,List<Guid> InvoiceIds,string DatasetChecksum,string Kind,string Locale);

// SQL interpolation here joins only static SQL, validated predicate/sort
// fragments and generated parameter names. Request JSON, manifests, actor IDs,
// amounts and file IDs are Npgsql parameters, never literal SQL values.
#pragma warning disable EF1002
public sealed class InvoiceExportService(InvoiceQueryService queries,IConfiguration configuration)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web){AllowOutOfOrderMetadataProperties=true};
    private static readonly CultureInfo Vi=CultureInfo.GetCultureInfo("vi-VN");
    private static readonly CultureInfo En=CultureInfo.GetCultureInfo("en-GB");
    private readonly string _root=Path.GetFullPath(configuration["BOTZONE_ROOT"] ?? Environment.GetEnvironmentVariable("BOTZONE_ROOT") ?? Directory.GetCurrentDirectory());
    private string ExportDirectory => Path.Combine(Path.GetFullPath(configuration["UPLOAD_DIR"] ?? "uploads",_root),"export-private");
    private string FontPath => Path.GetFullPath(configuration["PDF_FONT_PATH"] ?? "public/fonts/NotoSans-Regular.ttf",_root);
    private static string Hash(byte[] value)=>Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
    private static string SumMoney(IEnumerable<string> values)=>InvoiceQueryService.SumMoney(values);
    private static string ContentType(string format)=>format=="xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf";

    public async Task<InvoiceExportResult> CreateAsync(ExportRequest request,Actor actor,CancellationToken cancellationToken=default)
    {
        Validate(request,actor);
        var data=await queries.Snapshot(()=>Materialize(request,actor,cancellationToken),cancellationToken);
        if(!long.TryParse(data.TotalVnd,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out _))
            throw new BusinessException("EXPORT_LIMIT","Tổng lượt xuất vượt miền BIGINT. Hãy thu hẹp điều kiện; dữ liệu chưa bị làm tròn.");
        var exportId=Guid.NewGuid();var expiresAt=DateTimeOffset.UtcNow.AddHours(24);
        var insert=new SqlParameters();var id=insert.Add(exportId);var user=insert.Add(actor.Id);var json=insert.Add(JsonSerializer.Serialize(request,Json));
        var readAt=insert.Add(data.ReadAt);var count=insert.Add(checked((int)data.RowCount));var amount=insert.Add(data.TotalVnd);var expiry=insert.Add(expiresAt);var role=insert.Add(actor.Role);
        await queries.Db.Database.ExecuteSqlRawAsync($"INSERT INTO invoice_exports(id,requested_by_user_id,query_json,read_at,row_count,total_vnd,status,expires_at,role_snapshot) VALUES({id},{user},{json}::jsonb,{readAt},{count},{amount}::bigint,'building',{expiry},{role})",insert.Bind(),cancellationToken);
        var manifest=new ExportFileManifest([],data.Invoices.Select(invoice=>invoice.Id).ToList(),Hash(JsonSerializer.SerializeToUtf8Bytes(data,Json)),data.Kind,data.Locale);
        try
        {
            Directory.CreateDirectory(ExportDirectory);
            foreach(var format in request.Formats.Distinct(StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Both formats consume only this materialized object; no database read during rendering.
                var buffer=format=="xlsx" ? RenderXlsx(data) : RenderPdf(data);
                var filename=$"{exportId:D}.{format}";
                await using(var stream=new FileStream(Path.Combine(ExportDirectory,filename),FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true))
                    await stream.WriteAsync(buffer,cancellationToken);
                manifest.Files[format]=new(filename,buffer.LongLength,Hash(buffer),ContentType(format));
            }
            await SetState(exportId,"ready",manifest,cancellationToken);
            return new(exportId,data.RowCount,data.TotalVnd,data.ReadAt,manifest.Files.Keys.ToDictionary(format=>format,format=>$"/api/invoices/exports/{exportId:D}/{format}"));
        }
        catch
        {
            await SetState(exportId,"failed",manifest,CancellationToken.None);
            throw;
        }
    }

    private static void Validate(ExportRequest request,Actor actor)
    {
        Access.Require(actor,"owner","order_staff","customer");
        if(request.Kind is not ("detail" or "list" or "report") || request.Formats is null || request.Formats.Count is <1 or >2 || request.Formats.Any(format=>format is not ("xlsx" or "pdf")))throw new BusinessException("VALIDATION_ERROR","Chọn loại xuất và định dạng XLSX/PDF hợp lệ.");
        if(request.Locale is not ("vi" or "en"))throw new BusinessException("VALIDATION_ERROR","Ngôn ngữ không hợp lệ.");
        if(actor.Role=="customer" && request.Kind!="detail")throw new BusinessException("FORBIDDEN","Khách chỉ được xuất chi tiết hóa đơn của mình.",403);
        if(request.Kind=="detail" && (request.InvoiceId is null || request.InvoiceId==Guid.Empty))throw new BusinessException("VALIDATION_ERROR","Cần mã hóa đơn để xuất chi tiết.");
        if(request.Kind!="detail" && request.InvoiceId is not null)throw new BusinessException("VALIDATION_ERROR","Mã hóa đơn chỉ dùng cho xuất chi tiết.");
        InvoicePredicateBuilder.Validate(request.Query);
    }

    private async Task<InvoiceExportDataset> Materialize(ExportRequest request,Actor actor,CancellationToken cancellationToken)
    {
        var query=request.Query ?? new();
        if(request.Kind=="detail")
        {
            var detail=await queries.ReadDetail(request.InvoiceId!.Value,actor,cancellationToken);
            return new("detail",query,detail.ReadAt,1,detail.Invoice.GrandTotalVnd,[detail.Invoice],detail.Items,detail,null,request.Locale);
        }
        if(request.Kind=="report")
        {
            var report=await queries.ReadStatistics(new(){Query=query,GroupBy=request.GroupBy,Granularity=request.Granularity,Metric=request.Metric,DimensionId=request.DimensionId,ProductLevel=request.ProductLevel},actor,cancellationToken);
            if(report.InvoiceIds.Count>10000 || report.Rows.Count>100000)throw new BusinessException("EXPORT_LIMIT","Báo cáo vượt giới hạn xuất. Hãy thu hẹp điều kiện.");
            var predicate=InvoicePredicateBuilder.Build(query,actor,report:true,includeWindow:false,lifecycle:"issued");
            var ids=predicate.Parameters.Add(report.InvoiceIds.ToArray());
            List<InvoiceListItemDto> invoices=await queries.Db.Database.SqlQueryRaw<InvoiceListItemDto>($"WITH {InvoiceQueryService.Facts} SELECT {InvoiceQueryService.Projection} FROM invoice_facts i WHERE {predicate.Where} AND i.id=ANY({ids}) ORDER BY {predicate.OrderBy}",predicate.Parameters.Bind()).ToListAsync(cancellationToken);
            return new("report",query,report.ReadAt,invoices.Count,report.TotalVnd,invoices,await queries.ReadItems(report.InvoiceIds,cancellationToken),null,report,request.Locale);
        }
        var list=await queries.ReadList(query,actor,true,null,false,cancellationToken);
        return new("list",query,list.ReadAt,list.MatchedCount,list.MatchedTotalVnd,list.Items,await queries.ReadItems(list.Items.Select(invoice=>invoice.Id).ToList(),cancellationToken),null,null,request.Locale);
    }

    private async Task SetState(Guid id,string state,ExportFileManifest manifest,CancellationToken cancellationToken)
    {
        var parameters=new SqlParameters();var idParameter=parameters.Add(id);var status=parameters.Add(state);var json=parameters.Add(JsonSerializer.Serialize(manifest,Json));
        await queries.Db.Database.ExecuteSqlRawAsync($"UPDATE invoice_exports SET status={status},file_manifest={json}::jsonb WHERE id={idParameter}",parameters.Bind(),cancellationToken);
    }

    public async Task<ExportDownload> DownloadAsync(Guid id,string format,Actor actor,CancellationToken cancellationToken=default)
    {
        if(format is not ("xlsx" or "pdf"))throw new BusinessException("NOT_FOUND","Không có định dạng tệp này.",404);
        Access.Require(actor,"owner","order_staff","customer");
        var parameters=new SqlParameters();var idParameter=parameters.Add(id);
        var export=(await queries.Db.Database.SqlQueryRaw<ExportRow>($"SELECT requested_by_user_id AS \"RequestedBy\",role_snapshot AS \"Role\",status AS \"Status\",expires_at AS \"ExpiresAt\",query_json::text AS \"QueryJson\",file_manifest::text AS \"ManifestJson\" FROM invoice_exports WHERE id={idParameter}",parameters.Bind()).ToListAsync(cancellationToken)).SingleOrDefault();
        if(export is null || export.RequestedBy!=actor.Id)throw new BusinessException("NOT_FOUND","Không tìm thấy tệp xuất trong phạm vi của bạn.",404);
        if(export.Role!=actor.Role)throw new BusinessException("FORBIDDEN","Quyền truy cập tệp xuất đã thay đổi.",403);
        if(export.Status!="ready" || export.ExpiresAt<=DateTimeOffset.UtcNow)throw new BusinessException("EXPORT_EXPIRED","Tệp chưa sẵn sàng hoặc đã hết hạn. Hãy tạo lượt xuất mới.",410);
        var request=JsonSerializer.Deserialize<ExportRequest>(export.QueryJson,Json) ?? throw new BusinessException("EXPORT_CORRUPT","Metadata tệp không hợp lệ.",409);
        var manifest=JsonSerializer.Deserialize<ExportFileManifest>(export.ManifestJson,Json) ?? throw new BusinessException("EXPORT_CORRUPT","Manifest tệp không hợp lệ.",409);
        if(actor.Role!="customer")
        {
            var staffParameters=new SqlParameters();var user=staffParameters.Add(actor.Id);var role=staffParameters.Add(actor.Role);
            var permitted=(await queries.Db.Database.SqlQueryRaw<PermissionRow>($"SELECT EXISTS(SELECT 1 FROM staff_profiles WHERE user_id={user} AND role={role} AND active) AS \"Allowed\"",staffParameters.Bind()).ToListAsync(cancellationToken))[0].Allowed;
            if(!permitted)throw new BusinessException("FORBIDDEN","Tài khoản nhân viên không còn quyền đọc tệp.",403);
        }
        if(actor.Role=="customer")
        {
            if(request.Kind!="detail" || request.InvoiceId is null)throw new BusinessException("FORBIDDEN","Chỉ được tải hóa đơn của mình.",403);
            var ownedParameters=new SqlParameters();var invoice=ownedParameters.Add(request.InvoiceId.Value);var user=ownedParameters.Add(actor.Id);
            var owned=(await queries.Db.Database.SqlQueryRaw<PermissionRow>($"SELECT EXISTS(SELECT 1 FROM invoices WHERE id={invoice} AND customer_id={user} AND lifecycle='issued') AS \"Allowed\"",ownedParameters.Bind()).ToListAsync(cancellationToken))[0].Allowed;
            if(!owned)throw new BusinessException("FORBIDDEN","Không còn quyền đọc hóa đơn.",403);
        }
        if(actor.Role=="order_staff" && request.Kind=="report")
        {
            var scope=new SqlParameters();var ids=scope.Add(manifest.InvoiceIds.ToArray());var actorId=scope.Add(actor.Id);
            var allowed=(await queries.Db.Database.SqlQueryRaw<PermissionRow>($"SELECT NOT EXISTS(SELECT 1 FROM invoices WHERE id=ANY({ids}) AND responsible_staff_id IS DISTINCT FROM {actorId}) AS \"Allowed\"",scope.Bind()).ToListAsync(cancellationToken))[0].Allowed;
            if(!allowed)throw new BusinessException("FORBIDDEN","Tệp nằm ngoài phạm vi báo cáo hiện tại.",403);
        }
        if(!manifest.Files.TryGetValue(format,out var file) || file.Filename!=$"{id:D}.{format}")throw new BusinessException("NOT_FOUND","Tệp xuất không tồn tại.",404);
        var buffer=await File.ReadAllBytesAsync(Path.Combine(ExportDirectory,file.Filename),cancellationToken);
        if(buffer.LongLength!=file.Bytes || Hash(buffer)!=file.Checksum)throw new BusinessException("EXPORT_CORRUPT","Tệp không khớp checksum. Hãy tạo lượt xuất mới.",409);
        return new(buffer,$"BotZone-{id:D}.{format}",ContentType(format));
    }

    private sealed record Vnd(string Value);
    private static IXLWorksheet Sheet(XLWorkbook workbook,string name,string[] headers,double[] widths)
    {
        var sheet=workbook.AddWorksheet(name);
        for(var column=0;column<headers.Length;column++)
        {
            sheet.Cell(1,column+1).SetValue(headers[column]);sheet.Column(column+1).Width=column<widths.Length?widths[column]:22;
        }
        var heading=sheet.Range(1,1,1,headers.Length);heading.Style.Fill.BackgroundColor=XLColor.FromHtml("#18181b");heading.Style.Font.FontColor=XLColor.White;heading.Style.Font.Bold=true;
        sheet.Row(1).Height=32;sheet.Style.Alignment.WrapText=true;sheet.Style.Alignment.Vertical=XLAlignmentVerticalValues.Top;
        sheet.Style.Font.FontName="Noto Sans";sheet.Style.Font.FontSize=10;sheet.SheetView.FreezeRows(1);
        sheet.PageSetup.PageOrientation=XLPageOrientation.Landscape;sheet.PageSetup.PaperSize=XLPaperSize.A4Paper;sheet.PageSetup.FitToPages(1,0);
        return sheet;
    }
    private static void Row(IXLWorksheet sheet,params object?[] values)
    {
        var row=(sheet.LastRowUsed()?.RowNumber()??0)+1;
        for(var column=0;column<values.Length;column++)
        {
            var cell=sheet.Cell(row,column+1);var value=values[column];
            if(value is Vnd amount)
            {
                var integer=BigInteger.Parse(amount.Value,CultureInfo.InvariantCulture);
                if(BigInteger.Abs(integer)<=999999999999999L){cell.Value=(double)integer;cell.Style.NumberFormat.Format="#,##0";}
                else{cell.SetValue(amount.Value);cell.Style.NumberFormat.Format="@";cell.CreateComment().AddText("Giữ nguyên số nguyên dạng text: vượt độ chính xác 15 chữ số của Excel.");}
            }
            else if(value is int count)cell.Value=count;
            else if(value is long longCount && longCount is >=-999999999999999L and <=999999999999999L)cell.Value=longCount;
            else{cell.SetValue(value?.ToString()??"");cell.Style.NumberFormat.Format="@";}
        }
        sheet.Row(row).Height=30;
    }
    private static string Money(string value,bool en)=>BigInteger.Parse(value,CultureInfo.InvariantCulture).ToString("N0",en?En:Vi)+" VND";
    private static string Date(DateTimeOffset? value,bool en)=>value is null ? "—" : value.Value.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm",en?En:Vi);
    private static string Cell(JsonElement element,string property)=>element.TryGetProperty(property,out var value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) ? value.ValueKind==JsonValueKind.String ? value.GetString()! : value.GetRawText() : "";
    private static string JsonDate(JsonElement value,string property,bool en)=>Date(DateTimeOffset.TryParse(Cell(value,property),CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)?date:null,en);
    private static string Sum(string first,string second)=>InvoiceQueryService.SumMoney([first,second]);
    private static string VariantText(JsonElement variant)=>variant.ValueKind switch
    {
        JsonValueKind.Object=>string.Join(" · ",variant.EnumerateObject().Select(property=>property.Value.ToString())),
        JsonValueKind.Null or JsonValueKind.Undefined=>"",
        _=>variant.ToString()
    };

    internal static byte[] RenderXlsx(InvoiceExportDataset data)
    {
        using var workbook=new XLWorkbook();var en=data.Locale=="en";
        workbook.Properties.Author="BotZone local";workbook.Properties.Created=data.ReadAt.UtcDateTime;
        var invoices=Sheet(workbook,"HoaDon",en?["Invoice","Issued (Vietnam)","Customer","Staff","Items","Subtotal VND","Discount VND","Invoice total VND","Credit notes VND","Invoice net VND","Payment state","Invoice ID"]:["Mã hóa đơn","Phát hành (giờ Việt Nam)","Khách hàng","Nhân viên","Số món","Trước giảm VND","Giảm VND","Tổng hóa đơn VND","Chứng từ giảm VND","Ròng hóa đơn VND","Thanh toán","ID hóa đơn"],[26,25,28,25,12,22,22,24,24,24,22,40]);
        foreach(var item in data.Invoices)Row(invoices,item.InvoiceNo??"DRAFT",Date(item.IssuedAt,en),item.CustomerName,item.StaffName,item.ItemCount,new Vnd(item.SubtotalVnd),new Vnd(item.DiscountTotalVnd),new Vnd(item.GrandTotalVnd),new Vnd(item.CreditNoteTotalVnd),new Vnd(item.NetVnd),item.PaymentStatus,item.Id);
        invoices.Range(1,1,Math.Max(1,data.Invoices.Count+1),12).SetAutoFilter();
        Row(invoices,en?"INVOICE TOTAL":"TỔNG HÓA ĐƠN","","","",data.Invoices.Sum(invoice=>invoice.ItemCount),"","",new Vnd(InvoiceQueryService.SumMoney(data.Invoices.Select(invoice=>invoice.GrandTotalVnd))));
        var items=Sheet(workbook,"ChiTietHoaDon",en?["Invoice","Line","Type","SKU","Product / service","Configuration","Quantity","Unit price VND","Voucher VND","Points VND","Net VND","Serial / IMEI","Invoice ID","Order item ID"]:["Mã hóa đơn","Dòng","Loại","SKU","Sản phẩm / dịch vụ","Cấu hình","Số lượng","Đơn giá VND","Voucher VND","Điểm VND","Thành tiền VND","Serial / IMEI","ID hóa đơn","ID dòng đơn"],[26,10,16,28,36,36,12,22,22,22,22,40,40,40]);
        var numbers=data.Invoices.ToDictionary(invoice=>invoice.Id,invoice=>invoice.InvoiceNo??"DRAFT");
        foreach(var item in data.Items)Row(items,numbers[item.InvoiceId],item.LineNo,item.LineType,item.Sku,item.Name,item.Variant.GetRawText(),item.Quantity,new Vnd(item.UnitPriceVnd),new Vnd(item.VoucherDiscountVnd),new Vnd(item.PointsDiscountVnd),new Vnd(item.NetAmountVnd),string.Join("\n",item.Serials),item.InvoiceId,item.OrderItemId);
        var conditions=Sheet(workbook,"DieuKienLoc",en?["Property","Value"]:["Thuộc tính","Giá trị"],[30,110]);
        Row(conditions,"kind",data.Kind);Row(conditions,"readAt",data.ReadAt.ToString("O"));Row(conditions,"timezone","Asia/Ho_Chi_Minh");Row(conditions,"currency","VND");
        Row(conditions,"invoiceCount",data.RowCount);Row(conditions,"reportOrListTotalVnd",new Vnd(data.TotalVnd));Row(conditions,"query",JsonSerializer.Serialize(data.Query,Json));
        Row(conditions,"note",en?"Local demo document. Original invoices remain unchanged by adjustments/refunds.":"Chứng từ mô phỏng local. Chứng từ giảm/hoàn không sửa hóa đơn gốc.");
        Row(conditions,"excelPrecision",en?"Money beyond 15 digits is stored as text.":"Tiền vượt 15 chữ số được giữ dạng text để không mất chính xác.");
        if(data.Statistics is {} statistics)
        {
            Row(conditions,"report",$"{statistics.GroupBy}/{statistics.Granularity}/{statistics.Metric}");Row(conditions,"fromInclusive",statistics.FromInclusive);Row(conditions,"toExclusive",statistics.ToExclusive);
            var report=Sheet(workbook,"ThongKe",en?["Period","Object ID","Object","Invoices","Quantity","Value VND","Demo cash VND","Store credit VND"]:["Kỳ","ID đối tượng","Đối tượng","Số hóa đơn","Số lượng","Giá trị VND","Tiền mô phỏng VND","Số dư mua hàng VND"],[18,40,38,15,15,25,25,25]);
            foreach(var item in statistics.Rows)Row(report,item.Period,item.DimensionId,item.DimensionName,item.InvoiceCount,item.Quantity,new Vnd(item.TotalVnd),new Vnd(item.CashDemoVnd),new Vnd(item.StoreCreditVnd));
            Row(report,en?"TOTAL":"TỔNG","","","","",new Vnd(statistics.TotalVnd));
        }
        if(data.Detail is {} detail)
        {
            var header=Sheet(workbook,"ThongTinChungTu",en?["Property","Value"]:["Thuộc tính","Giá trị"],[30,110]);
            Row(header,"invoiceNo",detail.Invoice.InvoiceNo);Row(header,"orderNo",detail.Invoice.OrderNo);Row(header,"customer",detail.Invoice.CustomerSnapshot.GetRawText());Row(header,"staff",detail.Invoice.StaffSnapshot.GetRawText());Row(header,"address",detail.Invoice.AddressSnapshot.GetRawText());Row(header,"issuedBy",detail.Invoice.IssuedByUserId);Row(header,"exchangeRateVnd",detail.Invoice.ExchangeRateVnd);Row(header,"promotions",JsonSerializer.Serialize(detail.Promotions,Json));
            var settlements=Sheet(workbook,"ThuVaHoan",en?["Type","ID","Source / status","Amount VND","Date","Allocations"]:["Loại","ID","Nguồn / trạng thái","Số tiền VND","Thời điểm","Phân bổ"],[20,40,25,25,25,110]);
            foreach(var item in detail.Receipts)Row(settlements,"receipt",Cell(item,"id"),Cell(item,"tender_kind"),new Vnd(Cell(item,"amount_vnd")),JsonDate(item,"received_at",en),Cell(item,"allocations"));
            foreach(var item in detail.Refunds)Row(settlements,"refund",Cell(item,"id"),Cell(item,"status"),new Vnd(Cell(item,"amount_vnd")),JsonDate(item,"completed_at",en),Cell(item,"allocations"));
            var notes=Sheet(workbook,"ChungTuGiam",en?["Code","Status","Date","Reason","Amount VND","Items"]:["Mã","Trạng thái","Thời điểm","Lý do","Số tiền VND","Dòng"],[26,20,25,60,25,110]);
            foreach(var note in detail.CreditNotes)Row(notes,Cell(note,"credit_note_no"),Cell(note,"status"),JsonDate(note,"issued_at",en),Cell(note,"reason"),new Vnd(Cell(note,"total_vnd")),Cell(note,"items"));
            var units=Sheet(workbook,"PhanBoDonVi",en?["Unit","Order item","Ordinal","Gross VND","Voucher VND","Points","Points VND","Net VND","Planned credit VND","Planned cash VND"]:["Unit","Dòng đơn","Ordinal","Gốc VND","Voucher VND","Điểm","Điểm VND","Net VND","Credit dự kiến VND","Tiền dự kiến VND"],[40,40,12,25,25,12,25,25,25,25]);
            foreach(var unit in detail.Units)Row(units,Cell(unit,"id"),Cell(unit,"order_item_id"),int.Parse(Cell(unit,"unit_ordinal"),CultureInfo.InvariantCulture),new Vnd(Cell(unit,"gross_vnd")),new Vnd(Cell(unit,"voucher_discount_vnd")),int.Parse(Cell(unit,"redeemed_points"),CultureInfo.InvariantCulture),new Vnd(Cell(unit,"points_discount_vnd")),new Vnd(Cell(unit,"net_amount_vnd")),new Vnd(Cell(unit,"credit_planned_vnd")),new Vnd(Cell(unit,"cash_planned_vnd")));
        }
        using var output=new MemoryStream();workbook.SaveAs(output);return output.ToArray();
    }

    private static void Paragraph(Section section,string text,double size=9,bool bold=false,bool keepWithNext=false)
    {
        var paragraph=section.AddParagraph(text);paragraph.Format.Font.Size=size;paragraph.Format.Font.Bold=bold;paragraph.Format.SpaceAfter=Unit.FromPoint(5);paragraph.Format.KeepWithNext=keepWithNext;
    }
    private static void Table(Section section,string title,string[] headers,double[] fractions,IEnumerable<string[]> rows,double totalWidth,string[]? summary=null)
    {
        Paragraph(section,title,11,true,true);
        var table=section.AddTable();table.Borders.Color=Colors.LightGray;table.Borders.Width=0.4;table.Format.Font.Size=8;
        foreach(var width in fractions)table.AddColumn(Unit.FromCentimeter(totalWidth*width));
        var head=table.AddRow();head.HeadingFormat=true;head.Shading.Color=Color.Parse("#ededed");head.Format.Font.Bold=true;head.TopPadding=Unit.FromPoint(6);head.BottomPadding=Unit.FromPoint(6);
        for(var index=0;index<headers.Length;index++)head.Cells[index].AddParagraph(headers[index]);
        foreach(var values in rows)
        {
            // MigraDoc keeps table rows on one page. Split unusually long cells into
            // continuation rows so user text never extends below the page boundary.
            var chunks=values.Select(value=>ChunkText(value,240)).ToArray();
            var continuationCount=Math.Max(1,chunks.Select(parts=>parts.Count).DefaultIfEmpty(1).Max());
            for(var part=0;part<continuationCount;part++)
            {
                var row=table.AddRow();row.TopPadding=Unit.FromPoint(5);row.BottomPadding=Unit.FromPoint(5);
                for(var index=0;index<headers.Length;index++)row.Cells[index].AddParagraph(index<chunks.Length && part<chunks[index].Count ? chunks[index][part] : "");
            }
        }
        if(summary is not null)
        {
            // Keep the final data row and its total on the same page. A paragraph
            // after the table can otherwise become a page containing only a total.
            if(table.Rows.Count>1)table.Rows[table.Rows.Count-1].KeepWith=1;
            var total=table.AddRow();total.Format.Font.Bold=true;total.Shading.Color=Color.Parse("#f3f3f3");total.TopPadding=Unit.FromPoint(7);total.BottomPadding=Unit.FromPoint(7);
            for(var index=0;index<headers.Length;index++)total.Cells[index].AddParagraph(index<summary.Length?summary[index]:"");
        }
        section.AddParagraph().Format.SpaceAfter=Unit.FromPoint(5);
    }

    private static string FilterDescription(InvoiceExportDataset data,bool en)
    {
        string Field(string field)=>field switch
        {
            "invoiceNo"=>en?"Invoice number":"Mã hóa đơn","issuedAt"=>en?"Issue date":"Ngày phát hành",
            "grandTotalVnd"=>en?"Invoice total":"Tổng hóa đơn","discountTotalVnd"=>en?"Discount":"Tổng giảm",
            "responsibleStaffId"=>en?"Responsible staff":"Nhân viên phụ trách","customerId"=>en?"Customer ID":"Mã khách hàng",
            "lifecycle"=>en?"Document status":"Trạng thái chứng từ","paymentStatus"=>en?"Payment status":"Trạng thái thanh toán",_=>field
        };
        string Value(InvoiceRuleFilter rule)
        {
            var value=rule.Value.ValueKind==JsonValueKind.String?rule.Value.GetString()!:rule.Value.ToString();
            if(rule.Field is "grandTotalVnd" or "discountTotalVnd")return Money(value,en);
            if(rule.Field=="responsibleStaffId")return data.Invoices.FirstOrDefault(i=>i.ResponsibleStaffId==value)?.StaffName??value;
            if(!en)return value switch{"issued"=>"Đã phát hành","draft"=>"Bản nháp","paid"=>"Đã thu đủ","unpaid"=>"Chưa thu","partially_paid"=>"Thu một phần","refunded"=>"Đã hoàn","partially_refunded"=>"Hoàn một phần",_=>value};
            return value;
        }
        string Node(FilterNode node)=>node switch
        {
            InvoiceRuleFilter rule=>$"{Field(rule.Field)} {rule.Op} {Value(rule)}",
            InvoiceGroupFilter group=>"("+string.Join(group.Logic=="AND"?(en?" AND ":" VÀ "):(en?" OR ":" HOẶC "),group.Children.Select(Node))+")",
            InvoiceNotFilter not=>(en?"NOT ":"KHÔNG ")+"("+Node(not.Child)+")",
            InvoiceNullFilter empty=>$"{Field(empty.Field)} {(empty.IsNull?(en?"is empty":"để trống"):(en?"is not empty":"không trống"))}",
            InvoiceProductFilter product=>(product.Mode=="contains"?(en?"Contains product: ":"Có sản phẩm: "):(en?"Excludes product: ":"Không có sản phẩm: "))+(data.Items.FirstOrDefault(i=>i.ProductId==product.ProductId)?.Name??product.ProductId.ToString()),
            _=>throw new InvalidOperationException("Unsupported validated filter.")
        };
        var parts=new List<string>();var query=data.Query;
        if(query.FromInclusive is not null)parts.Add((en?"From (inclusive): ":"Từ (bao gồm): ")+query.FromInclusive);
        if(query.ToExclusive is not null)parts.Add((en?"To (exclusive): ":"Đến (không bao gồm): ")+query.ToExclusive);
        if(!string.IsNullOrWhiteSpace(query.QuickSearch))parts.Add((en?"Search: ":"Tìm kiếm: ")+query.QuickSearch);
        if(query.Filter is not null)parts.Add(Node(query.Filter));
        if(query.Lifecycle is not null)parts.Add((en?"Document status: ":"Trạng thái chứng từ: ")+(query.Lifecycle=="draft"?(en?"Draft":"Bản nháp"):(en?"Issued":"Đã phát hành")));
        if(parts.Count==0)parts.Add(en?"All documents in your permitted scope":"Tất cả chứng từ trong phạm vi được phép");
        parts.Add((en?"Sort: ":"Sắp xếp: ")+string.Join(", ",query.Sort.Select(s=>Field(s.Field)+" "+(s.Direction=="asc"?(en?"ascending":"tăng dần"):(en?"descending":"giảm dần")))));
        return string.Join("; ",parts);
    }

    private static List<string> ChunkText(string text,int limit)
    {
        var chunks=new List<string>();
        while(text.Length>limit)
        {
            var boundary=text.LastIndexOfAny([' ','\n'],limit-1,limit);
            if(boundary<limit/2)boundary=limit;
            chunks.Add(text[..boundary]);text=text[boundary..];
        }
        chunks.Add(text);return chunks;
    }

    internal byte[] RenderPdf(InvoiceExportDataset data)
    {
        BotZoneFontResolver.Ensure(FontPath);var en=data.Locale=="en";var doc=new Document();
        doc.Info.Title=en?"BotZone sales document":"Chứng từ bán hàng BotZone";doc.Info.Author="BotZone local";
        doc.Styles[StyleNames.Normal]!.Font.Name="BotZone Noto";doc.Styles[StyleNames.Normal]!.Font.Size=9;
        var section=doc.AddSection();section.PageSetup.PageFormat=PageFormat.A4;section.PageSetup.Orientation=data.Kind=="detail"?Orientation.Portrait:Orientation.Landscape;
        section.PageSetup.TopMargin=Unit.FromCentimeter(1.4);section.PageSetup.BottomMargin=Unit.FromCentimeter(1.5);section.PageSetup.LeftMargin=Unit.FromCentimeter(1.3);section.PageSetup.RightMargin=Unit.FromCentimeter(1.3);
        var width=data.Kind=="detail"?18.4:27.1;
        var footer=section.Footers.Primary.AddParagraph("BotZone · "+(en?"Page ":"Trang "));footer.AddPageField();footer.AddText("/");footer.AddNumPagesField();footer.Format.Alignment=ParagraphAlignment.Right;footer.Format.Font.Size=8;
        Paragraph(section,"BotZone",23,true,true);
        Paragraph(section,data.Kind=="detail"?$"{(en?"SALES DOCUMENT":"PHIẾU BÁN HÀNG")} · {data.Detail!.Invoice.InvoiceNo??"DRAFT"}":data.Kind=="report"?(en?"INVOICE STATISTICS":"THỐNG KÊ HÓA ĐƠN"):(en?"INVOICE LIST":"DANH SÁCH HÓA ĐƠN"),14,true,true);
        Paragraph(section,en?"Local demonstration document — not a statutory electronic invoice.":"Chứng từ mô phỏng local — không phải hóa đơn điện tử hợp pháp.");
        Paragraph(section,$"{(en?"Data read":"Dữ liệu đọc")}: {Date(data.ReadAt,en)} (Asia/Ho_Chi_Minh)");
        Paragraph(section,$"{(en?"Invoices":"Hóa đơn")}: {data.RowCount} · {(en?"Value":"Giá trị")}: {Money(data.TotalVnd,en)}");
        if(data.Kind!="detail")
        {
            Paragraph(section,(en?"Filters: ":"Điều kiện lọc: ")+FilterDescription(data,en),8);
            Paragraph(section,en?"All matching results are included, independent of the current UI page.":"Bao gồm toàn bộ kết quả phù hợp, không phụ thuộc trang đang xem.",8);
        }
        if(data.Detail is {} detail)
        {
            Paragraph(section,$"{(en?"Order":"Đơn hàng")}: {detail.Invoice.OrderNo}");Paragraph(section,$"{(en?"Customer":"Khách hàng")}: {detail.Invoice.CustomerName}");
            Paragraph(section,$"{(en?"Address":"Địa chỉ")}: {string.Join(", ",detail.Invoice.AddressSnapshot.EnumerateObject().Select(property=>property.Value.ToString()))}");
            Paragraph(section,$"{(en?"Responsible staff":"Nhân viên phụ trách")}: {detail.Invoice.StaffName} · {(en?"Issued":"Phát hành")}: {Date(detail.Invoice.IssuedAt,en)}");
            Paragraph(section,$"{(en?"Promotions":"Khuyến mãi")}: {(detail.Promotions.Count==0?"—":string.Join("; ",detail.Promotions.Select(p=>$"{Cell(p,"code_normalized")} v{Cell(p,"version_no")}: {Money(Cell(p,"amount_vnd"),en)}")))}");
            Table(section,en?"Purchased items":"Chi tiết hàng mua",en?["Product / SKU / serial","Qty","Unit VND","Discount VND","Net VND"]:["Sản phẩm / SKU / serial","SL","Đơn giá VND","Giảm VND","Thành tiền VND"],[.39,.07,.18,.17,.19],detail.Items.Select(item=>new[]{item.Name+"\n"+item.Sku+"\n"+VariantText(item.Variant)+(item.Serials.Count>0?"\n"+string.Join("\n",item.Serials):""),item.Quantity.ToString(CultureInfo.InvariantCulture),Money(item.UnitPriceVnd,en),Money(Sum(item.VoucherDiscountVnd,item.PointsDiscountVnd),en),Money(item.NetAmountVnd,en)}),width,[en?"ORIGINAL INVOICE TOTAL":"TỔNG HÓA ĐƠN GỐC","","","",Money(detail.Invoice.GrandTotalVnd,en)]);
            Paragraph(section,$"{(en?"Subtotal / discount":"Trước giảm / giảm")}: {Money(detail.Invoice.SubtotalVnd,en)} / {Money(detail.Invoice.DiscountTotalVnd,en)}");
            Paragraph(section,$"{(en?"Issued adjustments / current net":"Điều chỉnh đã phát hành / ròng hiện tại")}: {Money(detail.Invoice.CreditNoteTotalVnd,en)} / {Money(detail.Invoice.NetVnd,en)}");
            Paragraph(section,$"{(en?"Received / refunded at read time":"Đã thu / đã hoàn tại thời điểm đọc")}: {Money(detail.Invoice.ReceivedVnd,en)} / {Money(detail.Invoice.RefundedVnd,en)}");
            Table(section,en?"Settlement at read time":"Thu và hoàn tại thời điểm đọc",en?["Type / source","Date / status","VND"]:["Loại / nguồn","Thời điểm / trạng thái","VND"],[.3,.4,.3],detail.Receipts.Select(r=>new[]{(en?"Receipt":"Khoản thu")+" · "+Cell(r,"tender_kind"),JsonDate(r,"received_at",en),Money(Cell(r,"amount_vnd"),en)}).Concat(detail.Refunds.Select(r=>new[]{(en?"Refund":"Khoản hoàn"),Cell(r,"status")+" · "+JsonDate(r,"completed_at",en),Money(Cell(r,"amount_vnd"),en)})),width);
            if(detail.CreditNotes.Count>0)Table(section,en?"Adjustments":"Điều chỉnh",en?["Credit note","Reason / date","VND"]:["Chứng từ giảm","Lý do / ngày","VND"],[.25,.45,.3],detail.CreditNotes.Select(note=>new[]{Cell(note,"credit_note_no"),Cell(note,"reason")+"\n"+JsonDate(note,"issued_at",en),Money(Cell(note,"total_vnd"),en)}),width);
        }
        else if(data.Statistics is {} stats)
        {
            Paragraph(section,$"{stats.GroupBy} · {stats.Granularity} · {stats.Metric}");Paragraph(section,$"{stats.FromInclusive} ≤ {(en?"event time":"thời điểm phát sinh")} < {stats.ToExclusive}");
            Table(section,en?"Statistics":"Bảng thống kê",en?["Period","Object","Invoices","VND","Demo cash","Store credit"]:["Kỳ","Đối tượng","Hóa đơn","VND","Tiền mô phỏng","Số dư mua hàng"],[.12,.25,.09,.18,.18,.18],stats.Rows.Select(row=>new[]{row.Period,row.DimensionName,row.InvoiceCount.ToString(CultureInfo.InvariantCulture),Money(row.TotalVnd,en),Money(row.CashDemoVnd,en),Money(row.StoreCreditVnd,en)}),width,[en?"TOTAL":"TỔNG","","",Money(stats.TotalVnd,en),Money(SumMoney(stats.Rows.Select(r=>r.CashDemoVnd)),en),Money(SumMoney(stats.Rows.Select(r=>r.StoreCreditVnd)),en)]);
        }
        else Table(section,en?"Matching invoices":"Hóa đơn phù hợp",en?["Invoice / issued","Customer","Staff","Total VND","Adjustment VND","Net VND"]:["Mã / ngày phát hành","Khách hàng","Nhân viên","Tổng VND","Giảm VND","Ròng VND"],[.2,.2,.16,.16,.14,.14],data.Invoices.Select(invoice=>new[]{(invoice.InvoiceNo??"DRAFT")+"\n"+Date(invoice.IssuedAt,en),invoice.CustomerName,invoice.StaffName,Money(invoice.GrandTotalVnd,en),Money(invoice.CreditNoteTotalVnd,en),Money(invoice.NetVnd,en)}),width,[en?"TOTAL":"TỔNG","","",Money(data.TotalVnd,en),Money(SumMoney(data.Invoices.Select(i=>i.CreditNoteTotalVnd)),en),Money(SumMoney(data.Invoices.Select(i=>i.NetVnd)),en)]);
        var renderer=new PdfDocumentRenderer{Document=doc};renderer.RenderDocument();using var output=new MemoryStream();renderer.PdfDocument.Save(output,false);return output.ToArray();
    }

    private sealed class ExportRow{public string RequestedBy{get;set;}="";public string Role{get;set;}="";public string Status{get;set;}="";public DateTimeOffset ExpiresAt{get;set;}public string QueryJson{get;set;}="{}";public string ManifestJson{get;set;}="{}";}
    private sealed class PermissionRow{public bool Allowed{get;set;}}
}
#pragma warning restore EF1002

internal sealed class BotZoneFontResolver(byte[] font):IFontResolver
{
    private static readonly object Gate=new();
    public static void Ensure(string fontPath)
    {
        lock(Gate)
        {
            if(GlobalFontSettings.FontResolver is BotZoneFontResolver)return;
            if(GlobalFontSettings.FontResolver is not null)throw new InvalidOperationException("BotZone PDF requires the configured local Noto Sans font resolver.");
            GlobalFontSettings.FontResolver=new BotZoneFontResolver(File.ReadAllBytes(fontPath));
        }
    }
    public FontResolverInfo? ResolveTypeface(string familyName,bool bold,bool italic)=>new("BotZoneNotoRegular",false,italic);
    public byte[]? GetFont(string faceName)=>faceName=="BotZoneNotoRegular"?font:null;
}
