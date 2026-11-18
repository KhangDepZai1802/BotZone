using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class WarrantyClaim
{
    public Guid Id { get; set; }

    public Guid EntitlementId { get; set; }

    public string State { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string DamageKind { get; set; } = null!;

    public string? InspectionResult { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

    public virtual WarrantyEntitlement Entitlement { get; set; } = null!;

    public virtual ICollection<WarrantyEvent> WarrantyEvents { get; set; } = new List<WarrantyEvent>();

    public virtual WarrantyReplacement? WarrantyReplacement { get; set; }

    public virtual ICollection<MediaAsset> Media { get; set; } = new List<MediaAsset>();
}
