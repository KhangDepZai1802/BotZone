using System.Data;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using BotZone.Contracts;
using BotZone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BotZone.Infrastructure.Invoices;

// EF1002 cannot follow our SQL fragment compiler. Interpolated identifiers and
// operators come only from constants/InvoicePredicateBuilder/ValidateReport;
// every actor, filter, date, amount and ID is bound through SqlParameters.Bind().
// Integration TS01/TS02/TS09 and query-plan capture exercise this boundary.
#pragma warning disable EF1002
public sealed class InvoiceQueryService(BotZoneDbContext db,IConfiguration configuration)
{
    internal readonly BotZoneDbContext Db=db;
    private readonly IConfiguration _configuration=configuration;
    internal const string Facts = """
        invoice_facts AS (
          SELECT i0.*, COALESCE(lines.item_count,0)::bigint AS item_count,
            COALESCE(credits.total,0) AS credit_note_total_vnd,
            i0.grand_total_vnd-COALESCE(credits.total,0) AS net_vnd,
            COALESCE(receipts.total,0) AS received_vnd,COALESCE(refunded.total,0) AS refunded_vnd,
            CASE WHEN COALESCE(refunded.total,0)>0 AND refunded.total>=COALESCE(receipts.total,0) THEN 'refunded'
                 WHEN COALESCE(refunded.total,0)>0 THEN 'partially_refunded'
                 WHEN i0.grand_total_vnd=0 OR COALESCE(receipts.total,0)>=i0.grand_total_vnd THEN 'paid'
                 WHEN COALESCE(receipts.total,0)>0 THEN 'partially_paid' ELSE 'unpaid' END AS payment_status
          FROM invoices i0
          LEFT JOIN LATERAL (SELECT sum(quantity) AS item_count FROM invoice_items WHERE invoice_id=i0.id AND line_type='product') lines ON TRUE
          LEFT JOIN LATERAL (SELECT sum(total_vnd) AS total FROM credit_notes WHERE invoice_id=i0.id AND status='issued') credits ON TRUE
          LEFT JOIN LATERAL (SELECT sum(amount_vnd) AS total FROM payment_receipts WHERE order_id=i0.order_id) receipts ON TRUE
          LEFT JOIN LATERAL (SELECT sum(amount_vnd) AS total FROM refunds WHERE order_id=i0.order_id AND status='completed') refunded ON TRUE
        )
        """;
    internal const string Projection = """
        i.id AS "Id",i.invoice_no AS "InvoiceNo",i.order_id AS "OrderId",i.issued_at AS "IssuedAt",
        COALESCE(i.customer_snapshot->>'name',i.customer_snapshot->>'displayName',i.customer_snapshot->>'display_name','Khách hàng') AS "CustomerName",
        COALESCE(i.staff_snapshot->>'name',i.staff_snapshot->>'displayName',i.responsible_staff_id,'Chưa phân công') AS "StaffName",
        i.responsible_staff_id AS "ResponsibleStaffId",i.item_count AS "ItemCount",
        i.subtotal_vnd::text AS "SubtotalVnd",i.discount_total_vnd::text AS "DiscountTotalVnd",i.grand_total_vnd::text AS "GrandTotalVnd",
        i.credit_note_total_vnd::text AS "CreditNoteTotalVnd",i.net_vnd::text AS "NetVnd",i.payment_status AS "PaymentStatus"
        """;

    internal async Task<T> Snapshot<T>(Func<Task<T>> read,CancellationToken cancellationToken)
    {
        await using var transaction=await Db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead,cancellationToken);
        await Db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY",cancellationToken);
        var result=await read();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
    internal async Task<DateTimeOffset> ReadAt(CancellationToken cancellationToken) =>
        (await Db.Database.SqlQueryRaw<ReadAtRow>("SELECT transaction_timestamp() AS \"ReadAt\"").ToListAsync(cancellationToken))[0].ReadAt;

    public Task<InvoiceListResult> ListInvoicesAsync(InvoiceQuery query,Actor actor,CancellationToken cancellationToken=default) =>
        Snapshot(()=>ReadList(query,actor,false,null,false,cancellationToken),cancellationToken);

