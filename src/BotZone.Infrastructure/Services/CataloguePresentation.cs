using System.Text.Json;
using System.Text.Json.Nodes;

namespace BotZone.Infrastructure.Services;

public sealed record BilingualText(string Vi, string En)
{
    public string In(string locale) => locale == "en" ? En : Vi;
}
public sealed record SpecificationSource(string Title, string Url);
public sealed record SpecificationItem(string Id, BilingualText Label, BilingualText Value);
public sealed record SpecificationGroup(string Id, BilingualText Label, SpecificationItem[] Items);
public sealed record ProductSpecification(string ModelCode, string Name, string VerifiedAt,
    SpecificationSource[] Sources, SpecificationGroup[] Groups, string[] HighlightIds);
public sealed record SpecificationItemView(string Id, string Label, string Value);
public sealed record SpecificationGroupView(string Id, string Label, SpecificationItemView[] Items);
public sealed record TechnicalSpecificationsView(string VerifiedAt, SpecificationSource[] Sources,
    SpecificationGroupView[] Groups, string[] HighlightIds);

// Display labels are separate from the raw SKU options used by stock, photo matching and invoices.
// Technical content is read from the existing PostgreSQL product JSON, never from a second client catalogue.
public static class CataloguePresentation
{
    public const string TechnicalKey = "technicalSpecifications";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Dictionary<string, BilingualText> OptionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["White"] = new("Trắng", "White"), ["Black"] = new("Đen", "Black"),
        ["Midnight"] = new("Đêm xanh thẳm", "Midnight"), ["Starlight"] = new("Ánh sao", "Starlight"),
        ["Blue"] = new("Xanh dương", "Blue"), ["Silver"] = new("Bạc", "Silver"),
        ["Sky Blue"] = new("Xanh da trời", "Sky Blue"), ["Dark Bronze"] = new("Đồng sẫm", "Dark Bronze"),
        ["Burgundy"] = new("Burgundy", "Burgundy"), ["Băng Thanh"] = new("Băng Thanh", "Glacier"),
        ["Trắng Ánh Sao"] = new("Trắng Ánh Sao", "Star White"), ["Trời Đêm"] = new("Trời Đêm", "Night Sky"),
        ["Aluminum"] = new("Nhôm", "Aluminum"), ["Sport Band"] = new("Dây thể thao", "Sport Band"),
        ["Standard"] = new("Kính tiêu chuẩn", "Standard glass"),
        ["Tilt-adjustable"] = new("Chân đế chỉnh độ nghiêng", "Tilt-adjustable stand"),
        ["Type A"] = new("Phích cắm loại A", "Type A plug")
    };
    public static void Attach(List<Dictionary<string, object?>> products, string locale)
    {
        foreach (var product in products)
        {
            if (product.J("specs").TryGetProperty(TechnicalKey, out var technical))
            {
                var data = technical.Deserialize<ProductSpecification>(JsonOptions);
                if (data is not null)
                {
                    var groups = data.Groups.Select(group => new SpecificationGroupView(group.Id, group.Label.In(locale),
                        group.Items.Select(item => new SpecificationItemView(item.Id, item.Label.In(locale), item.Value.In(locale))).ToArray())).ToArray();
                    var sources = data.Sources.Select(source => locale == "en" ? source : source with
                    { Title = source.Title.Replace("Apple technical specifications", "Thông số kỹ thuật Apple", StringComparison.Ordinal) }).ToArray();
                    product["technicalSpecs"] = new TechnicalSpecificationsView(data.VerifiedAt, sources, groups, data.HighlightIds);
                    product["specs"] = groups.SelectMany(group => group.Items.Select(item => (Key: group.Label + " · " + item.Label, item.Value)))
                        .ToDictionary(item => item.Key, item => item.Value);
                }
            }
            else
            {
                // Legacy fixture bookkeeping is not a customer-facing specification.
                product["specs"] = new Dictionary<string, string>();
            }
            var variants = JsonNode.Parse(product.J("variants").GetRawText())!.AsArray();
            foreach (var variant in variants)
            {
                if (variant?["options"] is not JsonObject options) continue;
                var labels = new JsonObject();
                foreach (var option in options)
                {
                    var value = option.Value?.GetValue<string>() ?? "";
                    labels[option.Key] = option.Key == "pack"
                        ? (locale == "en" ? $"{value} item" : $"{value} chiếc")
                        : OptionNames.TryGetValue(value, out var name) ? name.In(locale) : value;
                }
                variant["optionLabels"] = labels;
            }
            product["variants"] = variants;
            if (locale != "vi") continue;
            product["name"] = product.S("name")
                .Replace("(1st generation)", "(thế hệ 1)", StringComparison.Ordinal)
                .Replace("(2nd generation)", "(thế hệ 2)", StringComparison.Ordinal)
                .Replace("(3rd generation)", "(thế hệ 3)", StringComparison.Ordinal)
                .Replace("-inch", " inch", StringComparison.Ordinal)
                .Replace("60W USB-C Charge Cable", "Cáp sạc USB-C 60W", StringComparison.Ordinal)
                .Replace("20W USB-C Power Adapter", "Củ sạc USB-C 20W", StringComparison.Ordinal)
                .Replace("iPhone 18 Pro Max Silicone Case with MagSafe", "Ốp lưng silicon MagSafe cho iPhone 18 Pro Max", StringComparison.Ordinal);
        }
    }
}
