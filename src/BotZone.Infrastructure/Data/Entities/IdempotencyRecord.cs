using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class IdempotencyRecord
{
    public string Scope { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string RequestHash { get; set; } = null!;

    public string Result { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
