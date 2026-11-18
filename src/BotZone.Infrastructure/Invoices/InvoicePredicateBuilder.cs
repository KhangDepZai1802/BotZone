using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BotZone.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace BotZone.Infrastructure.Invoices;

public sealed class SqlParameters
{
    private readonly List<(string Name, object Value, NpgsqlDbType Type)> _values = [];
    public string Add(object value, NpgsqlDbType? type = null)
    {
        var name = "p" + _values.Count;
        var actualType = type ?? value switch
        {
            Guid => NpgsqlDbType.Uuid, Guid[] => NpgsqlDbType.Array | NpgsqlDbType.Uuid,
            int => NpgsqlDbType.Integer, long => NpgsqlDbType.Bigint,
            DateTimeOffset => NpgsqlDbType.TimestampTz, bool => NpgsqlDbType.Boolean,
            _ => NpgsqlDbType.Text
        };
        _values.Add((name,value,actualType));
        return "@" + name;
    }
    public object[] Bind() => _values.Select(value => (object)new NpgsqlParameter(value.Name,value.Type) {Value=value.Value}).ToArray();
    public IReadOnlyList<object> Values => _values.Select(value => value.Value).ToList();
}

public sealed record InvoicePredicate(string Where, string OrderBy, SqlParameters Parameters, InvoiceQuery Query);

