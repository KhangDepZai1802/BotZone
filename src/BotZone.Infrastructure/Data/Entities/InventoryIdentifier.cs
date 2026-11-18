using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class InventoryIdentifier
{
    public Guid Id { get; set; }

    public Guid InventoryUnitId { get; set; }

    public string Kind { get; set; } = null!;

    public string NormalizedValue { get; set; } = null!;

    public virtual InventoryUnit InventoryUnit { get; set; } = null!;
}
