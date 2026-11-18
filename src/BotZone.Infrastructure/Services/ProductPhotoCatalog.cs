using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace BotZone.Infrastructure.Services;

public sealed record ProductPhotoView(string Path, string Alt, string[] VariantColors, double FramingScale = 1);

// Bundled editorial metadata belongs to the API. Clients receive the same model/color mapping.
public sealed class ProductPhotoCatalog(IConfiguration configuration)
{
    private readonly object gate = new();
    private DateTime loadedAt;
    private Dictionary<string, JsonElement> entries = new(StringComparer.Ordinal);
    private string ManifestPath => Path.Combine(configuration["BOTZONE_ROOT"]
        ?? Environment.GetEnvironmentVariable("BOTZONE_ROOT") ?? Directory.GetCurrentDirectory(), "data/product-photos.json");

    private Dictionary<string, JsonElement> Snapshot()
    {
        lock (gate)
        {
            var file = new FileInfo(ManifestPath);
            if (!file.Exists) return new(StringComparer.Ordinal);
            if (file.LastWriteTimeUtc != loadedAt)
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file.FullName));
                entries = document.RootElement.EnumerateArray().ToDictionary(p => p.Text("modelCode"), p => p.Clone(), StringComparer.Ordinal);
                loadedAt = file.LastWriteTimeUtc;
            }
            return entries;
        }
    }

    public void Attach(List<Dictionary<string, object?>> products, string locale)
    {
        var snapshot = Snapshot();
        foreach (var product in products)
        {
            if (!snapshot.TryGetValue(product.S("modelCode"), out var entry) || product.S("poster") != entry.Text("path")) continue;
            var images = new List<ProductPhotoView>();
            void Add(JsonElement image)
            {
                var path = image.Text("path");
                if (!Regex.IsMatch(path, "^/product-photos/[a-z0-9_-]+\\.(jpg|jpeg|png|webp)$")) return;
                var colors = image.TryGetProperty("variantColors", out var values) && values.ValueKind == JsonValueKind.Array
                    ? values.EnumerateArray().Select(value => value.GetString()!).ToArray() : [];
                var scale = image.TryGetProperty("framingScale", out var framing) && framing.TryGetDouble(out var value) && double.IsFinite(value)
                    ? Math.Clamp(value, 1, 3) : 1;
                images.Add(new(path, image.Text(locale == "en" ? "altEn" : "altVi"), colors, scale));
            }
            Add(entry);
            if (entry.TryGetProperty("galleries", out var galleries)) foreach (var image in galleries.EnumerateArray()) Add(image);
            product["photos"] = images;
        }
    }
}
