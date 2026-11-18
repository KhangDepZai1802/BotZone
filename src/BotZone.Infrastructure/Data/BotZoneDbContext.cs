using System;
using System.Collections.Generic;
using BotZone.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotZone.Infrastructure.Data;

public partial class BotZoneDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<User, Microsoft.AspNetCore.Identity.IdentityRole, string>
{
    public BotZoneDbContext(DbContextOptions<BotZoneDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<AppSetting> AppSettings { get; set; }

    public virtual DbSet<AttributeDefinition> AttributeDefinitions { get; set; }

    public virtual DbSet<AuditEvent> AuditEvents { get; set; }

    public virtual DbSet<BenefitHold> BenefitHolds { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<CreditNote> CreditNotes { get; set; }

    public virtual DbSet<CreditNoteItem> CreditNoteItems { get; set; }

    public virtual DbSet<CreditNoteItemUnit> CreditNoteItemUnits { get; set; }

    public virtual DbSet<CustomerProfile> CustomerProfiles { get; set; }

    public virtual DbSet<GoodsReceipt> GoodsReceipts { get; set; }

    public virtual DbSet<GoodsReceiptItem> GoodsReceiptItems { get; set; }

    public virtual DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

    public virtual DbSet<InventoryAllocation> InventoryAllocations { get; set; }

    public virtual DbSet<InventoryIdentifier> InventoryIdentifiers { get; set; }

    public virtual DbSet<InventoryLot> InventoryLots { get; set; }

    public virtual DbSet<InventoryReservation> InventoryReservations { get; set; }

    public virtual DbSet<InventoryUnit> InventoryUnits { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceExport> InvoiceExports { get; set; }

    public virtual DbSet<InvoiceItem> InvoiceItems { get; set; }

    public virtual DbSet<LandingDraft> LandingDrafts { get; set; }

    public virtual DbSet<LandingPage> LandingPages { get; set; }

    public virtual DbSet<LandingPublication> LandingPublications { get; set; }

    public virtual DbSet<LandingVersion> LandingVersions { get; set; }

    public virtual DbSet<LoyaltyAccount> LoyaltyAccounts { get; set; }

    public virtual DbSet<LoyaltyEntry> LoyaltyEntries { get; set; }

    public virtual DbSet<MediaAsset> MediaAssets { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderAdjustment> OrderAdjustments { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderItemUnitAmount> OrderItemUnitAmounts { get; set; }

    public virtual DbSet<OrderTenderAllocation> OrderTenderAllocations { get; set; }

    public virtual DbSet<OutboxEvent> OutboxEvents { get; set; }

    public virtual DbSet<PaymentAttempt> PaymentAttempts { get; set; }

    public virtual DbSet<PaymentEvent> PaymentEvents { get; set; }

    public virtual DbSet<PaymentReceipt> PaymentReceipts { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }

    public virtual DbSet<ProductMedium> ProductMedia { get; set; }

    public virtual DbSet<ProductTranslation> ProductTranslations { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionRedemption> PromotionRedemptions { get; set; }

    public virtual DbSet<PromotionTarget> PromotionTargets { get; set; }

    public virtual DbSet<PromotionVersion> PromotionVersions { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    public virtual DbSet<Refund> Refunds { get; set; }

    public virtual DbSet<RefundAllocation> RefundAllocations { get; set; }

    public virtual DbSet<ReturnItem> ReturnItems { get; set; }

    public virtual DbSet<ReturnItemUnit> ReturnItemUnits { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequests { get; set; }

    public virtual DbSet<SchemaMigration> SchemaMigrations { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<SettingVersion> SettingVersions { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<ShipmentEvent> ShipmentEvents { get; set; }

    public virtual DbSet<StaffProfile> StaffProfiles { get; set; }

    public virtual DbSet<StockAdjustment> StockAdjustments { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<StoreCreditAccount> StoreCreditAccounts { get; set; }

    public virtual DbSet<StoreCreditEntry> StoreCreditEntries { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<TradeInInspection> TradeInInspections { get; set; }

    public virtual DbSet<TradeInItem> TradeInItems { get; set; }

    public virtual DbSet<TradeInQuote> TradeInQuotes { get; set; }

    public virtual DbSet<TradeInQuoteItem> TradeInQuoteItems { get; set; }

    public virtual DbSet<TradeInRequest> TradeInRequests { get; set; }

    public virtual DbSet<TradeInShipment> TradeInShipments { get; set; }

    public override DbSet<User> Users { get; set; }

    public virtual DbSet<VariantAttributeValue> VariantAttributeValues { get; set; }

    public virtual DbSet<VariantStock> VariantStocks { get; set; }

    public virtual DbSet<Verification> Verifications { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarrantyClaim> WarrantyClaims { get; set; }

    public virtual DbSet<WarrantyEntitlement> WarrantyEntitlements { get; set; }

    public virtual DbSet<WarrantyEvent> WarrantyEvents { get; set; }

    public virtual DbSet<WarrantyPlan> WarrantyPlans { get; set; }

    public virtual DbSet<WarrantyPlanVersion> WarrantyPlanVersions { get; set; }

    public virtual DbSet<WarrantyReplacement> WarrantyReplacements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("account_pkey");

            entity.ToTable("account");

            entity.HasIndex(e => new { e.ProviderId, e.AccountId }, "account_provider_id_account_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessToken).HasColumnName("access_token");
            entity.Property(e => e.AccessTokenExpiresAt).HasColumnName("access_token_expires_at");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IdToken).HasColumnName("id_token");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
            entity.Property(e => e.Scope).HasColumnName("scope");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("account_user_id_fkey");
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("addresses_pkey");

            entity.ToTable("addresses");

            entity.HasIndex(e => e.UserId, "address_default_idx")
                .IsUnique()
                .HasFilter("is_default");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AddressLine).HasColumnName("address_line");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Province).HasColumnName("province");
            entity.Property(e => e.Recipient).HasColumnName("recipient");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Ward).HasColumnName("ward");

            entity.HasOne(d => d.User).WithOne(p => p.Address)
                .HasForeignKey<Address>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("addresses_user_id_fkey");
        });

        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("app_settings_pkey");

            entity.ToTable("app_settings");

            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value)
                .HasColumnType("jsonb")
                .HasColumnName("value");
            entity.Property(e => e.Version)
                .HasDefaultValue(1)
                .HasColumnName("version");
        });

        modelBuilder.Entity<AttributeDefinition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attribute_definitions_pkey");

            entity.ToTable("attribute_definitions");

            entity.HasIndex(e => new { e.CategoryId, e.Code }, "attribute_definitions_category_id_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CompareEnabled)
                .HasDefaultValue(true)
                .HasColumnName("compare_enabled");
            entity.Property(e => e.Unit).HasColumnName("unit");
            entity.Property(e => e.ValueType).HasColumnName("value_type");

            entity.HasOne(d => d.Category).WithMany(p => p.AttributeDefinitions)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("attribute_definitions_category_id_fkey");
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_events_pkey");

            entity.ToTable("audit_events");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Action).HasColumnName("action");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.AfterData)
                .HasColumnType("jsonb")
                .HasColumnName("after_data");
            entity.Property(e => e.BeforeData)
                .HasColumnType("jsonb")
                .HasColumnName("before_data");
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType).HasColumnName("entity_type");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("occurred_at");
            entity.Property(e => e.Reason).HasColumnName("reason");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditEvents)
                .HasForeignKey(d => d.ActorUserId)
                .HasConstraintName("audit_events_actor_user_id_fkey");
        });

        modelBuilder.Entity<BenefitHold>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("benefit_holds_pkey");

            entity.ToTable("benefit_holds");

