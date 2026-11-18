using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Category
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string NameVi { get; set; } = null!;

    public string NameEn { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<AttributeDefinition> AttributeDefinitions { get; set; } = new List<AttributeDefinition>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();
}
