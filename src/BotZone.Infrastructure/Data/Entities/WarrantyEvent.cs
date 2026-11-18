using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class WarrantyEvent
{
    public Guid Id { get; set; }

    public Guid ClaimId { get; set; }

    public string ActorId { get; set; } = null!;

    public string Event { get; set; } = null!;

    public string Note { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User Actor { get; set; } = null!;

    public virtual WarrantyClaim Claim { get; set; } = null!;
}
