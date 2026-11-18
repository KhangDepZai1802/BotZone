using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class LandingDraft
{
    public Guid PageId { get; set; }

    public int Revision { get; set; }

    public string Sections { get; set; } = null!;

    public string? UpdatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual LandingPage Page { get; set; } = null!;

    public virtual User? UpdatedByNavigation { get; set; }
}
