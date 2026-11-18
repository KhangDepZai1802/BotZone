using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class ProductTranslation
{
    public Guid ProductId { get; set; }

    public string Locale { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string SeoTitle { get; set; } = null!;

    public string ContentStatus { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
