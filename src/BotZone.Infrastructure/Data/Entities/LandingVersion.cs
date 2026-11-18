using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class LandingVersion
{
    public Guid Id { get; set; }

    public Guid PageId { get; set; }

    public int VersionNo { get; set; }

    public string Sections { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<LandingPublication> LandingPublications { get; set; } = new List<LandingPublication>();

    public virtual LandingPage Page { get; set; } = null!;

    public virtual ICollection<MediaAsset> Media { get; set; } = new List<MediaAsset>();
}
