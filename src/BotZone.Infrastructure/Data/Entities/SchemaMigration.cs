using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class SchemaMigration
{
    public string Name { get; set; } = null!;

    public string Checksum { get; set; } = null!;

    public DateTime AppliedAt { get; set; }
}