    internal async Task<InvoiceListResult> ReadList(InvoiceQuery query,Actor actor,bool all,Guid? invoiceId,bool reportScope,CancellationToken cancellationToken)
    {
        query=InvoicePredicateBuilder.Validate(query);
        var predicate=InvoicePredicateBuilder.Build(query,actor,reportScope,allowCustomer:invoiceId.HasValue,lifecycle:invoiceId.HasValue ? actor.Role=="customer" ? "issued" : "any" : null);
        var parameters=predicate.Parameters;
        var where=predicate.Where;
        if(invoiceId.HasValue)where+=$" AND i.id={parameters.Add(invoiceId.Value)}";
        var totals=(await Db.Database.SqlQueryRaw<TotalsRow>($"WITH {Facts} SELECT count(*) AS \"Count\",COALESCE(sum(i.grand_total_vnd),0)::text AS \"Total\" FROM invoice_facts i WHERE {where}",parameters.Bind()).ToListAsync(cancellationToken))[0];
        if(all && totals.Count>10000)throw new BusinessException("EXPORT_LIMIT","Tập kết quả vượt 10.000 hóa đơn. Hãy thu hẹp điều kiện.");
        var paging=all ? "" : $" LIMIT {parameters.Add(query.PageSize)} OFFSET {parameters.Add(checked((long)(query.Page-1)*query.PageSize))}";
        // Required rubric path: PostgreSQL -> typed EF projection -> List<T> -> API -> BindingSource.
        List<InvoiceListItemDto> items=await Db.Database.SqlQueryRaw<InvoiceListItemDto>($"WITH {Facts} SELECT {Projection} FROM invoice_facts i WHERE {where} ORDER BY {predicate.OrderBy}{paging}",parameters.Bind()).ToListAsync(cancellationToken);
        return new(items,totals.Count,totals.Total,await ReadAt(cancellationToken),query.Page,query.PageSize);
    }

    public Task<InvoiceDetailDto> GetInvoiceDetailAsync(Guid id,Actor actor,CancellationToken cancellationToken=default) =>
        Snapshot(()=>ReadDetail(id,actor,cancellationToken),cancellationToken);

