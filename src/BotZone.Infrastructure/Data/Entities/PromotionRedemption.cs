using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class PromotionRedemption
{
    public Guid Id { get; set; }

    public Guid PromotionId { get; set; }

    public Guid PromotionVersionId { get; set; }

    public string CustomerId { get; set; } = null!;

    public Guid OrderId { get; set; }

    public string Status { get; set; } = null!;

    public long AmountVnd { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public virtual User Customer { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<OrderAdjustment> OrderAdjustments { get; set; } = new List<OrderAdjustment>();

    public virtual Promotion Promotion { get; set; } = null!;

    public virtual PromotionVersion PromotionVersion { get; set; } = null!;
}
