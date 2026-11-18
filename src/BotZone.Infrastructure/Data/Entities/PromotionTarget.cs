using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class PromotionTarget
{
    public Guid Id { get; set; }

    public Guid PromotionVersionId { get; set; }

    public Guid? VariantId { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? CategoryId { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Product? Product { get; set; }

    public virtual PromotionVersion PromotionVersion { get; set; } = null!;

    public virtual ProductVariant? Variant { get; set; }
}
