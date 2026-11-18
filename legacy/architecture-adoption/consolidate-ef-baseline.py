from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]
source=(root/'artifacts/ef-scaffold-attempts/20261113070520_IdentitySessions.cs').read_text(encoding='utf-8-sig')
body=source.split('protected override void Up(MigrationBuilder migrationBuilder)',1)[1].split('/// <inheritdoc />',1)[0]
body=body[body.index('{')+1:body.rfind('}')]
body=re.sub(r'\s*migrationBuilder.AlterColumn<string>\(.*?oldType: "text"\);','',body,count=1,flags=re.S)
target=next((root/'src/BotZone.Infrastructure/Data/Migrations').glob('*_AdoptLegacyAndIdentity.cs'))
target.write_text('''using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
namespace BotZone.Infrastructure.Data.Migrations;
public partial class AdoptLegacyAndIdentity : Migration {
 protected override void Up(MigrationBuilder migrationBuilder) {
  MigrationSql.Adopt(migrationBuilder);
'''+body+'''
  migrationBuilder.Sql(MigrationSql.Read("identity-cutover.sql"));
 }
 protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("No destructive downgrade: use the verified pre-EF backup in an isolated database.");
}
''',encoding='utf-8')
print('Single EF adoption migration verifies legacy schema then adds Identity; old SQL unchanged.')
