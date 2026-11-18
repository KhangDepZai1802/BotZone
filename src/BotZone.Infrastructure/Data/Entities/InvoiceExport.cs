using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class InvoiceExport
{
    public Guid Id { get; set; }

    public string RequestedByUserId { get; set; } = null!;

    public string QueryJson { get; set; } = null!;

    public DateTime ReadAt { get; set; }

    public int RowCount { get; set; }

    public long TotalVnd { get; set; }

    public string Status { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public string FileManifest { get; set; } = null!;

    public string RoleSnapshot { get; set; } = null!;

    public virtual User RequestedByUser { get; set; } = null!;
}
