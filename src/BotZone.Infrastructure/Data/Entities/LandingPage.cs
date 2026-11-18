using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class LandingPage
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = null!;

    public virtual LandingDraft? LandingDraft { get; set; }

    public virtual ICollection<LandingVersion> LandingVersions { get; set; } = new List<LandingVersion>();
}
