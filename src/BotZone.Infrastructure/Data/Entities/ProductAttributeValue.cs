using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class ProductAttributeValue
{
    public Guid ProductId { get; set; }

    public Guid AttributeId { get; set; }

    public string? ValueText { get; set; }

    public decimal? ValueNumber { get; set; }

    public bool? ValueBoolean { get; set; }

    public virtual AttributeDefinition Attribute { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
