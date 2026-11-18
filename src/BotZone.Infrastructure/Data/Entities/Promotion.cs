using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Promotion
{
    public Guid Id { get; set; }

    public string CodeNormalized { get; set; } = null!;

    public bool Active { get; set; }

    public int? MaxUses { get; set; }

    public int? MaxUsesPerCustomer { get; set; }

    public virtual ICollection<PromotionRedemption> PromotionRedemptions { get; set; } = new List<PromotionRedemption>();

    public virtual ICollection<PromotionVersion> PromotionVersions { get; set; } = new List<PromotionVersion>();
}
