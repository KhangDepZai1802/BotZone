using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class OrderAdjustment
{
    public Guid Id { get; set; }

    public Guid OrderItemId { get; set; }

    public string Kind { get; set; } = null!;

    public Guid? PromotionRedemptionId { get; set; }

    public long AmountVnd { get; set; }

    public string SourceEventKey { get; set; } = null!;

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual PromotionRedemption? PromotionRedemption { get; set; }
}
