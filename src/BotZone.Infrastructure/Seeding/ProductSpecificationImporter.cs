using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BotZone.Infrastructure.Services;

namespace BotZone.Infrastructure.Seeding;

public sealed record SpecificationChange(string ModelCode, Guid ProductId, JsonElement Before, JsonElement After);
public sealed record SpecificationImportResult(bool Applied, int Verified, int Changed, int AlreadyApplied,
    int SpecificationCount, IReadOnlyList<SpecificationChange> Changes);

public sealed class ProductSpecificationImporter(SqlStore sql)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    public static async Task<ProductSpecification[]> ValidateAsync(string root, CancellationToken token = default)
    {
        var entries = JsonSerializer.Deserialize<ProductSpecification[]>(
            await File.ReadAllTextAsync(Path.Combine(root, "data/product-specifications.json"), token), JsonOptions) ?? [];
        using var catalogue = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "data/catalogue-demo.json"), token));
        var models = catalogue.RootElement.EnumerateArray().ToDictionary(p => p.Text("modelCode"), StringComparer.Ordinal);
        Require(entries.Length == models.Count && entries.Select(p => p.ModelCode).ToHashSet(StringComparer.Ordinal).SetEquals(models.Keys),
            "Specifications must cover every existing catalogue model exactly once.");
        foreach (var entry in entries)
        {
            Require(entry.Name == models[entry.ModelCode].Text("name"), $"Model name mismatch: {entry.ModelCode}.");
            Require(DateTimeOffset.TryParse(entry.VerifiedAt, out _), $"Missing verification date: {entry.ModelCode}.");
            Require(entry.Sources.Length > 0 && entry.Sources.All(source => !string.IsNullOrWhiteSpace(source.Title)
                && Uri.TryCreate(source.Url, UriKind.Absolute, out var url) && url.Scheme == "https"
                && (url.Host == "apple.com" || url.Host.EndsWith(".apple.com", StringComparison.OrdinalIgnoreCase))),
                $"Verified official sources required: {entry.ModelCode}.");
            Require(entry.Groups.Length > 0, $"No specification groups: {entry.ModelCode}.");
            var groups = new HashSet<string>(StringComparer.Ordinal);
            var items = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in entry.Groups)
            {
                Require(ValidId(group.Id) && groups.Add(group.Id) && Complete(group.Label) && group.Items.Length > 0,
                    $"Invalid group: {entry.ModelCode}/{group.Id}.");
                foreach (var item in group.Items)
                    Require(ValidId(item.Id) && items.Add(item.Id) && Complete(item.Label) && Complete(item.Value),
                        $"Invalid or duplicate specification: {entry.ModelCode}/{item.Id}.");
            }
            Require(items.Count >= 5 && entry.HighlightIds.Length > 0 && entry.HighlightIds.All(items.Contains),
                $"Incomplete specifications/highlights: {entry.ModelCode}.");
        }
        return entries;
    }
    private static bool ValidId(string value) => Regex.IsMatch(value, "^[a-z][a-z0-9_]{0,63}$");
    private static bool Complete(BilingualText text) => text is not null && !string.IsNullOrWhiteSpace(text.Vi)
        && !string.IsNullOrWhiteSpace(text.En) && text.Vi.Length <= 1600 && text.En.Length <= 1600;
    public async Task<SpecificationImportResult> RunAsync(string root, bool apply)
    {
        var entries = await ValidateAsync(root, sql.Token);
        return await sql.Transaction<SpecificationImportResult>(null, async () =>
        {
            var changes = new List<SpecificationChange>();
            foreach (var entry in entries)
            {
                var product = await sql.Optional("SELECT id,specs FROM products WHERE model_code=$1 FOR UPDATE", entry.ModelCode);
                Require(product is not null, $"Missing product: {entry.ModelCode}. No data changed.");
                var before = product!.J("specs");
                var after = JsonNode.Parse(before.GetRawText())!.AsObject();
                var technical = JsonSerializer.SerializeToNode(entry, JsonOptions)!;
                if (after.TryGetPropertyValue(CataloguePresentation.TechnicalKey, out var current))
                {
                    Require(JsonNode.DeepEquals(current, technical),
                        $"Preserving existing technical content for {entry.ModelCode}; reconcile edits before importing. Nothing applied.");
                    continue;
                }
                after[CataloguePresentation.TechnicalKey] = technical;
                changes.Add(new(entry.ModelCode, product!.G("id"), before.Clone(), JsonSerializer.SerializeToElement(after, JsonOptions)));
            }
            if (apply)
                foreach (var change in changes)
                {
                    await sql.Exec("UPDATE products SET specs=$2::jsonb WHERE id=$1", change.ProductId, change.After.GetRawText());
                    await sql.Exec("""
                        INSERT INTO audit_events(action,entity_type,entity_id,before_data,after_data,reason)
                        VALUES('import_product_specifications','product',$1,$2::jsonb,$3::jsonb,$4)
                        """, change.ProductId.ToString(), Values.Json(new { specs = change.Before }),
                        Values.Json(new { specs = change.After }), "Verified bilingual Apple specifications; existing product metadata retained");
                }
            return new(apply, entries.Length, changes.Count, entries.Length - changes.Count,
                entries.Sum(entry => entry.Groups.Sum(group => group.Items.Length)), changes);
        });
    }
}
