using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class InventoryAllocation
{
    public Guid Id { get; set; }

    public Guid OrderItemId { get; set; }

    public Guid OrderItemUnitAmountId { get; set; }

    public Guid LotId { get; set; }

    public Guid? InventoryUnitId { get; set; }

    public int Quantity { get; set; }

    public long CostVnd { get; set; }

    public string State { get; set; } = null!;

    public virtual InventoryUnit? InventoryUnit { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual OrderItemUnitAmount OrderItemUnitAmount { get; set; } = null!;
}
