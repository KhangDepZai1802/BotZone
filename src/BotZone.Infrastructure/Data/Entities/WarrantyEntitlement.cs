using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class WarrantyEntitlement
{
    public Guid Id { get; set; }

    public Guid OrderItemUnitAmountId { get; set; }

    public Guid PlanVersionId { get; set; }

    public Guid? InventoryUnitId { get; set; }

    public DateOnly? StartsOn { get; set; }

    public DateOnly? EndsOn { get; set; }

    public string State { get; set; } = null!;

    public bool Used { get; set; }

    public virtual InventoryUnit? InventoryUnit { get; set; }

    public virtual OrderItemUnitAmount OrderItemUnitAmount { get; set; } = null!;

    public virtual WarrantyPlanVersion PlanVersion { get; set; } = null!;

    public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; } = new List<WarrantyClaim>();
}
