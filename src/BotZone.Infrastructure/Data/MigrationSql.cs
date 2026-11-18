using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
namespace BotZone.Infrastructure.Data;
internal static class MigrationSql{
 public static string Read(string suffix){var a=typeof(MigrationSql).Assembly;var name=a.GetManifestResourceNames().Single(n=>n.EndsWith(suffix,StringComparison.Ordinal));using var r=new StreamReader(a.GetManifestResourceStream(name)!);return r.ReadToEnd();}
 public static void Adopt(MigrationBuilder b){
  // Existing databases are verified, never blindly marked as an InitialCreate.
  var core=Read("0001_core.sql");var guards=Read("0002_guards.sql");
  var c1=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(core))).ToLowerInvariant();
  var c2=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(guards))).ToLowerInvariant();
  b.Sql($"""
DO $adopt$
BEGIN
 IF to_regclass('public.schema_migrations') IS NULL THEN
  IF EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name<>'__EFMigrationsHistory') THEN RAISE EXCEPTION 'Unknown existing schema: baseline refused'; END IF;
  EXECUTE $core${core}$core$;
  EXECUTE $guards${guards}$guards$;
  CREATE TABLE schema_migrations(name text PRIMARY KEY,checksum text NOT NULL,applied_at timestamptz NOT NULL DEFAULT now());
  INSERT INTO schema_migrations(name,checksum) VALUES ('0001_core.sql','{c1}'),('0002_guards.sql','{c2}');
 END IF;
END $adopt$;
""");
  b.Sql(Read("baseline-assert.sql"));
 }
}
