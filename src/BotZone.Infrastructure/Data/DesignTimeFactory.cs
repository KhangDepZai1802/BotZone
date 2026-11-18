using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace BotZone.Infrastructure.Data;
public class DesignTimeFactory:IDesignTimeDbContextFactory<BotZoneDbContext>{
 public BotZoneDbContext CreateDbContext(string[] args){var cs=Environment.GetEnvironmentVariable("BOTZONE_MIGRATION_CONNECTION")??Environment.GetEnvironmentVariable("ConnectionStrings__BotZone")??throw new InvalidOperationException("Set BOTZONE_MIGRATION_CONNECTION for EF design-time operations.");return new(new DbContextOptionsBuilder<BotZoneDbContext>().UseNpgsql(cs).Options);}
}
