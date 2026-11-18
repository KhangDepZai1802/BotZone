using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotZone.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(InvoiceRuleFilter), "rule")]
[JsonDerivedType(typeof(InvoiceGroupFilter), "group")]
[JsonDerivedType(typeof(InvoiceNotFilter), "not")]
[JsonDerivedType(typeof(InvoiceNullFilter), "null")]
[JsonDerivedType(typeof(InvoiceProductFilter), "product")]
public abstract record FilterNode;
public sealed record InvoiceRuleFilter(string Field, string Op, JsonElement Value) : FilterNode;
public sealed record InvoiceGroupFilter(string Logic, List<FilterNode> Children) : FilterNode;
public sealed record InvoiceNotFilter(FilterNode Child) : FilterNode;
public sealed record InvoiceNullFilter(string Field, bool IsNull) : FilterNode;
public sealed record InvoiceProductFilter(Guid ProductId, string Mode) : FilterNode;
public sealed record InvoiceSort(string Field, string Direction);
public sealed record InvoiceQuery
{
    public FilterNode? Filter { get; init; }
    public string? FromInclusive { get; init; }
    public string? ToExclusive { get; init; }
    public List<InvoiceSort> Sort { get; init; } = [new("issuedAt", "desc")];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Lifecycle { get; init; }
    public string? QuickSearch { get; init; }
}
public class InvoiceListItemDto
{
    public Guid Id { get; set; }
    public string? InvoiceNo { get; set; }
    public Guid OrderId { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public string CustomerName { get; set; } = "";
    public string StaffName { get; set; } = "";
    public string? ResponsibleStaffId { get; set; }
    public long ItemCount { get; set; }
    public string SubtotalVnd { get; set; } = "0";
    public string DiscountTotalVnd { get; set; } = "0";
    public string GrandTotalVnd { get; set; } = "0";
    public string CreditNoteTotalVnd { get; set; } = "0";
    public string NetVnd { get; set; } = "0";
    public string PaymentStatus { get; set; } = "unpaid";
}
public sealed record InvoiceListResult(List<InvoiceListItemDto> Items, long MatchedCount, string MatchedTotalVnd, DateTimeOffset ReadAt, int Page, int PageSize);
public sealed record InvoiceDetailItemDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public Guid OrderItemId { get; init; }
    public int LineNo { get; init; }
    public string LineType { get; init; } = "";
    public Guid? ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public Guid? ParentInvoiceItemId { get; init; }
    public string Sku { get; init; } = "";
    public string Name { get; init; } = "";
    public JsonElement Variant { get; init; }
    public int Quantity { get; init; }
    public string UnitPriceVnd { get; init; } = "0";
    public string VoucherDiscountVnd { get; init; } = "0";
    public string PointsDiscountVnd { get; init; } = "0";
    public string NetAmountVnd { get; init; } = "0";
    public List<string> Serials { get; init; } = [];
}
public sealed class InvoiceDetailHeaderDto : InvoiceListItemDto
{
    public string Lifecycle { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public string? IssuedByUserId { get; set; }
    public JsonElement CustomerSnapshot { get; set; }
    public JsonElement StaffSnapshot { get; set; }
    public string OrderNo { get; set; } = "";
    public JsonElement AddressSnapshot { get; set; }
    public string ReceivedVnd { get; set; } = "0";
    public string RefundedVnd { get; set; } = "0";
    public string ExchangeRateVnd { get; set; } = "25000";
}
public sealed record InvoiceDetailDto(InvoiceDetailHeaderDto Invoice, List<InvoiceDetailItemDto> Items,
    List<JsonElement> Receipts, List<JsonElement> CreditNotes, List<JsonElement> Refunds,
    List<JsonElement> Units, List<JsonElement> Attempts, List<JsonElement> Promotions, DateTimeOffset ReadAt);
public sealed record ReportRequest
{
    public InvoiceQuery Query { get; init; } = new();
    public string GroupBy { get; init; } = "period";
    public string Granularity { get; init; } = "month";
    public string Metric { get; init; } = "issued";
    public string? DimensionId { get; init; }
    public string ProductLevel { get; init; } = "model";
}
public sealed record InvoiceStatisticDto(string Period, string? DimensionId, string DimensionName,
    long InvoiceCount, long Quantity, string TotalVnd, string CashDemoVnd, string StoreCreditVnd);
public sealed record InvoiceStatisticsResult(List<InvoiceStatisticDto> Rows, string TotalVnd, DateTimeOffset ReadAt,
    string GroupBy, string Granularity, string Metric, string FromInclusive, string ToExclusive, List<Guid> InvoiceIds);
public sealed record ExportRequest
{
    public InvoiceQuery? Query { get; init; }
    public Guid? InvoiceId { get; init; }
    public List<string> Formats { get; init; } = ["xlsx", "pdf"];
    public string Kind { get; init; } = "list";
    public string GroupBy { get; init; } = "period";
    public string Granularity { get; init; } = "month";
    public string Metric { get; init; } = "issued";
    public string? DimensionId { get; init; }
    public string ProductLevel { get; init; } = "model";
    public string Locale { get; init; } = "vi";
}
public sealed record InvoiceExportResult(Guid ExportId, long RowCount, string TotalVnd, DateTimeOffset ReadAt, Dictionary<string,string> Links);
public sealed record ExportDownload(byte[] Buffer, string Filename, string ContentType);
