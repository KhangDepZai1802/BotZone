using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class WarrantyPlanVersion
{
    public Guid Id { get; set; }

    public Guid PlanId { get; set; }

    public int VersionNo { get; set; }

    public int TermMonths { get; set; }

    public long FeeVnd { get; set; }

    public string PolicySnapshot { get; set; } = null!;

    public virtual WarrantyPlan Plan { get; set; } = null!;

    public virtual ICollection<WarrantyEntitlement> WarrantyEntitlements { get; set; } = new List<WarrantyEntitlement>();
}
