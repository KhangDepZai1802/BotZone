using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class VariantAttributeValue
{
    public Guid VariantId { get; set; }

    public Guid AttributeId { get; set; }

    public string? ValueText { get; set; }

    public decimal? ValueNumber { get; set; }

    public bool? ValueBoolean { get; set; }

    public virtual AttributeDefinition Attribute { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
