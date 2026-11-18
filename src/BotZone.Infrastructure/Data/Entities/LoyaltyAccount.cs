using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class LoyaltyAccount
{
    public string UserId { get; set; } = null!;

    public int BalancePoints { get; set; }

    public long NetSpendVnd { get; set; }

    public virtual ICollection<LoyaltyEntry> LoyaltyEntries { get; set; } = new List<LoyaltyEntry>();

    public virtual User User { get; set; } = null!;
}
