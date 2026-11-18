using System;
using System.Collections.Generic;

namespace BotZone.Infrastructure.Data.Entities;

public partial class User : Microsoft.AspNetCore.Identity.IdentityUser<string>
{


    public string Name { get; set; } = null!;



    public bool PasswordResetRequired { get; set; }

    public string? Image { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual Address? Address { get; set; }

    public virtual ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();

    public virtual ICollection<BenefitHold> BenefitHolds { get; set; } = new List<BenefitHold>();

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

    public virtual CustomerProfile? CustomerProfile { get; set; }

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<Invoice> InvoiceCustomers { get; set; } = new List<Invoice>();

    public virtual ICollection<InvoiceExport> InvoiceExports { get; set; } = new List<InvoiceExport>();

    public virtual ICollection<Invoice> InvoiceIssuedByUsers { get; set; } = new List<Invoice>();

    public virtual ICollection<LandingDraft> LandingDrafts { get; set; } = new List<LandingDraft>();

    public virtual ICollection<LandingPublication> LandingPublications { get; set; } = new List<LandingPublication>();

    public virtual ICollection<LandingVersion> LandingVersions { get; set; } = new List<LandingVersion>();

    public virtual LoyaltyAccount? LoyaltyAccount { get; set; }

    public virtual ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PromotionRedemption> PromotionRedemptions { get; set; } = new List<PromotionRedemption>();

    public virtual ICollection<PurchaseOrder> PurchaseOrderApprovedByNavigations { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<PurchaseOrder> PurchaseOrderCreatedByNavigations { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<SettingVersion> SettingVersions { get; set; } = new List<SettingVersion>();

    public virtual StaffProfile? StaffProfile { get; set; }

    public virtual ICollection<StockAdjustment> StockAdjustmentApprovedByNavigations { get; set; } = new List<StockAdjustment>();

    public virtual ICollection<StockAdjustment> StockAdjustmentRequestedByNavigations { get; set; } = new List<StockAdjustment>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual StoreCreditAccount? StoreCreditAccount { get; set; }

    public virtual ICollection<TradeInQuote> TradeInQuoteApprovedByNavigations { get; set; } = new List<TradeInQuote>();

    public virtual ICollection<TradeInQuote> TradeInQuoteQuotedByNavigations { get; set; } = new List<TradeInQuote>();

    public virtual ICollection<TradeInRequest> TradeInRequests { get; set; } = new List<TradeInRequest>();

    public virtual ICollection<WarrantyEvent> WarrantyEvents { get; set; } = new List<WarrantyEvent>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
