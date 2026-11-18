using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Cart
{
    public Guid Id { get; set; }

    public string CustomerId { get; set; } = null!;

    public Guid? AppliedPromotionVersionId { get; set; }

    public int Revision { get; set; }

    public virtual PromotionVersion? AppliedPromotionVersion { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual User Customer { get; set; } = null!;
}