public static partial class InvoicePredicateBuilder
{
    private static readonly Dictionary<string,string> Fields = new(StringComparer.Ordinal)
    {
        ["invoiceNo"]="i.invoice_no", ["issuedAt"]="i.issued_at", ["grandTotalVnd"]="i.grand_total_vnd",
        ["discountTotalVnd"]="i.discount_total_vnd", ["responsibleStaffId"]="i.responsible_staff_id",
        ["customerId"]="i.customer_id", ["lifecycle"]="i.lifecycle", ["paymentStatus"]="i.payment_status"
    };
    private static readonly HashSet<string> Operators = [">", "<", "=", "<>", ">=", "<="];
    private static readonly HashSet<string> NullableFields = ["issuedAt", "invoiceNo", "responsibleStaffId"];
    private static readonly HashSet<string> SortFields = ["issuedAt", "invoiceNo", "grandTotalVnd"];
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    private static partial Regex DayRegex();
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,7})?)?(?:Z|[+-]\d{2}:\d{2})$")]
    private static partial Regex TimestampRegex();
    private static BusinessException Invalid(string message) => new("VALIDATION_ERROR",message);

    public static DateTimeOffset ParseInstant(string? value)
    {
        if (value is null) throw Invalid("Ngày cần YYYY-MM-DD hoặc timestamp có múi giờ.");
        if (DayRegex().IsMatch(value))
        {
            if (!DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day)) throw Invalid("Ngày không hợp lệ.");
            return new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(7)).ToUniversalTime();
        }
        if (!TimestampRegex().IsMatch(value) || !DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.None,out var instant)) throw Invalid("Ngày cần timestamp hợp lệ và có múi giờ.");
        // PostgreSQL timestamptz stores microseconds. Accept .NET round-trip
        // timestamps (seven digits) and expose the same truncated bound we bind.
        var utc=instant.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks-utc.Ticks%10,TimeSpan.Zero);
    }

    public static InvoiceQuery Validate(InvoiceQuery? query)
    {
        query ??= new();
        if (query.Page < 1 || query.Page > 1000000 || query.PageSize < 1 || query.PageSize > 100) throw Invalid("Trang từ 1, kích thước trang từ 1 đến 100.");
        if (query.Sort is null || query.Sort.Count > 3 || query.Sort.Any(sort => sort is null || !SortFields.Contains(sort.Field) || (sort.Direction != "asc" && sort.Direction != "desc"))) throw Invalid("Thứ tự sắp xếp không hợp lệ.");
        if (query.Lifecycle is not (null or "draft" or "issued")) throw Invalid("Trạng thái hóa đơn không hợp lệ.");
        if (query.QuickSearch?.Length > 300) throw Invalid("Tìm nhanh tối đa 300 ký tự.");
        var from=query.FromInclusive is null ? (DateTimeOffset?)null : ParseInstant(query.FromInclusive);
        var to=query.ToExclusive is null ? (DateTimeOffset?)null : ParseInstant(query.ToExclusive);
        if (from.HasValue && to.HasValue && from>=to) throw Invalid("Ngày kết thúc phải sau ngày bắt đầu.");
        var leaves=0;var nodes=0;
        if(query.Filter is not null)ValidateNode(query.Filter,1,ref leaves,ref nodes);
        return query;
    }
    private static void ValidateNode(FilterNode? node,int depth,ref int leaves,ref int nodes)
    {
        if(node is null || depth>4 || ++nodes>90)throw Invalid("Bộ lọc tối đa bốn cấp và ba mươi điều kiện.");
        if(node is InvoiceGroupFilter group)
        {
            if(group.Logic is not ("AND" or "OR") || group.Children is null || group.Children.Count is <1 or >30)throw Invalid("Nhóm cần AND/OR và từ một đến ba mươi điều kiện.");
            foreach(var child in group.Children)ValidateNode(child,depth+1,ref leaves,ref nodes);
            return;
        }
        if(node is InvoiceNotFilter not){ValidateNode(not.Child,depth+1,ref leaves,ref nodes);return;}
        if(++leaves>30)throw Invalid("Bộ lọc tối đa ba mươi điều kiện.");
        switch(node)
        {
            case InvoiceProductFilter product:
                if(product.ProductId==Guid.Empty || product.Mode is not ("contains" or "not_contains"))throw Invalid("Điều kiện sản phẩm không hợp lệ.");
                return;
            case InvoiceNullFilter nullFilter:
                if(!NullableFields.Contains(nullFilter.Field))throw Invalid("Trường không hỗ trợ kiểm tra trống.");
                return;
            case InvoiceRuleFilter rule:
                if(rule.Field is null || !Fields.ContainsKey(rule.Field)||!Operators.Contains(rule.Op))throw Invalid("Trường hoặc toán tử không được hỗ trợ.");
                if(rule.Field is "grandTotalVnd" or "discountTotalVnd") {ParseMoney(rule.Value);return;}
                if(rule.Value.ValueKind!=JsonValueKind.String || rule.Value.GetString() is not string value || value.Length>300)throw Invalid("Giá trị cần văn bản tối đa 300 ký tự.");
                if(rule.Field=="issuedAt")ParseInstant(value);
                if(rule.Field=="lifecycle" && value is not ("draft" or "issued"))throw Invalid("Trạng thái hóa đơn không hợp lệ.");
                if(rule.Field=="paymentStatus" && value is not ("unpaid" or "partially_paid" or "paid" or "partially_refunded" or "refunded"))throw Invalid("Trạng thái thanh toán không hợp lệ.");
                return;
            default:throw Invalid("Loại điều kiện không được hỗ trợ.");
        }
    }
    public static long ParseMoney(JsonElement value)
    {
        var raw=value.ValueKind==JsonValueKind.String ? value.GetString() : value.ValueKind==JsonValueKind.Number ? value.GetRawText() : null;
        if(raw is null || !long.TryParse(raw,NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var amount))throw Invalid("Tiền phải là số nguyên VND trong miền BIGINT.");
        // Numbers sent by JavaScript must fit its safe integer range; strings retain all BIGINT digits.
        if(value.ValueKind==JsonValueKind.Number && (amount>9007199254740991L || amount< -9007199254740991L))throw Invalid("Tiền lớn phải gửi dưới dạng chuỗi số nguyên VND.");
        return amount;
    }

    public static InvoicePredicate Build(InvoiceQuery? raw,Actor actor,bool report=false,bool includeWindow=true,bool allowCustomer=false,string? lifecycle=null)
    {
        var query=Validate(raw);var parameters=new SqlParameters();var clauses=new List<string>();
        if(actor.Role=="owner")clauses.Add("TRUE");
        else if(actor.Role=="order_staff")clauses.Add(report ? $"i.responsible_staff_id={parameters.Add(actor.Id)}" : "TRUE");
        else if(actor.Role=="customer" && allowCustomer && !report)clauses.Add($"i.customer_id={parameters.Add(actor.Id)}");
        else throw new BusinessException("FORBIDDEN","Bạn không có quyền đọc hóa đơn này.",403);
        var status=lifecycle ?? query.Lifecycle ?? "issued";
        if(status!="any")clauses.Add($"i.lifecycle={parameters.Add(status)}");
        if(includeWindow)
        {
            if(query.FromInclusive is not null)clauses.Add($"i.issued_at>={parameters.Add(ParseInstant(query.FromInclusive))}");
            if(query.ToExclusive is not null)clauses.Add($"i.issued_at<{parameters.Add(ParseInstant(query.ToExclusive))}");
        }
        if(!string.IsNullOrWhiteSpace(query.QuickSearch))
        {
            var term="%"+query.QuickSearch.Replace("\\","\\\\").Replace("%","\\%").Replace("_","\\_")+"%";
            var parameter=parameters.Add(term);
            clauses.Add($"(i.invoice_no ILIKE {parameter} OR COALESCE(i.customer_snapshot->>'name',i.customer_snapshot->>'displayName','') ILIKE {parameter})");
        }
        if(query.Filter is not null)clauses.Add(Compile(query.Filter,parameters));
        var sort=query.Sort.Select(sort=>$"{Fields[sort.Field]} {sort.Direction.ToUpperInvariant()} NULLS LAST").ToList();
        sort.Add($"i.id {(query.Sort.FirstOrDefault()?.Direction=="asc" ? "ASC" : "DESC")}");
        return new(string.Join(" AND ",clauses.Select(clause=>$"({clause})")),string.Join(", ",sort),parameters,query);
    }

    private static string Compile(FilterNode node,SqlParameters parameters) => node switch
    {
        InvoiceGroupFilter group=>$"({string.Join($" {group.Logic} ",group.Children.Select(child=>Compile(child,parameters)))})",
        InvoiceNotFilter not=>$"(NOT ({Compile(not.Child,parameters)}))",
        InvoiceNullFilter nullable=>$"{Fields[nullable.Field]} IS {(nullable.IsNull ? "" : "NOT ")}NULL",
        InvoiceProductFilter product=>$"{(product.Mode=="not_contains" ? "NOT " : "")}EXISTS (SELECT 1 FROM invoice_items fi WHERE fi.invoice_id=i.id AND fi.line_type='product' AND fi.product_id={parameters.Add(product.ProductId)})",
        InvoiceRuleFilter rule=>CompileRule(rule,parameters),
        _=>throw Invalid("Loại điều kiện không được hỗ trợ.")
    };
    private static string CompileRule(InvoiceRuleFilter rule,SqlParameters parameters)
    {
        var column=Fields[rule.Field];
        if(rule.Field is "grandTotalVnd" or "discountTotalVnd")return $"{column} {rule.Op} {parameters.Add(ParseMoney(rule.Value))}";
        var value=rule.Value.GetString()!;
        if(rule.Field!="issuedAt")return $"{column} {rule.Op} {parameters.Add(value)}";
        var instant=ParseInstant(value);
        if(!DayRegex().IsMatch(value))return $"{column} {rule.Op} {parameters.Add(instant)}";
        if(rule.Op is "=" or "<>")
        {
            var day=$"({column}>={parameters.Add(instant)} AND {column}<{parameters.Add(instant.AddDays(1))})";
            return rule.Op=="<>" ? $"(NOT {day})" : day;
        }
        var op=rule.Op is ">" or ">=" ? ">=" : "<";
        var boundary=rule.Op is ">" or "<=" ? instant.AddDays(1) : instant;
        return $"{column} {op} {parameters.Add(boundary)}";
    }
}
