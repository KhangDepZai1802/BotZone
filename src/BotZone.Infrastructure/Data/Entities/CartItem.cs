using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class CartItem
{
    public Guid Id { get; set; }

    public Guid CartId { get; set; }

    public Guid VariantId { get; set; }

    public int Quantity { get; set; }

    public bool Care { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
