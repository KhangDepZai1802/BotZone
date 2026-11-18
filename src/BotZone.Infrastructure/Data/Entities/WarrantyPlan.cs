using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class WarrantyPlan
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<WarrantyPlanVersion> WarrantyPlanVersions { get; set; } = new List<WarrantyPlanVersion>();
}
