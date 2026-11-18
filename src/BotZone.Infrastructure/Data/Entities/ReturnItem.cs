using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class ReturnItem
{
    public Guid Id { get; set; }

    public Guid ReturnRequestId { get; set; }

    public Guid OrderId { get; set; }

    public Guid OrderItemId { get; set; }

    public int Qty { get; set; }

    public string? ActualReceivedIdentifier { get; set; }

    public virtual OrderItem OrderItem { get; set; } = null!;

    public virtual ICollection<ReturnItemUnit> ReturnItemUnits { get; set; } = new List<ReturnItemUnit>();

    public virtual ReturnRequest ReturnRequest { get; set; } = null!;
}