            entity.HasIndex(e => new { e.OrderId, e.Kind }, "benefit_hold_active_idx")
                .IsUnique()
                .HasFilter("(status = 'active'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Customer).WithMany(p => p.BenefitHolds)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("benefit_holds_customer_id_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.BenefitHolds)
                .HasPrincipalKey(p => new { p.Id, p.CustomerId })
                .HasForeignKey(d => new { d.OrderId, d.CustomerId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("benefit_holds_order_id_customer_id_fkey");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("carts_pkey");

            entity.ToTable("carts");

            entity.HasIndex(e => e.CustomerId, "carts_customer_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AppliedPromotionVersionId).HasColumnName("applied_promotion_version_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Revision).HasColumnName("revision");

            entity.HasOne(d => d.AppliedPromotionVersion).WithMany(p => p.Carts)
                .HasForeignKey(d => d.AppliedPromotionVersionId)
                .HasConstraintName("carts_applied_promotion_version_id_fkey");

            entity.HasOne(d => d.Customer).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("carts_customer_id_fkey");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cart_items_pkey");

            entity.ToTable("cart_items");

            entity.HasIndex(e => new { e.CartId, e.VariantId, e.Care }, "cart_items_cart_id_variant_id_care_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Care).HasColumnName("care");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("cart_items_cart_id_fkey");

            entity.HasOne(d => d.Variant).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("cart_items_variant_id_fkey");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categories_pkey");

            entity.ToTable("categories");

            entity.HasIndex(e => e.Code, "categories_code_key").IsUnique();

            entity.HasIndex(e => e.Slug, "categories_slug_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.NameEn).HasColumnName("name_en");
            entity.Property(e => e.NameVi).HasColumnName("name_vi");
            entity.Property(e => e.Slug).HasColumnName("slug");
        });

        modelBuilder.Entity<CreditNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("credit_notes_pkey");

            entity.ToTable("credit_notes");

            entity.HasIndex(e => e.CreditNoteNo, "credit_notes_credit_note_no_key").IsUnique();

            entity.HasIndex(e => e.EventKey, "credit_notes_event_key_key").IsUnique();

            entity.HasIndex(e => new { e.Id, e.OrderId }, "credit_notes_id_order_id_key").IsUnique();

            entity.HasIndex(e => new { e.InvoiceId, e.IssuedAt }, "credit_notes_invoice_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ApprovedByUserId).HasColumnName("approved_by_user_id");
            entity.Property(e => e.CreditNoteNo).HasColumnName("credit_note_no");
            entity.Property(e => e.EventKey).HasColumnName("event_key");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.IssuedAt).HasColumnName("issued_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ReturnRequestId).HasColumnName("return_request_id");
            entity.Property(e => e.ShipmentId).HasColumnName("shipment_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalVnd).HasColumnName("total_vnd");
            entity.Property(e => e.WarrantyClaimId).HasColumnName("warranty_claim_id");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.CreditNotes)
                .HasForeignKey(d => d.ApprovedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("credit_notes_approved_by_user_id_fkey");

            entity.HasOne(d => d.WarrantyClaim).WithMany(p => p.CreditNotes)
                .HasForeignKey(d => d.WarrantyClaimId)
                .HasConstraintName("credit_notes_warranty_claim_id_fkey");

            entity.HasOne(d => d.Invoice).WithMany(p => p.CreditNotes)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.InvoiceId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("credit_notes_invoice_id_order_id_fkey");

            entity.HasOne(d => d.ReturnRequest).WithMany(p => p.CreditNotes)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.ReturnRequestId, d.OrderId })
                .HasConstraintName("credit_notes_return_request_id_order_id_fkey");

            entity.HasOne(d => d.Shipment).WithMany(p => p.CreditNotes)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.ShipmentId, d.OrderId })
                .HasConstraintName("credit_notes_shipment_id_order_id_fkey");
        });

        modelBuilder.Entity<CreditNoteItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("credit_note_items_pkey");

            entity.ToTable("credit_note_items");

            entity.HasIndex(e => new { e.Id, e.InvoiceItemId }, "credit_note_items_id_invoice_item_id_key").IsUnique();

            entity.HasIndex(e => e.InvoiceItemId, "credit_note_items_original_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.CreditNoteId).HasColumnName("credit_note_id");
            entity.Property(e => e.InvoiceItemId).HasColumnName("invoice_item_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.CreditNote).WithMany(p => p.CreditNoteItems)
                .HasForeignKey(d => d.CreditNoteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("credit_note_items_credit_note_id_fkey");

            entity.HasOne(d => d.InvoiceItem).WithMany(p => p.CreditNoteItems)
                .HasForeignKey(d => d.InvoiceItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("credit_note_items_invoice_item_id_fkey");
        });

        modelBuilder.Entity<CreditNoteItemUnit>(entity =>
        {
            entity.HasKey(e => new { e.CreditNoteItemId, e.OrderItemUnitAmountId }).HasName("credit_note_item_units_pkey");

            entity.ToTable("credit_note_item_units");

            entity.Property(e => e.CreditNoteItemId).HasColumnName("credit_note_item_id");
            entity.Property(e => e.OrderItemUnitAmountId).HasColumnName("order_item_unit_amount_id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");

            entity.HasOne(d => d.CreditNoteItem).WithMany(p => p.CreditNoteItemUnits)
                .HasForeignKey(d => d.CreditNoteItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("credit_note_item_units_credit_note_item_id_fkey");

            entity.HasOne(d => d.OrderItemUnitAmount).WithMany(p => p.CreditNoteItemUnits)
                .HasForeignKey(d => d.OrderItemUnitAmountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("credit_note_item_units_order_item_unit_amount_id_fkey");
        });

        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("customer_profiles_pkey");

            entity.ToTable("customer_profiles");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.Phone).HasColumnName("phone");

            entity.HasOne(d => d.User).WithOne(p => p.CustomerProfile)
                .HasForeignKey<CustomerProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("customer_profiles_user_id_fkey");
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("goods_receipts_pkey");

            entity.ToTable("goods_receipts");

            entity.HasIndex(e => e.EventKey, "goods_receipts_event_key_key").IsUnique();

            entity.HasIndex(e => new { e.Id, e.PurchaseOrderId }, "goods_receipts_id_purchase_order_id_key").IsUnique();

            entity.HasIndex(e => e.ReceiptNo, "goods_receipts_receipt_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.EventKey).HasColumnName("event_key");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.ReceiptNo).HasColumnName("receipt_no");
            entity.Property(e => e.ReceivedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("received_at");
            entity.Property(e => e.ReceivedBy).HasColumnName("received_by");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.PurchaseOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipts_purchase_order_id_fkey");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.ReceivedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipts_received_by_fkey");
        });

        modelBuilder.Entity<GoodsReceiptItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("goods_receipt_items_pkey");

            entity.ToTable("goods_receipt_items");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.PurchaseOrderItemId).HasColumnName("purchase_order_item_id");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.ReceivedQty).HasColumnName("received_qty");
            entity.Property(e => e.UnitCostVnd).HasColumnName("unit_cost_vnd");

            entity.HasOne(d => d.PurchaseOrderItem).WithMany(p => p.GoodsReceiptItems)
                .HasPrincipalKey(p => new { p.Id, p.PurchaseOrderId })
                .HasForeignKey(d => new { d.PurchaseOrderItemId, d.PurchaseOrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipt_items_purchase_order_item_id_purchase_order__fkey");

            entity.HasOne(d => d.GoodsReceipt).WithMany(p => p.GoodsReceiptItems)
                .HasPrincipalKey(p => new { p.Id, p.PurchaseOrderId })
                .HasForeignKey(d => new { d.ReceiptId, d.PurchaseOrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipt_items_receipt_id_purchase_order_id_fkey");
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(e => new { e.Scope, e.Key }).HasName("idempotency_records_pkey");

            entity.ToTable("idempotency_records");

            entity.Property(e => e.Scope).HasColumnName("scope");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.RequestHash).HasColumnName("request_hash");
            entity.Property(e => e.Result)
                .HasColumnType("jsonb")
                .HasColumnName("result");
        });

        modelBuilder.Entity<InventoryAllocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inventory_allocations_pkey");

            entity.ToTable("inventory_allocations");

            entity.HasIndex(e => e.OrderItemUnitAmountId, "allocation_unit_active_idx")
                .IsUnique()
                .HasFilter("(state = ANY (ARRAY['reserved'::text, 'shipped'::text]))");

            entity.HasIndex(e => e.InventoryUnitId, "inventory_unit_active_idx")
                .IsUnique()
                .HasFilter("((inventory_unit_id IS NOT NULL) AND (state = ANY (ARRAY['reserved'::text, 'shipped'::text])))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CostVnd).HasColumnName("cost_vnd");
            entity.Property(e => e.InventoryUnitId).HasColumnName("inventory_unit_id");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.OrderItemUnitAmountId).HasColumnName("order_item_unit_amount_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.State).HasColumnName("state");

            entity.HasOne(d => d.Lot).WithMany(p => p.InventoryAllocations)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_allocations_lot_id_fkey");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.InventoryAllocations)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_allocations_order_item_id_fkey");

            entity.HasOne(d => d.InventoryUnit).WithMany(p => p.InventoryAllocations)
                .HasPrincipalKey(p => new { p.Id, p.LotId })
                .HasForeignKey(d => new { d.InventoryUnitId, d.LotId })
                .HasConstraintName("inventory_allocations_inventory_unit_id_lot_id_fkey");

            entity.HasOne(d => d.OrderItemUnitAmount).WithMany(p => p.InventoryAllocations)
                .HasPrincipalKey(p => new { p.Id, p.OrderItemId })
                .HasForeignKey(d => new { d.OrderItemUnitAmountId, d.OrderItemId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_allocations_order_item_unit_amount_id_order_item_fkey");
        });

        modelBuilder.Entity<InventoryIdentifier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inventory_identifiers_pkey");

            entity.ToTable("inventory_identifiers");

            entity.HasIndex(e => new { e.Kind, e.NormalizedValue }, "inventory_identifiers_kind_normalized_value_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.InventoryUnitId).HasColumnName("inventory_unit_id");
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.NormalizedValue).HasColumnName("normalized_value");

            entity.HasOne(d => d.InventoryUnit).WithMany(p => p.InventoryIdentifiers)
                .HasForeignKey(d => d.InventoryUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_identifiers_inventory_unit_id_fkey");
        });

        modelBuilder.Entity<InventoryLot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inventory_lots_pkey");

            entity.ToTable("inventory_lots");

            entity.HasIndex(e => new { e.Id, e.VariantId }, "inventory_lots_id_variant_id_key").IsUnique();

            entity.HasIndex(e => e.ReceiptItemId, "inventory_lots_receipt_item_id_key").IsUnique();

            entity.HasIndex(e => new { e.VariantId, e.ReceivedAt, e.Id }, "lots_fifo_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.QtyOnHand).HasColumnName("qty_on_hand");
            entity.Property(e => e.QtyReserved).HasColumnName("qty_reserved");
            entity.Property(e => e.ReceiptItemId).HasColumnName("receipt_item_id");
            entity.Property(e => e.ReceivedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("received_at");
            entity.Property(e => e.UnitCostVnd).HasColumnName("unit_cost_vnd");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.ReceiptItem).WithOne(p => p.InventoryLot)
                .HasForeignKey<InventoryLot>(d => d.ReceiptItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_lots_receipt_item_id_fkey");

            entity.HasOne(d => d.Variant).WithMany(p => p.InventoryLots)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_lots_variant_id_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.InventoryLots)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_lots_warehouse_id_fkey");
        });

        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inventory_reservations_pkey");

            entity.ToTable("inventory_reservations");

            entity.HasIndex(e => new { e.Status, e.ExpiresAt }, "reservations_expiry_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.InventoryReservations)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_reservations_order_item_id_fkey");

            entity.HasOne(d => d.Variant).WithMany(p => p.InventoryReservations)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_reservations_variant_id_fkey");
        });

        modelBuilder.Entity<InventoryUnit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inventory_units_pkey");

            entity.ToTable("inventory_units");

            entity.HasIndex(e => new { e.Id, e.LotId }, "inventory_units_id_lot_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ConditionStatus)
                .HasDefaultValueSql("'new'::text")
                .HasColumnName("condition_status");
            entity.Property(e => e.CustodyStatus)
                .HasDefaultValueSql("'available'::text")
                .HasColumnName("custody_status");
            entity.Property(e => e.LotId).HasColumnName("lot_id");

            entity.HasOne(d => d.Lot).WithMany(p => p.InventoryUnits)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_units_lot_id_fkey");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoices_pkey");

            entity.ToTable("invoices");

            entity.HasIndex(e => new { e.CustomerId, e.IssuedAt, e.Id }, "invoices_customer_idx");

            entity.HasIndex(e => new { e.Id, e.OrderId }, "invoices_id_order_id_key").IsUnique();

            entity.HasIndex(e => e.InvoiceNo, "invoices_invoice_no_key").IsUnique();

            entity.HasIndex(e => new { e.IssuedAt, e.Id }, "invoices_issued_idx").HasFilter("(lifecycle = 'issued'::text)");

            entity.HasIndex(e => e.OrderId, "invoices_order_id_key").IsUnique();

            entity.HasIndex(e => new { e.ResponsibleStaffId, e.IssuedAt, e.Id }, "invoices_staff_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Currency)
                .HasDefaultValueSql("'VND'::text")
                .HasColumnName("currency");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerSnapshot)
                .HasColumnType("jsonb")
                .HasColumnName("customer_snapshot");
            entity.Property(e => e.DiscountTotalVnd).HasColumnName("discount_total_vnd");
            entity.Property(e => e.GrandTotalVnd).HasColumnName("grand_total_vnd");
            entity.Property(e => e.InvoiceNo).HasColumnName("invoice_no");
            entity.Property(e => e.IssuedAt).HasColumnName("issued_at");
            entity.Property(e => e.IssuedByUserId).HasColumnName("issued_by_user_id");
            entity.Property(e => e.Lifecycle).HasColumnName("lifecycle");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ResponsibleStaffId).HasColumnName("responsible_staff_id");
            entity.Property(e => e.StaffSnapshot)
                .HasColumnType("jsonb")
                .HasColumnName("staff_snapshot");
            entity.Property(e => e.SubtotalVnd).HasColumnName("subtotal_vnd");

            entity.HasOne(d => d.Customer).WithMany(p => p.InvoiceCustomers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invoices_customer_id_fkey");

            entity.HasOne(d => d.IssuedByUser).WithMany(p => p.InvoiceIssuedByUsers)
                .HasForeignKey(d => d.IssuedByUserId)
                .HasConstraintName("invoices_issued_by_user_id_fkey");

            entity.HasOne(d => d.Order).WithOne(p => p.InvoiceOrder)
                .HasForeignKey<Invoice>(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invoices_order_id_fkey");

            entity.HasOne(d => d.ResponsibleStaff).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.ResponsibleStaffId)
                .HasConstraintName("invoices_responsible_staff_id_fkey");

            entity.HasOne(d => d.OrderNavigation).WithMany(p => p.InvoiceOrderNavigations)
                .HasPrincipalKey(p => new { p.Id, p.CustomerId })
                .HasForeignKey(d => new { d.OrderId, d.CustomerId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invoices_order_id_customer_id_fkey");
        });

        modelBuilder.Entity<InvoiceExport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoice_exports_pkey");

            entity.ToTable("invoice_exports");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.FileManifest)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("file_manifest");
            entity.Property(e => e.QueryJson)
                .HasColumnType("jsonb")
                .HasColumnName("query_json");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.RequestedByUserId).HasColumnName("requested_by_user_id");
            entity.Property(e => e.RoleSnapshot).HasColumnName("role_snapshot");
            entity.Property(e => e.RowCount).HasColumnName("row_count");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalVnd).HasColumnName("total_vnd");

            entity.HasOne(d => d.RequestedByUser).WithMany(p => p.InvoiceExports)
                .HasForeignKey(d => d.RequestedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invoice_exports_requested_by_user_id_fkey");
        });

        modelBuilder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoice_items_pkey");

            entity.ToTable("invoice_items");

            entity.HasIndex(e => new { e.Id, e.InvoiceId }, "invoice_items_id_invoice_id_key").IsUnique();

            entity.HasIndex(e => new { e.InvoiceId, e.LineNo }, "invoice_items_invoice_id_line_no_key").IsUnique();

            entity.HasIndex(e => e.InvoiceId, "invoice_items_invoice_idx");

            entity.HasIndex(e => e.OrderItemId, "invoice_items_order_item_id_key").IsUnique();

            entity.HasIndex(e => new { e.ProductId, e.InvoiceId }, "invoice_items_product_idx");

            entity.HasIndex(e => new { e.VariantId, e.InvoiceId }, "invoice_items_variant_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.LineNo).HasColumnName("line_no");
            entity.Property(e => e.LineType).HasColumnName("line_type");
            entity.Property(e => e.NameSnapshot).HasColumnName("name_snapshot");
            entity.Property(e => e.NetAmountVnd).HasColumnName("net_amount_vnd");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.ParentInvoiceItemId).HasColumnName("parent_invoice_item_id");
            entity.Property(e => e.PointsDiscountVnd).HasColumnName("points_discount_vnd");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SkuSnapshot).HasColumnName("sku_snapshot");
            entity.Property(e => e.UnitPriceVnd).HasColumnName("unit_price_vnd");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.VariantSnapshot)
                .HasColumnType("jsonb")
                .HasColumnName("variant_snapshot");
            entity.Property(e => e.VoucherDiscountVnd).HasColumnName("voucher_discount_vnd");

            entity.HasOne(d => d.Product).WithMany(p => p.InvoiceItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("invoice_items_product_id_fkey");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceItems)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.InvoiceId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invoice_items_invoice_id_order_id_fkey");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.InvoiceItems)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.OrderItemId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("invoice_items_order_item_id_order_id_fkey");

            entity.HasOne(d => d.InvoiceItemNavigation).WithMany(p => p.InverseInvoiceItemNavigation)
                .HasPrincipalKey(p => new { p.Id, p.InvoiceId })
                .HasForeignKey(d => new { d.ParentInvoiceItemId, d.InvoiceId })
                .HasConstraintName("invoice_items_parent_invoice_item_id_invoice_id_fkey");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.InvoiceItems)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.VariantId, d.ProductId })
                .HasConstraintName("invoice_items_variant_id_product_id_fkey");
        });

        modelBuilder.Entity<LandingDraft>(entity =>
        {
            entity.HasKey(e => e.PageId).HasName("landing_drafts_pkey");

            entity.ToTable("landing_drafts");

            entity.Property(e => e.PageId)
                .ValueGeneratedNever()
                .HasColumnName("page_id");
            entity.Property(e => e.Revision).HasColumnName("revision");
            entity.Property(e => e.Sections)
                .HasColumnType("jsonb")
                .HasColumnName("sections");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Page).WithOne(p => p.LandingDraft)
                .HasForeignKey<LandingDraft>(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("landing_drafts_page_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.LandingDrafts)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("landing_drafts_updated_by_fkey");
        });

        modelBuilder.Entity<LandingPage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("landing_pages_pkey");

            entity.ToTable("landing_pages");

            entity.HasIndex(e => e.Slug, "landing_pages_slug_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Slug).HasColumnName("slug");
        });

        modelBuilder.Entity<LandingPublication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("landing_publications_pkey");

            entity.ToTable("landing_publications");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.PageId).HasColumnName("page_id");
            entity.Property(e => e.PublishedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("published_at");
            entity.Property(e => e.PublishedBy).HasColumnName("published_by");
            entity.Property(e => e.VersionId).HasColumnName("version_id");

            entity.HasOne(d => d.PublishedByNavigation).WithMany(p => p.LandingPublications)
                .HasForeignKey(d => d.PublishedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("landing_publications_published_by_fkey");

            entity.HasOne(d => d.LandingVersion).WithMany(p => p.LandingPublications)
                .HasPrincipalKey(p => new { p.Id, p.PageId })
                .HasForeignKey(d => new { d.VersionId, d.PageId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("landing_publications_version_id_page_id_fkey");
        });

        modelBuilder.Entity<LandingVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("landing_versions_pkey");

            entity.ToTable("landing_versions");

            entity.HasIndex(e => new { e.Id, e.PageId }, "landing_versions_id_page_id_key").IsUnique();

            entity.HasIndex(e => new { e.PageId, e.VersionNo }, "landing_versions_page_id_version_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.PageId).HasColumnName("page_id");
            entity.Property(e => e.Sections)
                .HasColumnType("jsonb")
                .HasColumnName("sections");
            entity.Property(e => e.VersionNo).HasColumnName("version_no");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.LandingVersions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("landing_versions_created_by_fkey");

            entity.HasOne(d => d.Page).WithMany(p => p.LandingVersions)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("landing_versions_page_id_fkey");

            entity.HasMany(d => d.Media).WithMany(p => p.LandingVersions)
                .UsingEntity<Dictionary<string, object>>(
                    "LandingMediaRef",
                    r => r.HasOne<MediaAsset>().WithMany()
                        .HasForeignKey("MediaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("landing_media_refs_media_id_fkey"),
                    l => l.HasOne<LandingVersion>().WithMany()
                        .HasForeignKey("LandingVersionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("landing_media_refs_landing_version_id_fkey"),
                    j =>
                    {
                        j.HasKey("LandingVersionId", "MediaId").HasName("landing_media_refs_pkey");
                        j.ToTable("landing_media_refs");
                        j.IndexerProperty<Guid>("LandingVersionId").HasColumnName("landing_version_id");
                        j.IndexerProperty<Guid>("MediaId").HasColumnName("media_id");
                    });
        });

        modelBuilder.Entity<LoyaltyAccount>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("loyalty_accounts_pkey");

            entity.ToTable("loyalty_accounts");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BalancePoints).HasColumnName("balance_points");
            entity.Property(e => e.NetSpendVnd).HasColumnName("net_spend_vnd");

            entity.HasOne(d => d.User).WithOne(p => p.LoyaltyAccount)
                .HasForeignKey<LoyaltyAccount>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("loyalty_accounts_user_id_fkey");
        });

        modelBuilder.Entity<LoyaltyEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("loyalty_entries_pkey");

            entity.ToTable("loyalty_entries");

            entity.HasIndex(e => e.EventKey, "loyalty_entries_event_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Delta).HasColumnName("delta");
            entity.Property(e => e.EventKey).HasColumnName("event_key");
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.LoyaltyEntries)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("loyalty_entries_order_id_fkey");

            entity.HasOne(d => d.Unit).WithMany(p => p.LoyaltyEntries)
                .HasForeignKey(d => d.UnitId)
                .HasConstraintName("loyalty_entries_unit_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.LoyaltyEntries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("loyalty_entries_user_id_fkey");
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("media_assets_pkey");

            entity.ToTable("media_assets");

            entity.HasIndex(e => e.StorageKey, "media_assets_storage_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Checksum).HasColumnName("checksum");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LicenseNote).HasColumnName("license_note");
            entity.Property(e => e.Metadata)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.Mime).HasColumnName("mime");
            entity.Property(e => e.OwnerUserId).HasColumnName("owner_user_id");
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.SourceUrl).HasColumnName("source_url");
            entity.Property(e => e.StorageKey).HasColumnName("storage_key");
            entity.Property(e => e.ValidationStatus).HasColumnName("validation_status");
            entity.Property(e => e.Visibility).HasColumnName("visibility");

            entity.HasOne(d => d.OwnerUser).WithMany(p => p.MediaAssets)
                .HasForeignKey(d => d.OwnerUserId)
                .HasConstraintName("media_assets_owner_user_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.RecipientUserId).HasColumnName("recipient_user_id");
            entity.Property(e => e.Type).HasColumnName("type");

            entity.HasOne(d => d.RecipientUser).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.RecipientUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("notifications_recipient_user_id_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.HasIndex(e => new { e.CustomerId, e.CreatedAt }, "orders_customer_idx");

            entity.HasIndex(e => new { e.Id, e.CustomerId }, "orders_id_customer_id_key").IsUnique();

            entity.HasIndex(e => e.OrderNo, "orders_order_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AddressSnapshot)
                .HasColumnType("jsonb")
                .HasColumnName("address_snapshot");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasDefaultValueSql("'VND'::text")
                .HasColumnName("currency");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(e => e.ExchangeRateVnd)
                .HasDefaultValue(25000m)
                .HasColumnName("exchange_rate_vnd");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.OrderNo).HasColumnName("order_no");
            entity.Property(e => e.OrderStatus).HasColumnName("order_status");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
            entity.Property(e => e.PolicySnapshot)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("policy_snapshot");
            entity.Property(e => e.ResponsibleStaffId).HasColumnName("responsible_staff_id");
            entity.Property(e => e.TotalVnd).HasColumnName("total_vnd");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("orders_customer_id_fkey");

            entity.HasOne(d => d.ResponsibleStaff).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ResponsibleStaffId)
                .HasConstraintName("orders_responsible_staff_id_fkey");
        });

        modelBuilder.Entity<OrderAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_adjustments_pkey");

            entity.ToTable("order_adjustments");

            entity.HasIndex(e => e.SourceEventKey, "order_adjustments_source_event_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.PromotionRedemptionId).HasColumnName("promotion_redemption_id");
            entity.Property(e => e.SourceEventKey).HasColumnName("source_event_key");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderAdjustments)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_adjustments_order_item_id_fkey");

            entity.HasOne(d => d.PromotionRedemption).WithMany(p => p.OrderAdjustments)
                .HasForeignKey(d => d.PromotionRedemptionId)
                .HasConstraintName("order_adjustments_promotion_redemption_id_fkey");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_items_pkey");

            entity.ToTable("order_items");

            entity.HasIndex(e => new { e.Id, e.OrderId }, "order_items_id_order_id_key").IsUnique();

            entity.HasIndex(e => new { e.OrderId, e.LineNo }, "order_items_order_id_line_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.LineNo).HasColumnName("line_no");
            entity.Property(e => e.LineType).HasColumnName("line_type");
            entity.Property(e => e.NetAmountVnd).HasColumnName("net_amount_vnd");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ParentItemId).HasColumnName("parent_item_id");
            entity.Property(e => e.PointsDiscountVnd).HasColumnName("points_discount_vnd");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Snapshot)
                .HasColumnType("jsonb")
                .HasColumnName("snapshot");
            entity.Property(e => e.UnitPriceVnd).HasColumnName("unit_price_vnd");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.VoucherDiscountVnd).HasColumnName("voucher_discount_vnd");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_items_order_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("order_items_product_id_fkey");

            entity.HasOne(d => d.OrderItemNavigation).WithMany(p => p.InverseOrderItemNavigation)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.ParentItemId, d.OrderId })
                .HasConstraintName("order_items_parent_item_id_order_id_fkey");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.OrderItems)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.VariantId, d.ProductId })
                .HasConstraintName("order_items_variant_id_product_id_fkey");
        });

        modelBuilder.Entity<OrderItemUnitAmount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_item_unit_amounts_pkey");

            entity.ToTable("order_item_unit_amounts");

            entity.HasIndex(e => new { e.Id, e.OrderItemId }, "order_item_unit_amounts_id_order_item_id_key").IsUnique();

            entity.HasIndex(e => new { e.OrderItemId, e.UnitOrdinal }, "order_item_unit_amounts_order_item_id_unit_ordinal_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CashPlannedVnd).HasColumnName("cash_planned_vnd");
            entity.Property(e => e.CreditPlannedVnd).HasColumnName("credit_planned_vnd");
            entity.Property(e => e.GrossVnd).HasColumnName("gross_vnd");
            entity.Property(e => e.NetAmountVnd).HasColumnName("net_amount_vnd");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.ParentUnitAmountId).HasColumnName("parent_unit_amount_id");
            entity.Property(e => e.PointsDiscountVnd).HasColumnName("points_discount_vnd");
            entity.Property(e => e.RedeemedPoints).HasColumnName("redeemed_points");
            entity.Property(e => e.UnitOrdinal).HasColumnName("unit_ordinal");
            entity.Property(e => e.VoucherDiscountVnd).HasColumnName("voucher_discount_vnd");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemUnitAmounts)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_item_unit_amounts_order_item_id_fkey");

            entity.HasOne(d => d.ParentUnitAmount).WithMany(p => p.InverseParentUnitAmount)
                .HasForeignKey(d => d.ParentUnitAmountId)
                .HasConstraintName("order_item_unit_amounts_parent_unit_amount_id_fkey");
        });

        modelBuilder.Entity<OrderTenderAllocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("order_tender_allocations_pkey");

            entity.ToTable("order_tender_allocations");

            entity.HasIndex(e => new { e.ReceiptId, e.OrderItemUnitAmountId }, "order_tender_allocations_receipt_id_order_item_unit_amount__key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.OrderItemUnitAmountId).HasColumnName("order_item_unit_amount_id");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");

            entity.HasOne(d => d.OrderItemUnitAmount).WithMany(p => p.OrderTenderAllocations)
                .HasForeignKey(d => d.OrderItemUnitAmountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_tender_allocations_order_item_unit_amount_id_fkey");

            entity.HasOne(d => d.Receipt).WithMany(p => p.OrderTenderAllocations)
                .HasForeignKey(d => d.ReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("order_tender_allocations_receipt_id_fkey");
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("outbox_events_pkey");

            entity.ToTable("outbox_events");

            entity.HasIndex(e => e.EventKey, "outbox_events_event_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Attempts).HasColumnName("attempts");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EventKey).HasColumnName("event_key");
            entity.Property(e => e.NextAttemptAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("next_attempt_at");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'pending'::text")
                .HasColumnName("status");
            entity.Property(e => e.Type).HasColumnName("type");
        });

        modelBuilder.Entity<PaymentAttempt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payment_attempts_pkey");

            entity.ToTable("payment_attempts");

            entity.HasIndex(e => new { e.Id, e.OrderId }, "payment_attempts_id_order_id_key").IsUnique();

            entity.HasIndex(e => e.RequestKey, "payment_attempts_request_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Method).HasColumnName("method");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.RequestKey).HasColumnName("request_key");
            entity.Property(e => e.RequestedAmountVnd).HasColumnName("requested_amount_vnd");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Order).WithMany(p => p.PaymentAttempts)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payment_attempts_order_id_fkey");
        });

        modelBuilder.Entity<PaymentEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payment_events_pkey");

            entity.ToTable("payment_events");

            entity.HasIndex(e => e.EventKey, "payment_events_event_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EventKey).HasColumnName("event_key");
            entity.Property(e => e.EventType).HasColumnName("event_type");
            entity.Property(e => e.PayloadDigest).HasColumnName("payload_digest");
            entity.Property(e => e.PaymentAttemptId).HasColumnName("payment_attempt_id");

            entity.HasOne(d => d.PaymentAttempt).WithMany(p => p.PaymentEvents)
                .HasForeignKey(d => d.PaymentAttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payment_events_payment_attempt_id_fkey");
        });

        modelBuilder.Entity<PaymentReceipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("payment_receipts_pkey");

            entity.ToTable("payment_receipts");

            entity.HasIndex(e => new { e.Id, e.OrderId }, "payment_receipts_id_order_id_key").IsUnique();

            entity.HasIndex(e => e.SourceEventKey, "payment_receipts_source_event_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentAttemptId).HasColumnName("payment_attempt_id");
            entity.Property(e => e.ReceivedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("received_at");
            entity.Property(e => e.SourceEventKey).HasColumnName("source_event_key");
            entity.Property(e => e.TenderKind).HasColumnName("tender_kind");

            entity.HasOne(d => d.Order).WithMany(p => p.PaymentReceipts)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payment_receipts_order_id_fkey");

            entity.HasOne(d => d.PaymentAttempt).WithMany(p => p.PaymentReceipts)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.PaymentAttemptId, d.OrderId })
                .HasConstraintName("payment_receipts_payment_attempt_id_order_id_fkey");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("products_pkey");

            entity.ToTable("products");

            entity.HasIndex(e => e.ModelCode, "products_model_code_key").IsUnique();

            entity.HasIndex(e => e.Slug, "products_slug_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BusinessStatus)
                .HasDefaultValueSql("'active'::text")
                .HasColumnName("business_status");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Generation).HasColumnName("generation");
            entity.Property(e => e.ModelCode).HasColumnName("model_code");
            entity.Property(e => e.Poster)
                .HasDefaultValueSql("'/media/phone-burgundy.svg'::text")
                .HasColumnName("poster");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.SourceUrl).HasColumnName("source_url");
            entity.Property(e => e.Specs)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("specs");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("products_category_id_fkey");

            entity.HasMany(d => d.AccessoryProducts).WithMany(p => p.TargetProducts)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductCompatibility",
                    r => r.HasOne<Product>().WithMany()
                        .HasForeignKey("AccessoryProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("product_compatibilities_accessory_product_id_fkey"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("TargetProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("product_compatibilities_target_product_id_fkey"),
                    j =>
                    {
                        j.HasKey("AccessoryProductId", "TargetProductId").HasName("product_compatibilities_pkey");
                        j.ToTable("product_compatibilities");
                        j.IndexerProperty<Guid>("AccessoryProductId").HasColumnName("accessory_product_id");
                        j.IndexerProperty<Guid>("TargetProductId").HasColumnName("target_product_id");
                    });

            entity.HasMany(d => d.TargetProducts).WithMany(p => p.AccessoryProducts)
                .UsingEntity<Dictionary<string, object>>(
                    "ProductCompatibility",
                    r => r.HasOne<Product>().WithMany()
                        .HasForeignKey("TargetProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("product_compatibilities_target_product_id_fkey"),
                    l => l.HasOne<Product>().WithMany()
                        .HasForeignKey("AccessoryProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("product_compatibilities_accessory_product_id_fkey"),
                    j =>
                    {
                        j.HasKey("AccessoryProductId", "TargetProductId").HasName("product_compatibilities_pkey");
                        j.ToTable("product_compatibilities");
                        j.IndexerProperty<Guid>("AccessoryProductId").HasColumnName("accessory_product_id");
                        j.IndexerProperty<Guid>("TargetProductId").HasColumnName("target_product_id");
                    });
        });

        modelBuilder.Entity<ProductAttributeValue>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.AttributeId }).HasName("product_attribute_values_pkey");

            entity.ToTable("product_attribute_values");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.AttributeId).HasColumnName("attribute_id");
            entity.Property(e => e.ValueBoolean).HasColumnName("value_boolean");
            entity.Property(e => e.ValueNumber).HasColumnName("value_number");
            entity.Property(e => e.ValueText).HasColumnName("value_text");

            entity.HasOne(d => d.Attribute).WithMany(p => p.ProductAttributeValues)
                .HasForeignKey(d => d.AttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_attribute_values_attribute_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductAttributeValues)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_attribute_values_product_id_fkey");
        });

        modelBuilder.Entity<ProductMedium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_media_pkey");

            entity.ToTable("product_media");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.MediaId).HasColumnName("media_id");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.Media).WithMany(p => p.ProductMedia)
                .HasForeignKey(d => d.MediaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_media_media_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductMedia)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_media_product_id_fkey");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.ProductMedia)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.VariantId, d.ProductId })
                .HasConstraintName("product_media_variant_id_product_id_fkey");
        });

        modelBuilder.Entity<ProductTranslation>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.Locale }).HasName("product_translations_pkey");

            entity.ToTable("product_translations");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Locale).HasColumnName("locale");
            entity.Property(e => e.ContentStatus)
                .HasDefaultValueSql("'published'::text")
                .HasColumnName("content_status");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.SeoTitle).HasColumnName("seo_title");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductTranslations)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_translations_product_id_fkey");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_variants_pkey");

            entity.ToTable("product_variants");

            entity.HasIndex(e => new { e.Id, e.ProductId }, "product_variants_id_product_id_key").IsUnique();

            entity.HasIndex(e => new { e.ProductId, e.OptionSignature }, "product_variants_product_id_option_signature_key").IsUnique();

            entity.HasIndex(e => e.Sku, "product_variants_sku_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CareFeeVnd).HasColumnName("care_fee_vnd");
            entity.Property(e => e.OptionSignature).HasColumnName("option_signature");
            entity.Property(e => e.Options)
                .HasDefaultValueSql("'{}'::jsonb")
                .HasColumnType("jsonb")
                .HasColumnName("options");
            entity.Property(e => e.PriceVnd).HasColumnName("price_vnd");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Sku).HasColumnName("sku");
            entity.Property(e => e.StandardWarrantyMonths)
                .HasDefaultValue(12)
                .HasColumnName("standard_warranty_months");
            entity.Property(e => e.TrackingMode).HasColumnName("tracking_mode");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("product_variants_product_id_fkey");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("promotions_pkey");

            entity.ToTable("promotions");

            entity.HasIndex(e => e.CodeNormalized, "promotions_code_normalized_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.CodeNormalized).HasColumnName("code_normalized");
            entity.Property(e => e.MaxUses).HasColumnName("max_uses");
            entity.Property(e => e.MaxUsesPerCustomer).HasColumnName("max_uses_per_customer");
        });

        modelBuilder.Entity<PromotionRedemption>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("promotion_redemptions_pkey");

            entity.ToTable("promotion_redemptions");

            entity.HasIndex(e => e.IdempotencyKey, "promotion_redemptions_idempotency_key_key").IsUnique();

            entity.HasIndex(e => new { e.OrderId, e.PromotionId }, "promotion_redemptions_order_id_promotion_id_key").IsUnique();

            entity.HasIndex(e => new { e.PromotionId, e.Status, e.CustomerId }, "redemptions_quota_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.PromotionVersionId).HasColumnName("promotion_version_id");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Customer).WithMany(p => p.PromotionRedemptions)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("promotion_redemptions_customer_id_fkey");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionRedemptions)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("promotion_redemptions_promotion_id_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.PromotionRedemptions)
                .HasPrincipalKey(p => new { p.Id, p.CustomerId })
                .HasForeignKey(d => new { d.OrderId, d.CustomerId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("promotion_redemptions_order_id_customer_id_fkey");

            entity.HasOne(d => d.PromotionVersion).WithMany(p => p.PromotionRedemptions)
                .HasPrincipalKey(p => new { p.Id, p.PromotionId })
                .HasForeignKey(d => new { d.PromotionVersionId, d.PromotionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("promotion_redemptions_promotion_version_id_promotion_id_fkey");
        });

        modelBuilder.Entity<PromotionTarget>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("promotion_targets_pkey");

            entity.ToTable("promotion_targets");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.PromotionVersionId).HasColumnName("promotion_version_id");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.Category).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("promotion_targets_category_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("promotion_targets_product_id_fkey");

            entity.HasOne(d => d.PromotionVersion).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.PromotionVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("promotion_targets_promotion_version_id_fkey");

            entity.HasOne(d => d.Variant).WithMany(p => p.PromotionTargets)
                .HasForeignKey(d => d.VariantId)
                .HasConstraintName("promotion_targets_variant_id_fkey");
        });

        modelBuilder.Entity<PromotionVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("promotion_versions_pkey");

            entity.ToTable("promotion_versions");

            entity.HasIndex(e => new { e.Id, e.PromotionId }, "promotion_versions_id_promotion_id_key").IsUnique();

            entity.HasIndex(e => new { e.PromotionId, e.VersionNo }, "promotion_versions_promotion_id_version_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AllProducts)
                .HasDefaultValue(true)
                .HasColumnName("all_products");
            entity.Property(e => e.DiscountType).HasColumnName("discount_type");
            entity.Property(e => e.EndsAt).HasColumnName("ends_at");
            entity.Property(e => e.MaxDiscountVnd).HasColumnName("max_discount_vnd");
            entity.Property(e => e.MinPurchaseVnd).HasColumnName("min_purchase_vnd");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.StartsAt).HasColumnName("starts_at");
            entity.Property(e => e.Value).HasColumnName("value");
            entity.Property(e => e.VersionNo).HasColumnName("version_no");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionVersions)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("promotion_versions_promotion_id_fkey");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("purchase_orders_pkey");

            entity.ToTable("purchase_orders");

            entity.HasIndex(e => e.Code, "purchase_orders_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.PurchaseOrderApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("purchase_orders_approved_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PurchaseOrderCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_orders_created_by_fkey");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_orders_supplier_id_fkey");
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("purchase_order_items_pkey");

            entity.ToTable("purchase_order_items");

            entity.HasIndex(e => new { e.Id, e.PurchaseOrderId }, "purchase_order_items_id_purchase_order_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitCostVnd).HasColumnName("unit_cost_vnd");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(d => d.PurchaseOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_order_items_purchase_order_id_fkey");

            entity.HasOne(d => d.Variant).WithMany(p => p.PurchaseOrderItems)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_order_items_variant_id_fkey");
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refunds_pkey");

            entity.ToTable("refunds");

            entity.HasIndex(e => e.IdempotencyKey, "refunds_idempotency_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreditNoteId).HasColumnName("credit_note_id");
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("refunds_approved_by_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("refunds_order_id_fkey");

            entity.HasOne(d => d.CreditNote).WithMany(p => p.Refunds)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.CreditNoteId, d.OrderId })
                .HasConstraintName("refunds_credit_note_id_order_id_fkey");
        });

        modelBuilder.Entity<RefundAllocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refund_allocations_pkey");

            entity.ToTable("refund_allocations");

            entity.HasIndex(e => new { e.RefundId, e.PaymentReceiptId, e.OrderItemUnitAmountId }, "refund_allocations_refund_id_payment_receipt_id_order_item__key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.OrderItemUnitAmountId).HasColumnName("order_item_unit_amount_id");
            entity.Property(e => e.PaymentReceiptId).HasColumnName("payment_receipt_id");
            entity.Property(e => e.RefundId).HasColumnName("refund_id");

            entity.HasOne(d => d.OrderItemUnitAmount).WithMany(p => p.RefundAllocations)
                .HasForeignKey(d => d.OrderItemUnitAmountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("refund_allocations_order_item_unit_amount_id_fkey");

            entity.HasOne(d => d.PaymentReceipt).WithMany(p => p.RefundAllocations)
                .HasForeignKey(d => d.PaymentReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("refund_allocations_payment_receipt_id_fkey");

            entity.HasOne(d => d.Refund).WithMany(p => p.RefundAllocations)
                .HasForeignKey(d => d.RefundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("refund_allocations_refund_id_fkey");
        });

        modelBuilder.Entity<ReturnItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("return_items_pkey");

            entity.ToTable("return_items");

            entity.HasIndex(e => new { e.Id, e.OrderItemId }, "return_items_id_order_item_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActualReceivedIdentifier).HasColumnName("actual_received_identifier");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.ReturnRequestId).HasColumnName("return_request_id");

            entity.HasOne(d => d.OrderItem).WithMany(p => p.ReturnItems)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.OrderItemId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("return_items_order_item_id_order_id_fkey");

            entity.HasOne(d => d.ReturnRequest).WithMany(p => p.ReturnItems)
                .HasPrincipalKey(p => new { p.Id, p.OrderId })
                .HasForeignKey(d => new { d.ReturnRequestId, d.OrderId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("return_items_return_request_id_order_id_fkey");
        });

        modelBuilder.Entity<ReturnItemUnit>(entity =>
        {
            entity.HasKey(e => new { e.ReturnItemId, e.OrderItemUnitAmountId }).HasName("return_item_units_pkey");

            entity.ToTable("return_item_units");

            entity.Property(e => e.ReturnItemId).HasColumnName("return_item_id");
            entity.Property(e => e.OrderItemUnitAmountId).HasColumnName("order_item_unit_amount_id");
            entity.Property(e => e.InventoryUnitId).HasColumnName("inventory_unit_id");
            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");

            entity.HasOne(d => d.InventoryUnit).WithMany(p => p.ReturnItemUnits)
                .HasForeignKey(d => d.InventoryUnitId)
                .HasConstraintName("return_item_units_inventory_unit_id_fkey");

            entity.HasOne(d => d.OrderItemUnitAmount).WithMany(p => p.ReturnItemUnits)
                .HasPrincipalKey(p => new { p.Id, p.OrderItemId })
                .HasForeignKey(d => new { d.OrderItemUnitAmountId, d.OrderItemId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("return_item_units_order_item_unit_amount_id_order_item_id_fkey");

            entity.HasOne(d => d.ReturnItem).WithMany(p => p.ReturnItemUnits)
                .HasPrincipalKey(p => new { p.Id, p.OrderItemId })
                .HasForeignKey(d => new { d.ReturnItemId, d.OrderItemId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("return_item_units_return_item_id_order_item_id_fkey");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("return_requests_pkey");

            entity.ToTable("return_requests");

            entity.HasIndex(e => new { e.Id, e.OrderId }, "return_requests_id_order_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.FeeVnd)
                .HasDefaultValue(30000L)
                .HasColumnName("fee_vnd");
            entity.Property(e => e.InspectedAt).HasColumnName("inspected_at");
            entity.Property(e => e.InspectedBy).HasColumnName("inspected_by");
            entity.Property(e => e.InspectionResult).HasColumnName("inspection_result");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.State).HasColumnName("state");

            entity.HasOne(d => d.InspectedByNavigation).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.InspectedBy)
                .HasConstraintName("return_requests_inspected_by_fkey");

            entity.HasOne(d => d.Order).WithMany(p => p.ReturnRequests)
                .HasPrincipalKey(p => new { p.Id, p.CustomerId })
                .HasForeignKey(d => new { d.OrderId, d.CustomerId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("return_requests_order_id_customer_id_fkey");

            entity.HasMany(d => d.Media).WithMany(p => p.ReturnRequests)
                .UsingEntity<Dictionary<string, object>>(
                    "ReturnEvidence",
                    r => r.HasOne<MediaAsset>().WithMany()
                        .HasForeignKey("MediaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("return_evidence_media_id_fkey"),
                    l => l.HasOne<ReturnRequest>().WithMany()
                        .HasForeignKey("ReturnRequestId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("return_evidence_return_request_id_fkey"),
                    j =>
                    {
                        j.HasKey("ReturnRequestId", "MediaId").HasName("return_evidence_pkey");
                        j.ToTable("return_evidence");
                        j.IndexerProperty<Guid>("ReturnRequestId").HasColumnName("return_request_id");
                        j.IndexerProperty<Guid>("MediaId").HasColumnName("media_id");
                    });
        });

        modelBuilder.Entity<SchemaMigration>(entity =>
        {
            entity.HasKey(e => e.Name).HasName("schema_migrations_pkey");

            entity.ToTable("schema_migrations");

            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.AppliedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("applied_at");
            entity.Property(e => e.Checksum).HasColumnName("checksum");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("session_pkey");

            entity.ToTable("session");

            entity.HasIndex(e => e.Token, "session_token_key").IsUnique();

            entity.HasIndex(e => e.UserId, "session_user_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("session_user_id_fkey");
        });

        modelBuilder.Entity<SettingVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("setting_versions_pkey");

            entity.ToTable("setting_versions");

            entity.HasIndex(e => new { e.Key, e.VersionNo }, "setting_versions_key_version_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.Value)
                .HasColumnType("jsonb")
                .HasColumnName("value");
            entity.Property(e => e.VersionNo).HasColumnName("version_no");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.SettingVersions)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("setting_versions_changed_by_fkey");

            entity.HasOne(d => d.KeyNavigation).WithMany(p => p.SettingVersions)
                .HasForeignKey(d => d.Key)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("setting_versions_key_fkey");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shipments_pkey");

            entity.ToTable("shipments");

            entity.HasIndex(e => new { e.Id, e.OrderId }, "shipments_id_order_id_key").IsUnique();

            entity.HasIndex(e => e.TrackingNo, "shipments_tracking_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.RetryOf).HasColumnName("retry_of");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TrackingNo).HasColumnName("tracking_no");

            entity.HasOne(d => d.Order).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("shipments_order_id_fkey");

            entity.HasOne(d => d.RetryOfNavigation).WithMany(p => p.InverseRetryOfNavigation)
                .HasForeignKey(d => d.RetryOf)
                .HasConstraintName("shipments_retry_of_fkey");
        });

        modelBuilder.Entity<ShipmentEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("shipment_events_pkey");

            entity.ToTable("shipment_events");

            entity.HasIndex(e => e.EventKey, "shipment_events_event_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.EventKey).HasColumnName("event_key");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("occurred_at");
            entity.Property(e => e.ShipmentId).HasColumnName("shipment_id");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.Shipment).WithMany(p => p.ShipmentEvents)
                .HasForeignKey(d => d.ShipmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("shipment_events_shipment_id_fkey");
        });

        modelBuilder.Entity<StaffProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("staff_profiles_pkey");

            entity.ToTable("staff_profiles");

            entity.HasIndex(e => e.StaffCode, "staff_profiles_staff_code_key").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.StaffCode).HasColumnName("staff_code");

            entity.HasOne(d => d.User).WithOne(p => p.StaffProfile)
                .HasForeignKey<StaffProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("staff_profiles_user_id_fkey");
        });

        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_adjustments_pkey");

            entity.ToTable("stock_adjustments");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.QuantityDelta).HasColumnName("quantity_delta");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.StockAdjustmentApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("stock_adjustments_approved_by_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.StockAdjustments)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_adjustments_lot_id_fkey");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.StockAdjustmentRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_adjustments_requested_by_fkey");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_movements_pkey");

            entity.ToTable("stock_movements");

            entity.HasIndex(e => e.SourceEventKey, "stock_movements_source_event_key_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.QuantityDelta).HasColumnName("quantity_delta");
            entity.Property(e => e.Reason)
                .HasDefaultValueSql("''::text")
                .HasColumnName("reason");
            entity.Property(e => e.SourceEventKey).HasColumnName("source_event_key");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ActorUserId)
                .HasConstraintName("stock_movements_actor_user_id_fkey");

            entity.HasOne(d => d.Variant).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_movements_variant_id_fkey");

            entity.HasOne(d => d.InventoryLot).WithMany(p => p.StockMovements)
                .HasPrincipalKey(p => new { p.Id, p.VariantId })
                .HasForeignKey(d => new { d.LotId, d.VariantId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_movements_lot_id_variant_id_fkey");

            entity.HasOne(d => d.InventoryUnit).WithMany(p => p.StockMovements)
                .HasPrincipalKey(p => new { p.Id, p.LotId })
                .HasForeignKey(d => new { d.UnitId, d.LotId })
                .HasConstraintName("stock_movements_unit_id_lot_id_fkey");
        });

        modelBuilder.Entity<StoreCreditAccount>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("store_credit_accounts_pkey");

            entity.ToTable("store_credit_accounts");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BalanceVnd).HasColumnName("balance_vnd");

            entity.HasOne(d => d.User).WithOne(p => p.StoreCreditAccount)
                .HasForeignKey<StoreCreditAccount>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("store_credit_accounts_user_id_fkey");
        });

        modelBuilder.Entity<StoreCreditEntry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("store_credit_entries_pkey");

            entity.ToTable("store_credit_entries");

            entity.HasIndex(e => e.EventKey, "store_credit_entries_event_key_key").IsUnique();

            entity.HasIndex(e => e.TradeInItemId, "trade_in_credit_once")
                .IsUnique()
                .HasFilter("(kind = 'trade_in'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeltaVnd).HasColumnName("delta_vnd");
            entity.Property(e => e.EventKey).HasColumnName("event_key");
            entity.Property(e => e.Kind).HasColumnName("kind");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.QuoteItemId).HasColumnName("quote_item_id");
            entity.Property(e => e.RefundId).HasColumnName("refund_id");
            entity.Property(e => e.TradeInItemId).HasColumnName("trade_in_item_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Order).WithMany(p => p.StoreCreditEntries)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("store_credit_entries_order_id_fkey");

            entity.HasOne(d => d.QuoteItem).WithMany(p => p.StoreCreditEntries)
                .HasForeignKey(d => d.QuoteItemId)
                .HasConstraintName("store_credit_entries_quote_item_id_fkey");

            entity.HasOne(d => d.Refund).WithMany(p => p.StoreCreditEntries)
                .HasForeignKey(d => d.RefundId)
                .HasConstraintName("store_credit_entries_refund_id_fkey");

            entity.HasOne(d => d.TradeInItem).WithOne(p => p.StoreCreditEntry)
                .HasForeignKey<StoreCreditEntry>(d => d.TradeInItemId)
                .HasConstraintName("store_credit_entries_trade_in_item_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.StoreCreditEntries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("store_credit_entries_user_id_fkey");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("suppliers_pkey");

            entity.ToTable("suppliers");

            entity.HasIndex(e => e.Code, "suppliers_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Contact).HasColumnName("contact");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<TradeInInspection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trade_in_inspections_pkey");

            entity.ToTable("trade_in_inspections");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AssessedCondition)
                .HasColumnType("jsonb")
                .HasColumnName("assessed_condition");
            entity.Property(e => e.ChecklistVersion).HasColumnName("checklist_version");
            entity.Property(e => e.InspectedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("inspected_at");
            entity.Property(e => e.InspectorStaffId).HasColumnName("inspector_staff_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");

            entity.HasOne(d => d.InspectorStaff).WithMany(p => p.TradeInInspections)
                .HasForeignKey(d => d.InspectorStaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_inspections_inspector_staff_id_fkey");

            entity.HasOne(d => d.Item).WithMany(p => p.TradeInInspections)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_inspections_item_id_fkey");

            entity.HasMany(d => d.Media).WithMany(p => p.Inspections)
                .UsingEntity<Dictionary<string, object>>(
                    "TradeInEvidence",
                    r => r.HasOne<MediaAsset>().WithMany()
                        .HasForeignKey("MediaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("trade_in_evidence_media_id_fkey"),
                    l => l.HasOne<TradeInInspection>().WithMany()
                        .HasForeignKey("InspectionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("trade_in_evidence_inspection_id_fkey"),
                    j =>
                    {
                        j.HasKey("InspectionId", "MediaId").HasName("trade_in_evidence_pkey");
                        j.ToTable("trade_in_evidence");
                        j.IndexerProperty<Guid>("InspectionId").HasColumnName("inspection_id");
                        j.IndexerProperty<Guid>("MediaId").HasColumnName("media_id");
                    });
        });

        modelBuilder.Entity<TradeInItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trade_in_items_pkey");

            entity.ToTable("trade_in_items");

            entity.HasIndex(e => e.Identifier, "trade_in_identifier_active_idx")
                .IsUnique()
                .HasFilter("((identifier IS NOT NULL) AND (state <> ALL (ARRAY['completed'::text, 'returned'::text])))");

            entity.HasIndex(e => new { e.Id, e.RequestId }, "trade_in_items_id_request_id_key").IsUnique();

            entity.HasIndex(e => e.InternalCode, "trade_in_items_internal_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ConditionSnapshot)
                .HasColumnType("jsonb")
                .HasColumnName("condition_snapshot");
            entity.Property(e => e.Identifier).HasColumnName("identifier");
            entity.Property(e => e.InternalCode).HasColumnName("internal_code");
            entity.Property(e => e.InventoryUnitId).HasColumnName("inventory_unit_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Qty).HasColumnName("qty");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");

            entity.HasOne(d => d.InventoryUnit).WithMany(p => p.TradeInItems)
                .HasForeignKey(d => d.InventoryUnitId)
                .HasConstraintName("trade_in_items_inventory_unit_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.TradeInItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_items_product_id_fkey");

            entity.HasOne(d => d.Request).WithMany(p => p.TradeInItems)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_items_request_id_fkey");

            entity.HasOne(d => d.ProductVariant).WithMany(p => p.TradeInItems)
                .HasPrincipalKey(p => new { p.Id, p.ProductId })
                .HasForeignKey(d => new { d.VariantId, d.ProductId })
                .HasConstraintName("trade_in_items_variant_id_product_id_fkey");
        });

        modelBuilder.Entity<TradeInQuote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trade_in_quotes_pkey");

            entity.ToTable("trade_in_quotes");

            entity.HasIndex(e => new { e.Id, e.RequestId }, "trade_in_quotes_id_request_id_key").IsUnique();

            entity.HasIndex(e => new { e.RequestId, e.VersionNo }, "trade_in_quotes_request_id_version_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.QuotedBy).HasColumnName("quoted_by");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.VersionNo).HasColumnName("version_no");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.TradeInQuoteApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("trade_in_quotes_approved_by_fkey");

            entity.HasOne(d => d.QuotedByNavigation).WithMany(p => p.TradeInQuoteQuotedByNavigations)
                .HasForeignKey(d => d.QuotedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_quotes_quoted_by_fkey");

            entity.HasOne(d => d.Request).WithMany(p => p.TradeInQuotes)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_quotes_request_id_fkey");
        });

        modelBuilder.Entity<TradeInQuoteItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trade_in_quote_items_pkey");

            entity.ToTable("trade_in_quote_items");

            entity.HasIndex(e => new { e.QuoteId, e.ItemId }, "trade_in_quote_items_quote_id_item_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.DecidedAt).HasColumnName("decided_at");
            entity.Property(e => e.Decision).HasColumnName("decision");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.OfferedVnd).HasColumnName("offered_vnd");
            entity.Property(e => e.QuoteId).HasColumnName("quote_id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");

            entity.HasOne(d => d.TradeInItem).WithMany(p => p.TradeInQuoteItems)
                .HasPrincipalKey(p => new { p.Id, p.RequestId })
                .HasForeignKey(d => new { d.ItemId, d.RequestId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_quote_items_item_id_request_id_fkey");

            entity.HasOne(d => d.TradeInQuote).WithMany(p => p.TradeInQuoteItems)
                .HasPrincipalKey(p => new { p.Id, p.RequestId })
                .HasForeignKey(d => new { d.QuoteId, d.RequestId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_quote_items_quote_id_request_id_fkey");
        });

        modelBuilder.Entity<TradeInRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trade_in_requests_pkey");

            entity.ToTable("trade_in_requests");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ReceptionMethod).HasColumnName("reception_method");
            entity.Property(e => e.State).HasColumnName("state");

            entity.HasOne(d => d.Customer).WithMany(p => p.TradeInRequests)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_requests_customer_id_fkey");
        });

        modelBuilder.Entity<TradeInShipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trade_in_shipments_pkey");

            entity.ToTable("trade_in_shipments");

            entity.HasIndex(e => e.TrackingNo, "trade_in_shipments_tracking_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Direction).HasColumnName("direction");
            entity.Property(e => e.FeeVnd).HasColumnName("fee_vnd");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.TrackingNo).HasColumnName("tracking_no");

            entity.HasOne(d => d.Request).WithMany(p => p.TradeInShipments)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("trade_in_shipments_request_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_pkey");

            entity.ToTable("user");

            entity.HasIndex(e => e.Email, "user_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.EmailConfirmed).HasColumnName("email_verified");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasMany(d => d.Products).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "Wishlist",
                    r => r.HasOne<Product>().WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("wishlists_product_id_fkey"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("wishlists_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "ProductId").HasName("wishlists_pkey");
                        j.ToTable("wishlists");
                        j.IndexerProperty<string>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("ProductId").HasColumnName("product_id");
                    });
        });

        modelBuilder.Entity<VariantAttributeValue>(entity =>
        {
            entity.HasKey(e => new { e.VariantId, e.AttributeId }).HasName("variant_attribute_values_pkey");

            entity.ToTable("variant_attribute_values");

            entity.Property(e => e.VariantId).HasColumnName("variant_id");
            entity.Property(e => e.AttributeId).HasColumnName("attribute_id");
            entity.Property(e => e.ValueBoolean).HasColumnName("value_boolean");
            entity.Property(e => e.ValueNumber).HasColumnName("value_number");
            entity.Property(e => e.ValueText).HasColumnName("value_text");

            entity.HasOne(d => d.Attribute).WithMany(p => p.VariantAttributeValues)
                .HasForeignKey(d => d.AttributeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("variant_attribute_values_attribute_id_fkey");

            entity.HasOne(d => d.Variant).WithMany(p => p.VariantAttributeValues)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("variant_attribute_values_variant_id_fkey");
        });

        modelBuilder.Entity<VariantStock>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("variant_stock");

            entity.Property(e => e.Available).HasColumnName("available");
            entity.Property(e => e.VariantId).HasColumnName("variant_id");
        });

        modelBuilder.Entity<Verification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("verification_pkey");

            entity.ToTable("verification");

            entity.HasIndex(e => e.Identifier, "verification_identifier_idx");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Identifier).HasColumnName("identifier");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warehouses_pkey");

            entity.ToTable("warehouses");

            entity.HasIndex(e => e.Code, "warehouses_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<WarrantyClaim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warranty_claims_pkey");

            entity.ToTable("warranty_claims");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DamageKind).HasColumnName("damage_kind");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EntitlementId).HasColumnName("entitlement_id");
            entity.Property(e => e.InspectionResult).HasColumnName("inspection_result");
            entity.Property(e => e.State).HasColumnName("state");

            entity.HasOne(d => d.Entitlement).WithMany(p => p.WarrantyClaims)
                .HasForeignKey(d => d.EntitlementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_claims_entitlement_id_fkey");

            entity.HasMany(d => d.Media).WithMany(p => p.Claims)
                .UsingEntity<Dictionary<string, object>>(
                    "WarrantyEvidence",
                    r => r.HasOne<MediaAsset>().WithMany()
                        .HasForeignKey("MediaId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("warranty_evidence_media_id_fkey"),
                    l => l.HasOne<WarrantyClaim>().WithMany()
                        .HasForeignKey("ClaimId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("warranty_evidence_claim_id_fkey"),
                    j =>
                    {
                        j.HasKey("ClaimId", "MediaId").HasName("warranty_evidence_pkey");
                        j.ToTable("warranty_evidence");
                        j.IndexerProperty<Guid>("ClaimId").HasColumnName("claim_id");
                        j.IndexerProperty<Guid>("MediaId").HasColumnName("media_id");
                    });
        });

        modelBuilder.Entity<WarrantyEntitlement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warranty_entitlements_pkey");

            entity.ToTable("warranty_entitlements");

            entity.HasIndex(e => e.OrderItemUnitAmountId, "warranty_entitlements_order_item_unit_amount_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.EndsOn).HasColumnName("ends_on");
            entity.Property(e => e.InventoryUnitId).HasColumnName("inventory_unit_id");
            entity.Property(e => e.OrderItemUnitAmountId).HasColumnName("order_item_unit_amount_id");
            entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
            entity.Property(e => e.StartsOn).HasColumnName("starts_on");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Used).HasColumnName("used");

            entity.HasOne(d => d.InventoryUnit).WithMany(p => p.WarrantyEntitlements)
                .HasForeignKey(d => d.InventoryUnitId)
                .HasConstraintName("warranty_entitlements_inventory_unit_id_fkey");

            entity.HasOne(d => d.OrderItemUnitAmount).WithOne(p => p.WarrantyEntitlement)
                .HasForeignKey<WarrantyEntitlement>(d => d.OrderItemUnitAmountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_entitlements_order_item_unit_amount_id_fkey");

            entity.HasOne(d => d.PlanVersion).WithMany(p => p.WarrantyEntitlements)
                .HasForeignKey(d => d.PlanVersionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_entitlements_plan_version_id_fkey");
        });

        modelBuilder.Entity<WarrantyEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warranty_events_pkey");

            entity.ToTable("warranty_events");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.ClaimId).HasColumnName("claim_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Event).HasColumnName("event");
            entity.Property(e => e.Note).HasColumnName("note");

            entity.HasOne(d => d.Actor).WithMany(p => p.WarrantyEvents)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_events_actor_id_fkey");

            entity.HasOne(d => d.Claim).WithMany(p => p.WarrantyEvents)
                .HasForeignKey(d => d.ClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_events_claim_id_fkey");
        });

        modelBuilder.Entity<WarrantyPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warranty_plans_pkey");

            entity.ToTable("warranty_plans");

            entity.HasIndex(e => e.Code, "warranty_plans_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValue(true)
                .HasColumnName("active");
            entity.Property(e => e.Code).HasColumnName("code");
        });

        modelBuilder.Entity<WarrantyPlanVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warranty_plan_versions_pkey");

            entity.ToTable("warranty_plan_versions");

            entity.HasIndex(e => new { e.PlanId, e.VersionNo }, "warranty_plan_versions_plan_id_version_no_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.FeeVnd).HasColumnName("fee_vnd");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.PolicySnapshot)
                .HasColumnType("jsonb")
                .HasColumnName("policy_snapshot");
            entity.Property(e => e.TermMonths).HasColumnName("term_months");
            entity.Property(e => e.VersionNo).HasColumnName("version_no");

            entity.HasOne(d => d.Plan).WithMany(p => p.WarrantyPlanVersions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_plan_versions_plan_id_fkey");
        });

        modelBuilder.Entity<WarrantyReplacement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warranty_replacements_pkey");

            entity.ToTable("warranty_replacements");

            entity.HasIndex(e => e.ClaimId, "warranty_replacements_claim_id_key").IsUnique();

            entity.HasIndex(e => e.NewUnitId, "warranty_replacements_new_unit_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ClaimId).HasColumnName("claim_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.NewUnitId).HasColumnName("new_unit_id");
            entity.Property(e => e.OldUnitId).HasColumnName("old_unit_id");

            entity.HasOne(d => d.Claim).WithOne(p => p.WarrantyReplacement)
                .HasForeignKey<WarrantyReplacement>(d => d.ClaimId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_replacements_claim_id_fkey");

            entity.HasOne(d => d.NewUnit).WithOne(p => p.WarrantyReplacementNewUnit)
                .HasForeignKey<WarrantyReplacement>(d => d.NewUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_replacements_new_unit_id_fkey");

            entity.HasOne(d => d.OldUnit).WithMany(p => p.WarrantyReplacementOldUnits)
                .HasForeignKey(d => d.OldUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warranty_replacements_old_unit_id_fkey");
        });
        modelBuilder.HasSequence("invoice_number_seq");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder b);
}
