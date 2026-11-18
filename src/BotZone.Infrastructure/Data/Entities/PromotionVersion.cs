using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class PromotionVersion
{
    public Guid Id { get; set; }

    public Guid PromotionId { get; set; }

    public int VersionNo { get; set; }

    public string DiscountType { get; set; } = null!;

    public long Value { get; set; }

    public long? MaxDiscountVnd { get; set; }

    public long MinPurchaseVnd { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public bool AllProducts { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Promotion Promotion { get; set; } = null!;

    public virtual ICollection<PromotionRedemption> PromotionRedemptions { get; set; } = new List<PromotionRedemption>();

    public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();
}
