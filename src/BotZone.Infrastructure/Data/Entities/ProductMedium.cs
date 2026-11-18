using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class ProductMedium
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid? VariantId { get; set; }

    public Guid MediaId { get; set; }

    public int Position { get; set; }

    public string Role { get; set; } = null!;

    public virtual MediaAsset Media { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ProductVariant? ProductVariant { get; set; }
}
