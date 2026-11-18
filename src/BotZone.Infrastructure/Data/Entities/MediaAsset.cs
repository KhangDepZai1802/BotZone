using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class MediaAsset
{
    public Guid Id { get; set; }

    public string StorageKey { get; set; } = null!;

    public string Visibility { get; set; } = null!;

    public string Mime { get; set; } = null!;

    public long SizeBytes { get; set; }

    public string Checksum { get; set; } = null!;

    public string ValidationStatus { get; set; } = null!;

    public string? SourceUrl { get; set; }

    public string? LicenseNote { get; set; }

    public string? OwnerUserId { get; set; }

    public string Metadata { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User? OwnerUser { get; set; }

    public virtual ICollection<ProductMedium> ProductMedia { get; set; } = new List<ProductMedium>();

    public virtual ICollection<WarrantyClaim> Claims { get; set; } = new List<WarrantyClaim>();

    public virtual ICollection<TradeInInspection> Inspections { get; set; } = new List<TradeInInspection>();

    public virtual ICollection<LandingVersion> LandingVersions { get; set; } = new List<LandingVersion>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();
}
