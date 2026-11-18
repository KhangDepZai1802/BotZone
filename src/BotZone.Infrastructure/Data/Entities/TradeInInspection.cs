using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class TradeInInspection
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public string InspectorStaffId { get; set; } = null!;

    public int ChecklistVersion { get; set; }

    public string AssessedCondition { get; set; } = null!;

    public DateTime InspectedAt { get; set; }

    public virtual StaffProfile InspectorStaff { get; set; } = null!;

    public virtual TradeInItem Item { get; set; } = null!;

    public virtual ICollection<MediaAsset> Media { get; set; } = new List<MediaAsset>();
}
