using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Warehouse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();
}
