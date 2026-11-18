using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class StoreCreditAccount
{
    public string UserId { get; set; } = null!;

    public long BalanceVnd { get; set; }

    public virtual ICollection<StoreCreditEntry> StoreCreditEntries { get; set; } = new List<StoreCreditEntry>();

    public virtual User User { get; set; } = null!;
}
