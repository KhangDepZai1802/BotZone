using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class Address
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Recipient { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Province { get; set; } = null!;

    public string Ward { get; set; } = null!;

    public string AddressLine { get; set; } = null!;

    public bool IsDefault { get; set; }

    public virtual User User { get; set; } = null!;
}
