using System.Security.Cryptography;
using BotZone.Contracts;

namespace BotZone.Infrastructure.Services;

// Explicit versioned content, pinned to the supplied v003 manifest. No remote loading or migrations.
public static class BundledHeroAssets
{
    public const string ProMax = "v003:iphone-18-pro-max", Duo = "v003:iphone-duo";
    private static readonly IReadOnlyDictionary<string,string> Hashes = new Dictionary<string,string>
    {
        ["iphone-18-pro-max-desktop.glb"] = "a9b94e46477b13a5e18e148f4039d3321890de3d9f2053a31d73ff4d5a5a8f3a",
        ["iphone-18-pro-max-mobile.glb"] = "aedce816e094688a777b1daacc0414f9c1c3a3705a1eef2de79640b95300d0c8",
        ["iphone-duo-desktop.glb"] = "6be09394df5059d77f84c76e0b2bc4d9143cf030bd2a76f07f20479f94acb32d",
        ["iphone-duo-mobile.glb"] = "35999aa5fb952cdcd06edad76505076f1e3fd490ae66440be4211eaa509c1628",
        ["iphone-18-pro-max.webp"] = "6d5af009515148ee1fd7fdfcc92c4598e4d6314eccbbb9a0e2c361fc5d116a9f",
        ["iphone-duo.webp"] = "206e8da838cb131a9025ec560c8df6fa8acefd9455eb852f17a00c72f29f5aa6"
    };

    public static bool IsModel(string id) => id is ProMax or Duo;
    public static bool IsPoster(string path) => path is "/models/v003/iphone-18-pro-max.webp" or "/models/v003/iphone-duo.webp";

    private static async Task<byte[]> ReadVerified(string root, string name, CancellationToken token)
    {
        var path = Path.Combine(root, "public/models/v003", name);
        Access.Ensure(Hashes.ContainsKey(name) && File.Exists(path), "VALIDATION_ERROR", "Thiếu tài nguyên v003 đã bàn giao.", 400);
        var bytes = await File.ReadAllBytesAsync(path, token);
        Access.Ensure(Convert.ToHexString(SHA256.HashData(bytes)).Equals(Hashes[name], StringComparison.OrdinalIgnoreCase),
            "VALIDATION_ERROR", "Checksum tài nguyên v003 không khớp bản bàn giao.", 400);
        return bytes;
    }

    public static async Task ValidateModel(string root, string id, CancellationToken token)
    {
        Access.Ensure(IsModel(id), "VALIDATION_ERROR", "Model v003 không được hỗ trợ.", 400);
        foreach (var quality in new[] { "desktop", "mobile" })
            MediaService.ValidateGlb(await ReadVerified(root, $"{id[5..]}-{quality}.glb", token));
    }

    public static async Task ValidatePoster(string root, string path, CancellationToken token)
    {
        Access.Ensure(IsPoster(path), "VALIDATION_ERROR", "Poster v003 không được hỗ trợ.", 400);
        MediaService.ValidateImage(await ReadVerified(root, Path.GetFileName(path), token));
    }
}