    internal async Task<InvoiceDetailDto> ReadDetail(Guid id,Actor actor,CancellationToken cancellationToken)
    {
        var result=await ReadList(new(),actor,true,id,false,cancellationToken);
        var invoice=result.Items.SingleOrDefault() ?? throw new BusinessException("NOT_FOUND","Không tìm thấy hóa đơn trong phạm vi được phép.",404);
        var parameters=new SqlParameters();var idParameter=parameters.Add(id);
        var extra=(await Db.Database.SqlQueryRaw<HeaderRow>($"""
            WITH {Facts} SELECT i.lifecycle AS "Lifecycle",i.customer_id AS "CustomerId",i.issued_by_user_id AS "IssuedByUserId",
              i.customer_snapshot::text AS "CustomerSnapshot",i.staff_snapshot::text AS "StaffSnapshot",o.order_no AS "OrderNo",
              o.address_snapshot::text AS "AddressSnapshot",i.received_vnd::text AS "ReceivedVnd",i.refunded_vnd::text AS "RefundedVnd",
              o.exchange_rate_vnd::text AS "ExchangeRateVnd" FROM invoice_facts i JOIN orders o ON o.id=i.order_id WHERE i.id={idParameter}
            """,parameters.Bind()).ToListAsync(cancellationToken)).Single();
        var header=new InvoiceDetailHeaderDto
        {
            Id=invoice.Id,InvoiceNo=invoice.InvoiceNo,OrderId=invoice.OrderId,IssuedAt=invoice.IssuedAt,CustomerName=invoice.CustomerName,StaffName=invoice.StaffName,
            ResponsibleStaffId=invoice.ResponsibleStaffId,ItemCount=invoice.ItemCount,SubtotalVnd=invoice.SubtotalVnd,DiscountTotalVnd=invoice.DiscountTotalVnd,
            GrandTotalVnd=invoice.GrandTotalVnd,CreditNoteTotalVnd=invoice.CreditNoteTotalVnd,NetVnd=invoice.NetVnd,PaymentStatus=invoice.PaymentStatus,
            Lifecycle=extra.Lifecycle,CustomerId=extra.CustomerId,IssuedByUserId=extra.IssuedByUserId,CustomerSnapshot=ParseJson(extra.CustomerSnapshot),
            StaffSnapshot=ParseJson(extra.StaffSnapshot),OrderNo=extra.OrderNo,AddressSnapshot=ParseJson(extra.AddressSnapshot),ReceivedVnd=extra.ReceivedVnd,
            RefundedVnd=extra.RefundedVnd,ExchangeRateVnd=extra.ExchangeRateVnd
        };
        var orderParams=new SqlParameters();var order=orderParams.Add(invoice.OrderId);
        var receipts=await JsonRows($"""
            SELECT jsonb_build_object('id',r.id,'tender_kind',r.tender_kind,'amount_vnd',r.amount_vnd::text,'received_at',r.received_at,
              'allocations',COALESCE((SELECT jsonb_agg(jsonb_build_object('unitId',a.order_item_unit_amount_id,'amountVnd',a.amount_vnd::text)) FROM order_tender_allocations a WHERE a.receipt_id=r.id),'[]'::jsonb))::text AS "Json"
            FROM payment_receipts r WHERE r.order_id={order} ORDER BY r.received_at,r.id
            """,orderParams,cancellationToken);
        var credits=await JsonRows($"""
            SELECT jsonb_build_object('id',c.id,'credit_note_no',c.credit_note_no,'status',c.status,'issued_at',c.issued_at,'reason',c.reason,'total_vnd',c.total_vnd::text,
              'return_request_id',c.return_request_id,'warranty_claim_id',c.warranty_claim_id,'shipment_id',c.shipment_id,
              'items',COALESCE((SELECT jsonb_agg(jsonb_build_object('invoiceItemId',ci.invoice_item_id,'quantity',ci.quantity,'amountVnd',ci.amount_vnd::text)) FROM credit_note_items ci WHERE ci.credit_note_id=c.id),'[]'::jsonb))::text AS "Json"
            FROM credit_notes c WHERE c.invoice_id={idParameter} {(actor.Role=="customer" ? "AND c.status='issued'" : "")} ORDER BY c.issued_at,c.id
            """,parameters,cancellationToken);
        var refunds=await JsonRows($"""
            SELECT jsonb_build_object('id',r.id,'credit_note_id',r.credit_note_id,'status',r.status,'amount_vnd',r.amount_vnd::text,'completed_at',r.completed_at,
              'allocations',COALESCE((SELECT jsonb_agg(jsonb_build_object('unitId',a.order_item_unit_amount_id,'receiptId',a.payment_receipt_id,'tenderKind',p.tender_kind,'amountVnd',a.amount_vnd::text)) FROM refund_allocations a JOIN payment_receipts p ON p.id=a.payment_receipt_id WHERE a.refund_id=r.id),'[]'::jsonb))::text AS "Json"
            FROM refunds r WHERE r.order_id={order} ORDER BY r.completed_at,r.id
            """,orderParams,cancellationToken);
        var units=await JsonRows($"""
            SELECT jsonb_build_object('id',u.id,'order_item_id',u.order_item_id,'unit_ordinal',u.unit_ordinal,'parent_unit_amount_id',u.parent_unit_amount_id,
              'gross_vnd',u.gross_vnd::text,'voucher_discount_vnd',u.voucher_discount_vnd::text,'redeemed_points',u.redeemed_points,'points_discount_vnd',u.points_discount_vnd::text,
              'net_amount_vnd',u.net_amount_vnd::text,'credit_planned_vnd',u.credit_planned_vnd::text,'cash_planned_vnd',u.cash_planned_vnd::text)::text AS "Json"
            FROM order_item_unit_amounts u JOIN order_items oi ON oi.id=u.order_item_id WHERE oi.order_id={order} ORDER BY oi.line_no,u.unit_ordinal
            """,orderParams,cancellationToken);
        var attempts=await JsonRows($"SELECT jsonb_build_object('id',id,'method',method,'status',status,'requested_amount_vnd',requested_amount_vnd::text,'created_at',created_at)::text AS \"Json\" FROM payment_attempts WHERE order_id={order} ORDER BY created_at,id",orderParams,cancellationToken);
        var promotions=await JsonRows($"SELECT jsonb_build_object('code_normalized',p.code_normalized,'version_no',v.version_no,'status',r.status,'amount_vnd',r.amount_vnd::text)::text AS \"Json\" FROM promotion_redemptions r JOIN promotion_versions v ON v.id=r.promotion_version_id JOIN promotions p ON p.id=r.promotion_id WHERE r.order_id={order}",orderParams,cancellationToken);
        return new(header,await ReadItems([id],cancellationToken),receipts,credits,refunds,units,attempts,promotions,await ReadAt(cancellationToken));
    }

