using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class AttributeDefinition
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string ValueType { get; set; } = null!;

    public string? Unit { get; set; }

    public bool CompareEnabled { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();

    public virtual ICollection<VariantAttributeValue> VariantAttributeValues { get; set; } = new List<VariantAttributeValue>();
}
