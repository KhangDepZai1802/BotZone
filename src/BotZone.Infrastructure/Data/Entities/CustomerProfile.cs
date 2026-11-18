using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class CustomerProfile
{
    public string UserId { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? Phone { get; set; }

    public virtual User User { get; set; } = null!;
}
