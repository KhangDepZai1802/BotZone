using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using BotZone.Infrastructure.Services;

namespace BotZone.Infrastructure.Seeding;

public sealed record ProductPhoto(
    string ModelCode, string Name, string Path, string PreviousPoster,
    string SourcePage, string SourceImage, string AltVi, string AltEn,
    int Width, int Height, string Sha256, string VerifiedAt, string VerificationNote,
    ProductPhotoVariant[]? Galleries = null, string[]? VariantColors = null);
public sealed record ProductPhotoVariant(string Path, string SourcePage, string SourceImage, string AltVi, string AltEn,
    int Width, int Height, string Sha256, string VerifiedAt, string VerificationNote, string[]? VariantColors = null);
public sealed record ProductPhotoChange(string ModelCode, Guid ProductId, string Before, string After);
public sealed record ProductPhotoImportResult(bool Applied, int Verified, int Changed, int AlreadyApplied,
    IReadOnlyList<ProductPhotoChange> Changes);

// Content import through the existing C# backend/EF connection. No schema changes or reseeding.
public sealed class ProductPhotoImporter(SqlStore sql)
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static async Task<List<ProductPhoto>> ValidateAsync(string root, CancellationToken token = default)
    {
        root = System.IO.Path.GetFullPath(root);
        var manifestPath = System.IO.Path.Combine(root, "data/product-photos.json");
        var photos = JsonSerializer.Deserialize<List<ProductPhoto>>(await File.ReadAllTextAsync(manifestPath, token),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        Require(photos.Count > 0, "Product photo manifest is empty.");
        Require(photos.Select(p => p.ModelCode).Distinct(StringComparer.Ordinal).Count() == photos.Count,
            "Photo manifest contains duplicate model codes.");
        Require(photos.Select(p => p.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() == photos.Count,
            "Each model needs its own explicit photo record/path.");
        using var catalogue = JsonDocument.Parse(await File.ReadAllTextAsync(System.IO.Path.Combine(root, "data/catalogue-demo.json"), token));
        var models = catalogue.RootElement.EnumerateArray().ToDictionary(p => p.GetProperty("modelCode").GetString()!, StringComparer.Ordinal);
        Require(models.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(photos.Select(p => p.ModelCode)),
            "Photo manifest must cover the complete catalogue, with no unknown model codes.");
        foreach (var photo in photos)
        {
            Require(photo.Name == models[photo.ModelCode].GetProperty("name").GetString(), $"Model name mismatch: {photo.ModelCode}.");
            Require(Regex.IsMatch(photo.PreviousPoster ?? "", "^/media/[a-z0-9_-]+\\.svg$"),
                $"Missing original poster for {photo.ModelCode}.");
            var main = new ProductPhotoVariant(photo.Path, photo.SourcePage, photo.SourceImage, photo.AltVi, photo.AltEn,
                photo.Width, photo.Height, photo.Sha256, photo.VerifiedAt, photo.VerificationNote, photo.VariantColors);
            var images = new[] { main }.Concat(photo.Galleries ?? []).ToArray();
            var knownColors = models[photo.ModelCode].GetProperty("variants").EnumerateArray()
                .Select(variant => variant.GetProperty("options"))
                .Where(options => options.TryGetProperty("color", out _))
                .Select(options => options.GetProperty("color").GetString()!).ToHashSet(StringComparer.Ordinal);
            var mappedColors = images.SelectMany(image => image.VariantColors ?? []).ToArray();
            Require(knownColors.SetEquals(mappedColors) && mappedColors.Distinct(StringComparer.Ordinal).Count() == mappedColors.Length,
                $"Photos must map every configured color exactly once: {photo.ModelCode}.");
            foreach (var asset in images)
            {
            Require(Regex.IsMatch(asset.Path ?? "", "^/product-photos/[a-z0-9_-]+\\.(jpg|jpeg|png|webp)$"),
                $"Invalid local photo path: {photo.ModelCode}.");
            foreach (var source in new[] { asset.SourcePage, asset.SourceImage })
                Require(Uri.TryCreate(source, UriKind.Absolute, out var uri) && uri.Scheme == "https",
                    $"Missing HTTPS source: {photo.ModelCode}.");
            Require(!string.IsNullOrWhiteSpace(asset.AltVi) && !string.IsNullOrWhiteSpace(asset.AltEn)
                && !string.IsNullOrWhiteSpace(asset.VerificationNote) && DateTimeOffset.TryParse(asset.VerifiedAt, out _),
                $"Missing provenance: {photo.ModelCode}.");
            var file = System.IO.Path.Combine(root, "public", asset.Path!.TrimStart('/'));
            var info = new FileInfo(file);
            Require(info.Exists && info.Length is > 0 and <= 5 * 1024 * 1024, $"Missing/oversize photo: {photo.ModelCode}.");
            Require((info.Attributes & FileAttributes.ReparsePoint) == 0, "Photo assets cannot be links.");
            var bytes = await File.ReadAllBytesAsync(file, token);
            Require(Convert.ToHexString(SHA256.HashData(bytes)).Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase),
                $"Photo checksum mismatch: {photo.ModelCode}.");
            var image = MediaService.ValidateImage(bytes);
            Require(image.Width == asset.Width && image.Height == asset.Height && Math.Max(image.Width, image.Height) >= 500,
                $"Photo dimensions do not match manifest: {photo.ModelCode}.");
            var extension = System.IO.Path.GetExtension(file).ToLowerInvariant();
            Require(image.Mime switch { "image/jpeg" => extension is ".jpg" or ".jpeg", "image/png" => extension == ".png", "image/webp" => extension == ".webp", _ => false },
                $"Photo MIME/extension mismatch: {photo.ModelCode}.");
            }
        }
        return photos;
    }

    public async Task<ProductPhotoImportResult> RunAsync(string root, bool apply)
    {
        var photos = await ValidateAsync(root, sql.Token);
        return await sql.Transaction<ProductPhotoImportResult>(null, async () =>
        {
            var changes = new List<ProductPhotoChange>();
            foreach (var photo in photos)
            {
                var product = await sql.Optional("SELECT id,poster FROM products WHERE model_code=$1 FOR UPDATE", photo.ModelCode);
                Require(product is not null, $"Product is missing from this database: {photo.ModelCode}. No content was changed.");
                var before = product!.S("poster");
                if (before == photo.Path) continue;
                Require(before == photo.PreviousPoster,
                    $"Preserving custom poster for {photo.ModelCode}: expected {photo.PreviousPoster}, found {before}. Nothing was applied.");
                changes.Add(new(photo.ModelCode, product!.G("id"), before, photo.Path));
            }
            // Check the entire plan before writing any product. The transaction also protects against concurrent edits.
            if (apply)
            {
                foreach (var change in changes)
                {
                    var photo = photos.Single(p => p.ModelCode == change.ModelCode);
                    await sql.Exec("UPDATE products SET poster=$2 WHERE id=$1", change.ProductId, change.After);
                    await sql.Exec("""
                        INSERT INTO audit_events(action,entity_type,entity_id,before_data,after_data,reason)
                        VALUES('import_product_photo','product',$1,$2::jsonb,$3::jsonb,$4)
                        """, change.ProductId.ToString(), Values.Json(new { poster = change.Before }),
                        Values.Json(new { modelCode = photo.ModelCode, poster = change.After, photo.SourcePage, photo.SourceImage, photo.Sha256 }),
                        "Local verified catalogue photography import; data/product-photos.json");
                }
            }
            return new(apply, photos.Count, changes.Count, photos.Count - changes.Count, changes);
        });
    }
}