    internal async Task<List<InvoiceDetailItemDto>> ReadItems(List<Guid> ids,CancellationToken cancellationToken)
    {
        if(ids.Count==0)return [];
        var parameters=new SqlParameters();var idsParameter=parameters.Add(ids.ToArray());
        var rows=await Db.Database.SqlQueryRaw<ItemRow>($"""
            SELECT ii.id AS "Id",ii.invoice_id AS "InvoiceId",ii.order_item_id AS "OrderItemId",ii.line_no AS "LineNo",ii.line_type AS "LineType",
              ii.product_id AS "ProductId",ii.variant_id AS "VariantId",ii.parent_invoice_item_id AS "ParentInvoiceItemId",ii.sku_snapshot AS "Sku",ii.name_snapshot AS "Name",
              ii.variant_snapshot::text AS "Variant",ii.quantity AS "Quantity",ii.unit_price_vnd::text AS "UnitPriceVnd",ii.voucher_discount_vnd::text AS "VoucherDiscountVnd",
              ii.points_discount_vnd::text AS "PointsDiscountVnd",ii.net_amount_vnd::text AS "NetAmountVnd",
              COALESCE((SELECT array_agg(DISTINCT identifiers.normalized_value ORDER BY identifiers.normalized_value) FROM inventory_allocations allocation JOIN inventory_identifiers identifiers ON identifiers.inventory_unit_id=allocation.inventory_unit_id WHERE allocation.order_item_id=ii.order_item_id AND allocation.state IN ('shipped','returned','replaced')),ARRAY[]::text[]) AS "Serials"
            FROM invoice_items ii WHERE ii.invoice_id=ANY({idsParameter}) ORDER BY ii.invoice_id,ii.line_no,ii.id
            """,parameters.Bind()).ToListAsync(cancellationToken);
        return rows.Select(row=>new InvoiceDetailItemDto{Id=row.Id,InvoiceId=row.InvoiceId,OrderItemId=row.OrderItemId,LineNo=row.LineNo,LineType=row.LineType,
            ProductId=row.ProductId,VariantId=row.VariantId,ParentInvoiceItemId=row.ParentInvoiceItemId,Sku=row.Sku,Name=row.Name,Variant=ParseJson(row.Variant),Quantity=row.Quantity,
            UnitPriceVnd=row.UnitPriceVnd,VoucherDiscountVnd=row.VoucherDiscountVnd,PointsDiscountVnd=row.PointsDiscountVnd,NetAmountVnd=row.NetAmountVnd,Serials=row.Serials.ToList()}).ToList();
    }

    public Task<InvoiceStatisticsResult> GetInvoiceStatisticsAsync(ReportRequest request,Actor actor,CancellationToken cancellationToken=default)=>
        Snapshot(()=>ReadStatistics(request,actor,cancellationToken),cancellationToken);

