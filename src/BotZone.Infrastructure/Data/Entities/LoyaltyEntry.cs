using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class LoyaltyEntry
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public int Delta { get; set; }

    public string Kind { get; set; } = null!;

    public Guid OrderId { get; set; }

    public Guid? UnitId { get; set; }

    public string EventKey { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual OrderItemUnitAmount? Unit { get; set; }

    public virtual LoyaltyAccount User { get; set; } = null!;
}
