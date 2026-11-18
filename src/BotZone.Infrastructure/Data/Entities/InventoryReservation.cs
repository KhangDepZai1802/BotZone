using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class InventoryReservation
{
    public Guid Id { get; set; }

    public Guid OrderItemId { get; set; }

    public Guid VariantId { get; set; }

    public int Quantity { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