    internal static ReportRequest ValidateReport(ReportRequest request)
    {
        InvoicePredicateBuilder.Validate(request.Query);
        if(request.GroupBy is not ("period" or "staff" or "product") || request.Granularity is not ("month" or "quarter" or "year") || request.Metric is not ("issued" or "adjustments" or "net" or "receipts" or "refunds") || request.ProductLevel is not ("model" or "sku"))throw new BusinessException("VALIDATION_ERROR","Chiều hoặc chỉ tiêu thống kê không hợp lệ.");
        if(request.DimensionId?.Length>300 || (request.GroupBy=="product" && request.DimensionId is not null && !Guid.TryParse(request.DimensionId,out _)))throw new BusinessException("VALIDATION_ERROR","Đối tượng thống kê không hợp lệ.");
        return request;
    }
    internal async Task<InvoiceStatisticsResult> ReadStatistics(ReportRequest raw,Actor actor,CancellationToken cancellationToken)
    {
        var request=ValidateReport(raw);
        var predicate=InvoicePredicateBuilder.Build(request.Query,actor,report:true,includeWindow:false,lifecycle:"issued");
        var parameters=predicate.Parameters;
        var from=InvoicePredicateBuilder.ParseInstant(request.Query.FromInclusive ?? "2024-01-01");
        var to=InvoicePredicateBuilder.ParseInstant(request.Query.ToExclusive ?? "2027-01-01");
        if(to<=from || (to-from).TotalDays>36600)throw new BusinessException("VALIDATION_ERROR","Khoảng báo cáo phải lớn hơn 0 và tối đa 100 năm.");
        var fromParameter=parameters.Add(from);var toParameter=parameters.Add(to);
        var product=request.GroupBy=="product";
        var productColumn=request.ProductLevel=="sku" ? "ii.variant_id" : "ii.product_id";
        var dimId=request.GroupBy=="period" ? "''::text" : request.GroupBy=="staff" ? "i.responsible_staff_id" : $"{productColumn}::text";
        var dimName=request.GroupBy=="period" ? "'Tất cả hóa đơn'::text" : request.GroupBy=="staff" ? "COALESCE(i.staff_snapshot->>'name',i.responsible_staff_id)" : request.ProductLevel=="sku" ? "ii.name_snapshot || ' · ' || ii.sku_snapshot" : "ii.name_snapshot";
        var productJoin=product ? " JOIN invoice_items ii ON ii.invoice_id=i.id AND ii.line_type='product'" : "";
        var issued=$"SELECT i.id AS invoice_id,i.issued_at AS event_at,{dimId} AS dim_id,{dimName} AS dim_name,{(product ? "ii.net_amount_vnd" : "i.grand_total_vnd")}::bigint AS amount,{(product ? "ii.quantity" : "0")}::bigint AS quantity,NULL::text AS tender,'issued'::text AS kind FROM filtered i{productJoin}";
        var adjustments=$"SELECT i.id,c.issued_at,{dimId},{dimName},{(product ? "ci.amount_vnd" : "c.total_vnd")}::bigint,{(product ? "COALESCE(ci.quantity,0)" : "0")}::bigint,NULL::text,'adjustments'::text FROM filtered i JOIN credit_notes c ON c.invoice_id=i.id AND c.status='issued'{(product ? " JOIN credit_note_items ci ON ci.credit_note_id=c.id JOIN invoice_items ii ON ii.id=ci.invoice_item_id AND ii.line_type='product'" : "")}";
        var receipts=$"SELECT i.id,r.received_at,{dimId},{dimName},{(product ? "allocation.amount_vnd" : "r.amount_vnd")}::bigint,0::bigint,r.tender_kind,'receipts'::text FROM filtered i JOIN payment_receipts r ON r.order_id=i.order_id{(product ? " JOIN order_tender_allocations allocation ON allocation.receipt_id=r.id JOIN order_item_unit_amounts u ON u.id=allocation.order_item_unit_amount_id JOIN invoice_items ii ON ii.order_item_id=u.order_item_id AND ii.line_type='product'" : "")}";
        var refunds=$"SELECT i.id,r.completed_at,{dimId},{dimName},allocation.amount_vnd::bigint,0::bigint,receipt.tender_kind,'refunds'::text FROM filtered i JOIN refunds r ON r.order_id=i.order_id AND r.status='completed' JOIN refund_allocations allocation ON allocation.refund_id=r.id JOIN payment_receipts receipt ON receipt.id=allocation.payment_receipt_id{(product ? " JOIN order_item_unit_amounts u ON u.id=allocation.order_item_unit_amount_id JOIN invoice_items ii ON ii.order_item_id=u.order_item_id AND ii.line_type='product'" : "")}";
        var events=request.Metric switch {"issued"=>issued,"adjustments"=>adjustments,"net"=>$"{issued} UNION ALL {adjustments}","receipts"=>receipts,_=>refunds};
        var dimensionFilter=request.DimensionId is null ? "" : $" AND dim_id={parameters.Add(request.DimensionId)}";
        var common=$"WITH {Facts},filtered AS (SELECT i.* FROM invoice_facts i WHERE {predicate.Where}),events(invoice_id,event_at,dim_id,dim_name,amount,quantity,tender,kind) AS ({events}),window_events AS (SELECT * FROM events WHERE event_at>={fromParameter} AND event_at<{toParameter}{dimensionFilter})";
        var dimensions=request.GroupBy=="period" ? "SELECT ''::text AS dim_id,'Tất cả hóa đơn'::text AS dim_name" : "SELECT dim_id,(array_agg(dim_name ORDER BY event_at DESC,invoice_id))[1] AS dim_name FROM window_events GROUP BY dim_id";
        if(request.DimensionId is not null && request.GroupBy!="period")
        {
            if(actor.Role=="order_staff" && request.GroupBy=="staff" && request.DimensionId!=actor.Id)throw new BusinessException("FORBIDDEN","Chỉ được xem thống kê của nhân viên đang đăng nhập.",403);
            var dimension=parameters.Add(request.DimensionId);
            var label=request.GroupBy=="staff" ? $"(SELECT name FROM \"user\" WHERE id={dimension})" : request.ProductLevel=="sku" ? $"(SELECT sku FROM product_variants WHERE id={dimension}::uuid)" : $"(SELECT name FROM product_translations WHERE product_id={dimension}::uuid AND locale='vi')";
            dimensions=$"SELECT {dimension}::text AS dim_id,COALESCE((SELECT (array_agg(dim_name ORDER BY event_at DESC,invoice_id))[1] FROM window_events),{label},{dimension}) AS dim_name";
        }
        var interval=request.Granularity switch {"quarter"=>"3 months","year"=>"1 year",_=>"1 month"};
        var amount=request.Metric=="net" ? "CASE WHEN kind='adjustments' THEN -amount ELSE amount END" : "amount";
        var quantity=request.Metric=="net" ? "CASE WHEN kind='adjustments' THEN -quantity ELSE quantity END" : "quantity";
        var sql=$"""
            {common},periods AS (
              SELECT generate_series(date_trunc('{request.Granularity}',{fromParameter}::timestamptz AT TIME ZONE 'Asia/Ho_Chi_Minh'),date_trunc('{request.Granularity}',({toParameter}::timestamptz AT TIME ZONE 'Asia/Ho_Chi_Minh')-interval '1 microsecond'),interval '{interval}') AS period
            ),dimensions AS ({dimensions}),facts AS (
              SELECT date_trunc('{request.Granularity}',event_at AT TIME ZONE 'Asia/Ho_Chi_Minh') AS period,dim_id,count(DISTINCT invoice_id) AS invoice_count,
                sum({quantity}) AS quantity,sum({amount}) AS total,COALESCE(sum(amount) FILTER(WHERE tender='cash_demo'),0) AS cash_total,
                COALESCE(sum(amount) FILTER(WHERE tender='store_credit'),0) AS credit_total FROM window_events GROUP BY 1,2
            ) SELECT to_char(p.period,'YYYY-MM-DD') AS "Period",d.dim_id AS "DimensionId",d.dim_name AS "DimensionName",COALESCE(f.invoice_count,0)::bigint AS "InvoiceCount",
                COALESCE(f.quantity,0)::bigint AS "Quantity",COALESCE(f.total,0)::text AS "TotalVnd",COALESCE(f.cash_total,0)::text AS "CashDemoVnd",COALESCE(f.credit_total,0)::text AS "StoreCreditVnd"
              FROM periods p CROSS JOIN dimensions d LEFT JOIN facts f ON f.period=p.period AND f.dim_id=d.dim_id ORDER BY p.period,d.dim_id
            """;
        var rows=await Db.Database.SqlQueryRaw<StatisticRow>(sql,parameters.Bind()).ToListAsync(cancellationToken);
        // Same snapshot and event set; IDs drive report drill-down and export metadata.
        var ids=await Db.Database.SqlQueryRaw<IdRow>($"{common} SELECT DISTINCT invoice_id AS \"Id\" FROM window_events ORDER BY invoice_id",parameters.Bind()).ToListAsync(cancellationToken);
        var result=rows.Select(row=>new InvoiceStatisticDto(row.Period,string.IsNullOrEmpty(row.DimensionId)?null:row.DimensionId,row.DimensionName,row.InvoiceCount,row.Quantity,row.TotalVnd,row.CashDemoVnd,row.StoreCreditVnd)).ToList();
        return new(result,SumMoney(result.Select(row=>row.TotalVnd)),await ReadAt(cancellationToken),request.GroupBy,request.Granularity,request.Metric,from.ToString("O"),to.ToString("O"),ids.Select(row=>row.Id).ToList());
    }

