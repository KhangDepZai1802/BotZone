using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Npgsql;

// Read-only migration/backup verification. Never emits account data or connection strings.
var options = args.Chunk(2).ToDictionary(x => x[0], x => x.Length == 2 ? x[1] : throw new ArgumentException("Expected option value."));
var database = options["--database"];
if (database != "botzone" && !database.StartsWith("botzone_baseline_test_") && !database.StartsWith("botzone_integration_test_") && !database.StartsWith("botzone_restore_")) throw new ArgumentException("Expected an explicit BotZone local database.");
var root = Path.GetFullPath(options.GetValueOrDefault("--root", Directory.GetCurrentDirectory()));
var uploads = Path.GetFullPath(options.GetValueOrDefault("--uploads", "uploads"), root);
var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("BOTZONE_VERIFICATION_CONNECTION") ?? throw new ArgumentException("Set BOTZONE_VERIFICATION_CONNECTION for a read-only verification session.")) { Database = database };
await using var db = new NpgsqlConnection(connection.ConnectionString);
await db.OpenAsync();
await using var tx = await db.BeginTransactionAsync(IsolationLevel.RepeatableRead);
await using (var command = new NpgsqlCommand("SET TRANSACTION READ ONLY", db, tx)) await command.ExecuteNonQueryAsync();
async Task<List<Dictionary<string, object?>>> Rows(string sql)
{
    await using var command = new NpgsqlCommand(sql, db, tx);
    await using var reader = await command.ExecuteReaderAsync();
    var result = new List<Dictionary<string, object?>>();
    while (await reader.ReadAsync())
    {
        var row = new Dictionary<string, object?>();
        for (var n = 0; n < reader.FieldCount; n++) row[reader.GetName(n)] = reader.IsDBNull(n) ? null : reader.GetValue(n);
        result.Add(row);
    }
    return result;
}
var tables = new SortedDictionary<string, object>();
foreach (var row in await Rows("SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename"))
{
    var name = (string)row["tablename"]!;
    var identifier = '"' + name.Replace("\"", "\"\"") + '"';
    tables[name] = (await Rows($"SELECT count(*) row_count, md5(COALESCE(string_agg(to_jsonb(t)::text,E'\\n' ORDER BY to_jsonb(t)::text),'')) content_md5 FROM public.{identifier} t"))[0];
}
var sequences = await Rows("SELECT sequencename,last_value FROM pg_sequences WHERE schemaname='public' ORDER BY sequencename");
var golden = (await Rows("SELECT count(*)::int issued_count,COALESCE(sum(grand_total_vnd),0)::text issued_vnd,(SELECT COALESCE(sum(c.total_vnd),0)::text FROM credit_notes c JOIN invoices i ON i.id=c.invoice_id WHERE c.status='issued' AND i.invoice_no ~ '^G0[1-8]$') credits_vnd FROM invoices WHERE lifecycle='issued' AND invoice_no ~ '^G0[1-8]$'"))[0];
var checks = new List<object>();
foreach (var row in await Rows("SELECT id,storage_key,checksum,size_bytes FROM media_assets WHERE validation_status='valid' ORDER BY id"))
{
    var key = (string)row["storage_key"]!;
    var path = Path.GetFullPath(key, uploads);
    if (!path.StartsWith(uploads.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new Exception("Media path escaped uploads.");
    if (!File.Exists(path)) throw new Exception("Missing media: " + key);
    var actual = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))).ToLowerInvariant();
    if (actual != ((string)row["checksum"]!).ToLowerInvariant() || new FileInfo(path).Length != (long)row["size_bytes"]!) throw new Exception("Media checksum/length differs: " + key);
    checks.Add(new { id = row["id"], storageKey = key, sha256 = actual, passed = true });
}
var history = await Rows("SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"");
var legacy = await Rows("SELECT name,checksum FROM schema_migrations ORDER BY name");
var categories = await Rows("SELECT code,name_vi,name_en FROM categories ORDER BY code");
var historicalStaff = await Rows("SELECT s.staff_code,s.active,count(i.id)::int invoice_count FROM staff_profiles s LEFT JOIN invoices i ON i.responsible_staff_id=s.user_id WHERE s.staff_code='NV03' GROUP BY s.staff_code,s.active");
await tx.CommitAsync();
if ((int)golden["issued_count"]! != 8 || (string)golden["issued_vnd"]! != "97130000" || (string)golden["credits_vnd"]! != "3500000") throw new Exception("Golden totals differ.");
bool? compared = null;
if (options.TryGetValue("--compare", out var previous))
{
    using var expected = JsonDocument.Parse(await File.ReadAllTextAsync(previous));
    var actual = JsonSerializer.SerializeToElement(tables);
    if (!JsonElement.DeepEquals(expected.RootElement.GetProperty("tables"), actual)) throw new Exception("Restored row counts/content fingerprints differ.");
    if (!JsonElement.DeepEquals(expected.RootElement.GetProperty("sequences"), JsonSerializer.SerializeToElement(sequences))) throw new Exception("Restored sequence values differ.");
    compared = true;
}
var result = new { database, checkedAt = DateTimeOffset.UtcNow, readOnly = true, tableCount = tables.Count, tables, sequences, golden, netVnd = "93630000", history, legacy, mediaChecks = checks, categories, historicalStaff, matchedBackupSource = compared };
var output = Path.GetFullPath(options["--output"], root);
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(new { database, tables = tables.Count, media = checks.Count, golden, matchedBackupSource = compared, output }));
