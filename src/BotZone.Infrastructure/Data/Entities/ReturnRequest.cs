using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class ReturnRequest
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string CustomerId { get; set; } = null!;

    public string State { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? InspectionResult { get; set; }

    public string? InspectedBy { get; set; }

    public DateTime? InspectedAt { get; set; }

    public long FeeVnd { get; set; }

    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

    public virtual User? InspectedByNavigation { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();

    public virtual ICollection<MediaAsset> Media { get; set; } = new List<MediaAsset>();
}