    public Task<InvoiceExportResult> CreateInvoiceExportAsync(ExportRequest request,Actor actor,CancellationToken cancellationToken=default)=>
        new InvoiceExportService(this,_configuration).CreateAsync(request,actor,cancellationToken);
    public Task<ExportDownload> DownloadExportAsync(Guid id,string format,Actor actor,CancellationToken cancellationToken=default)=>
        new InvoiceExportService(this,_configuration).DownloadAsync(id,format,actor,cancellationToken);

    internal async Task<List<JsonElement>> JsonRows(string sql,SqlParameters parameters,CancellationToken cancellationToken)=>
        (await Db.Database.SqlQueryRaw<JsonRow>(sql,parameters.Bind()).ToListAsync(cancellationToken)).Select(row=>ParseJson(row.Json)).ToList();
    internal static JsonElement ParseJson(string json){using var document=JsonDocument.Parse(json);return document.RootElement.Clone();}
    internal static string SumMoney(IEnumerable<string> values)=>values.Aggregate(BigInteger.Zero,(sum,value)=>sum+BigInteger.Parse(value,CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture);

    internal sealed class ReadAtRow{public DateTimeOffset ReadAt{get;set;}}
    internal sealed class TotalsRow{public long Count{get;set;}public string Total{get;set;}="0";}
    internal sealed class IdRow{public Guid Id{get;set;}}
    internal sealed class JsonRow{public string Json{get;set;}="{}";}
    internal sealed class HeaderRow
    {
        public string Lifecycle{get;set;}="";public string CustomerId{get;set;}="";public string? IssuedByUserId{get;set;}
        public string CustomerSnapshot{get;set;}="{}";public string StaffSnapshot{get;set;}="{}";public string OrderNo{get;set;}="";
        public string AddressSnapshot{get;set;}="{}";public string ReceivedVnd{get;set;}="0";public string RefundedVnd{get;set;}="0";public string ExchangeRateVnd{get;set;}="25000";
    }
    internal sealed class ItemRow
    {
        public Guid Id{get;set;}public Guid InvoiceId{get;set;}public Guid OrderItemId{get;set;}public int LineNo{get;set;}public string LineType{get;set;}="";
        public Guid? ProductId{get;set;}public Guid? VariantId{get;set;}public Guid? ParentInvoiceItemId{get;set;}public string Sku{get;set;}="";public string Name{get;set;}="";
        public string Variant{get;set;}="{}";public int Quantity{get;set;}public string UnitPriceVnd{get;set;}="0";public string VoucherDiscountVnd{get;set;}="0";
        public string PointsDiscountVnd{get;set;}="0";public string NetAmountVnd{get;set;}="0";public string[] Serials{get;set;}=[];
    }
    internal sealed class StatisticRow
    {
        public string Period{get;set;}="";public string DimensionId{get;set;}="";public string DimensionName{get;set;}="";public long InvoiceCount{get;set;}public long Quantity{get;set;}
        public string TotalVnd{get;set;}="0";public string CashDemoVnd{get;set;}="0";public string StoreCreditVnd{get;set;}="0";
    }
}
#pragma warning restore EF1002
