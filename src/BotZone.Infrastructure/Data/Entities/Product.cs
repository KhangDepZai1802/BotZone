using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Product
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Slug { get; set; } = null!;

    public string ModelCode { get; set; } = null!;

    public string BusinessStatus { get; set; } = null!;

    public string? Generation { get; set; }

    public string SourceUrl { get; set; } = null!;

    public string Specs { get; set; } = null!;

    public string Poster { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();

    public virtual ICollection<ProductMedium> ProductMedia { get; set; } = new List<ProductMedium>();

    public virtual ICollection<ProductTranslation> ProductTranslations { get; set; } = new List<ProductTranslation>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

    public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();

    public virtual ICollection<TradeInItem> TradeInItems { get; set; } = new List<TradeInItem>();

    public virtual ICollection<Product> AccessoryProducts { get; set; } = new List<Product>();

    public virtual ICollection<Product> TargetProducts { get; set; } = new List<Product>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
