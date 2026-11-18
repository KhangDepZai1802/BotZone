using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class WarrantyReplacement
{
    public Guid Id { get; set; }

    public Guid ClaimId { get; set; }

    public Guid? OldUnitId { get; set; }

    public Guid? NewUnitId { get; set; }

    public Guid? OldLotId {get;set;}
    public Guid? NewLotId {get;set;}
    public Guid? OrderItemUnitAmountId {get;set;}

    public DateTime CreatedAt { get; set; }

    public virtual WarrantyClaim Claim { get; set; } = null!;

    public virtual InventoryUnit NewUnit { get; set; } = null!;

    public virtual InventoryUnit OldUnit { get; set; } = null!;
}
