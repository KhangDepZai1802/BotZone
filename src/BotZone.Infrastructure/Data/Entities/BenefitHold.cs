using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class BenefitHold
{
    public Guid Id { get; set; }

    public string CustomerId { get; set; } = null!;

    public Guid OrderId { get; set; }

    public string Kind { get; set; } = null!;

    public long Amount { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual User Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
