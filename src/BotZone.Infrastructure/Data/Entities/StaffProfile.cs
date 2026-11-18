using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class StaffProfile
{
    public string UserId { get; set; } = null!;

    public string StaffCode { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool Active { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<TradeInInspection> TradeInInspections { get; set; } = new List<TradeInInspection>();

    public virtual User User { get; set; } = null!;
}
