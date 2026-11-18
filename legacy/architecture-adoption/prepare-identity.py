from pathlib import Path
root=Path(__file__).resolve().parents[1]
folder=root/'src/BotZone.Infrastructure'
migration=next((folder/'Data/Migrations').glob('*_AdoptLegacySchema.cs'))
migration.write_text('''using Microsoft.EntityFrameworkCore.Migrations;
namespace BotZone.Infrastructure.Data.Migrations;
public partial class AdoptLegacySchema : Migration {
 protected override void Up(MigrationBuilder migrationBuilder) => MigrationSql.Adopt(migrationBuilder);
 protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Restoring the pre-EF database requires the verified backup; no destructive automatic downgrade.");
}
''',encoding='utf-8')
proj=folder/'BotZone.Infrastructure.csproj'
s=proj.read_text(encoding='utf-8-sig')
s=s.replace('</Project>','<ItemGroup><EmbeddedResource Include="../../migrations/*.sql" Link="Data/Sql/%(Filename)%(Extension)"/><EmbeddedResource Include="Data/Sql/*.sql"/></ItemGroup></Project>')
proj.write_text(s,encoding='utf-8')
p=folder/'Data/Entities/User.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('public partial class User','public partial class User : Microsoft.AspNetCore.Identity.IdentityUser<string>')
s=s.replace('    public string Id { get; set; } = null!;','').replace('    public string Email { get; set; } = null!;','').replace('    public bool EmailVerified { get; set; }','    public bool PasswordResetRequired { get; set; }')
p.write_text(s,encoding='utf-8')
p=folder/'Data/BotZoneDbContext.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('BotZoneDbContext : DbContext','BotZoneDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<User>')
s=s.replace('protected override void OnModelCreating(ModelBuilder modelBuilder)\n    {','protected override void OnModelCreating(ModelBuilder modelBuilder)\n    {\n        base.OnModelCreating(modelBuilder);')
s=s.replace('entity.Property(e => e.EmailVerified).HasColumnName("email_verified");','entity.Property(e => e.EmailConfirmed).HasColumnName("email_verified");')
p.write_text(s,encoding='utf-8')
print('Prepared verified EF adoption and Identity model extension; no database changed.')
