using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class ReturnItemUnit
{
    public Guid ReturnItemId { get; set; }

    public Guid OrderItemId { get; set; }

    public Guid OrderItemUnitAmountId { get; set; }

    public Guid? InventoryUnitId { get; set; }

    public virtual InventoryUnit? InventoryUnit { get; set; }

    public virtual OrderItemUnitAmount OrderItemUnitAmount { get; set; } = null!;

    public virtual ReturnItem ReturnItem { get; set; } = null!;
}
