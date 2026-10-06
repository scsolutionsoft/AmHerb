using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using AmHerb.Web.Domain;

namespace AmHerb.Web.Data;

public sealed class AmHerbDbContext(DbContextOptions<AmHerbDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Member> Members => Set<Member>();
    public DbSet<CreditAccount> CreditAccounts => Set<CreditAccount>();
    public DbSet<CreditInvoice> CreditInvoices => Set<CreditInvoice>();
    public DbSet<CreditReceipt> CreditReceipts => Set<CreditReceipt>();
    public DbSet<CreditAllocation> CreditAllocations => Set<CreditAllocation>();
    public DbSet<MemberClosure> MemberClosures => Set<MemberClosure>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sku> Skus => Set<Sku>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();
    public DbSet<TokenRate> TokenRates => Set<TokenRate>();
    public DbSet<TokenPolicy> TokenPolicies => Set<TokenPolicy>();
    public DbSet<ChannelPolicy> ChannelPolicies => Set<ChannelPolicy>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<ContentPost> ContentPosts => Set<ContentPost>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<TransferAllocation> TransferAllocations => Set<TransferAllocation>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StockAllocation> StockAllocations => Set<StockAllocation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<TokenDistribution> TokenDistributions => Set<TokenDistribution>();
    public DbSet<TokenLedger> TokenLedger => Set<TokenLedger>();
    public DbSet<TokenConsumption> TokenConsumptions => Set<TokenConsumption>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShippingRule> ShippingRules => Set<ShippingRule>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<ReferralVisit> ReferralVisits => Set<ReferralVisit>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<PosRegister> PosRegisters => Set<PosRegister>();
    public DbSet<PosSession> PosSessions => Set<PosSession>();
    public DbSet<ReportSnapshot> ReportSnapshots => Set<ReportSnapshot>();
    public DbSet<RedemptionAuthorization> RedemptionAuthorizations => Set<RedemptionAuthorization>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<CreditAccount>().HasIndex(x=>x.MemberId).IsUnique();
        b.Entity<CreditInvoice>().HasIndex(x=>x.OrderId).IsUnique();
        b.Entity<CreditInvoice>().HasIndex(x=>new{x.CreditAccountId,x.DueAt});
        b.Entity<CreditReceipt>().HasIndex(x=>x.RequestKey).IsUnique();
        b.Entity<CreditReceipt>().HasIndex(x=>x.ConfirmedReference).IsUnique().HasFilter("[ConfirmedReference] IS NOT NULL");
        b.Entity<CreditAllocation>().HasIndex(x=>new{x.CreditReceiptId,x.CreditInvoiceId}).IsUnique();
        b.Entity<CreditAccount>().ToTable(t=>t.HasCheckConstraint("CK_CreditAccount_Limit","[Limit] >= 0 AND [TermDays] BETWEEN 1 AND 365"));
        b.Entity<CreditInvoice>().ToTable(t=>t.HasCheckConstraint("CK_CreditInvoice_Balance","[Amount] > 0 AND [Paid] >= 0 AND [Adjusted] >= 0 AND [Amount] >= [Paid] + [Adjusted]"));
        b.Entity<CreditReceipt>().ToTable(t=>t.HasCheckConstraint("CK_CreditReceipt_Amount","[Amount] > 0"));
        b.Entity<CreditAllocation>().ToTable(t=>t.HasCheckConstraint("CK_CreditAllocation_Amount","[Amount] > 0"));
        b.Entity<MediaAsset>().HasIndex(x => new { x.ProductId, x.Slot }).IsUnique().HasFilter("[ProductId] IS NOT NULL");
        b.Entity<MediaAsset>().ToTable(t => t.HasCheckConstraint("CK_Media_Slot", "[ProductId] IS NULL OR [Slot] BETWEEN 1 AND 5"));
        b.Entity<ContentPost>().HasIndex(x => new { x.Published, x.StartsAt, x.EndsAt });
        b.Entity<ConversationMessage>().HasIndex(x => x.RequestKey).IsUnique();
        b.Entity<Conversation>().HasIndex(x => new { x.MemberId, x.UpdatedAt });
        b.Entity<JournalEntry>().HasIndex(x => x.EventKey).IsUnique();
        b.Entity<JournalLine>().ToTable(t => t.HasCheckConstraint("CK_JournalLine_Amount", "([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)"));
        b.Entity<StockTransfer>().HasIndex(x => x.RequestKey).IsUnique();
        b.Entity<StockTransfer>().HasIndex(x => new { x.DestinationWarehouseId, x.Status });
        b.Entity<StockTransfer>().HasOne(x => x.SourceWarehouse).WithMany().HasForeignKey(x => x.SourceWarehouseId);
        b.Entity<StockTransfer>().HasOne(x => x.DestinationWarehouse).WithMany().HasForeignKey(x => x.DestinationWarehouseId);
        b.Entity<StockTransfer>().ToTable(t => t.HasCheckConstraint("CK_Transfer_Quantity", "[Quantity] > 0 AND [SourceWarehouseId] <> [DestinationWarehouseId]"));
        b.Entity<TransferAllocation>().HasIndex(x => new { x.StockTransferId, x.SourceBatchId }).IsUnique();
        b.Entity<TransferAllocation>().ToTable(t => t.HasCheckConstraint("CK_TransferAllocation_Quantity", "[Quantity] > 0"));
        b.Entity<Member>().HasOne(x => x.Sponsor).WithMany().HasForeignKey(x => x.SponsorMemberId);
        b.Entity<MemberClosure>().HasKey(x => new { x.AncestorMemberId, x.DescendantMemberId });
        b.Entity<MemberClosure>().HasOne(x => x.Ancestor).WithMany().HasForeignKey(x => x.AncestorMemberId);
        b.Entity<MemberClosure>().HasOne(x => x.Descendant).WithMany().HasForeignKey(x => x.DescendantMemberId);
        b.Entity<MemberClosure>().HasIndex(x => new { x.DescendantMemberId, x.Depth });
        b.Entity<Order>().HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerMemberId);
        b.Entity<Order>().HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerMemberId);
        b.Entity<Member>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<Warehouse>().HasIndex(x => new { x.OwnerMemberId, x.Kind });
        b.Entity<Warehouse>().ToTable(t => t.HasCheckConstraint("CK_Warehouse_Owner", "([Kind] = 0 AND [OwnerMemberId] IS NULL) OR ([Kind] IN (1,2) AND [OwnerMemberId] IS NOT NULL)"));
        b.Entity<Member>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Member>().HasIndex(x => x.ReferralCode).IsUnique();
        b.Entity<Sku>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Order>().HasIndex(x => x.Number).IsUnique();
        b.Entity<Order>().HasIndex(x => x.PublicId).IsUnique();
        b.Entity<Order>().HasIndex(x => x.IdempotencyKey).IsUnique();
        b.Entity<Order>().HasIndex(x => new { x.Channel, x.ExternalOrderId }).IsUnique().HasFilter("[ExternalOrderId] IS NOT NULL");
        b.Entity<Order>().HasIndex(x => new { x.CashierUserId, x.CreatedAt });
        b.Entity<Payment>().HasIndex(x => new { x.Provider, x.TransactionRef }).IsUnique().HasFilter("[TransactionRef] IS NOT NULL");
        b.Entity<TokenLedger>().HasIndex(x => x.EventKey).IsUnique();
        b.Entity<TokenLedger>().HasIndex(x => new { x.MemberId, x.PostedAt });
        b.Entity<TokenDistribution>().HasIndex(x => new { x.OrderItemId, x.MemberId, x.Level }).IsUnique();
        b.Entity<TokenDistribution>().HasIndex(x => new { x.Status, x.ReleaseAt });
        b.Entity<TokenConsumption>().HasIndex(x => new { x.DistributionId, x.OrderId }).IsUnique();
        b.Entity<InventoryBatch>().HasIndex(x => new { x.SkuId, x.ExpDate });
        b.Entity<InventoryBatch>().HasIndex(x => new { x.WarehouseId, x.SkuId, x.LotNo }).IsUnique();
        b.Entity<ProductPrice>().HasIndex(x => new { x.SkuId, x.Tier, x.Channel, x.EffectiveFrom });
        b.Entity<TokenRate>().HasIndex(x => new { x.SkuId, x.Channel, x.EffectiveFrom });
        b.Entity<TokenPolicy>().HasIndex(x => x.EffectiveFrom).IsUnique();
        b.Entity<ChannelPolicy>().HasIndex(x => x.Channel).IsUnique();
        b.Entity<SystemSetting>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Promotion>().HasIndex(x => x.Code).IsUnique();
        b.Entity<PosRegister>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<PosSession>().HasIndex(x => x.PosRegisterId).IsUnique().HasFilter("[ClosedAt] IS NULL");
        b.Entity<Notification>().HasIndex(x => x.EventKey).IsUnique();
        b.Entity<ReportSnapshot>().HasIndex(x => x.Day).IsUnique();
        b.Entity<Cart>().HasIndex(x => x.PublicId).IsUnique();
        b.Entity<CartItem>().HasIndex(x => new { x.CartId, x.SkuId }).IsUnique();
        b.Entity<RedemptionAuthorization>().HasIndex(x => x.CodeHash).IsUnique();
        b.Entity<ReferralVisit>().HasIndex(x => x.PublicId).IsUnique();
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
            foreach (var p in entity.GetProperties())
            {
                if (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)) { p.SetPrecision(18); p.SetScale(2); }
                if (p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)) p.SetColumnType("datetime2");
            }
            if (!Database.IsSqlServer() && typeof(VersionedEntity).IsAssignableFrom(entity.ClrType))
                b.Entity(entity.ClrType).Property("RowVersion").ValueGeneratedNever();
        }
        b.Entity<InventoryBatch>().ToTable(t => t.HasCheckConstraint("CK_Batch_Quantities", "[QtyAvailable] >= 0 AND [QtyReserved] >= 0"));
        b.Entity<OrderItem>().ToTable(t => t.HasCheckConstraint("CK_Item_Quantities", "[Quantity] > 0 AND [ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]"));
        if (Database.IsSqlServer())
        {
            b.Entity<TokenLedger>().ToTable(t => t.UseSqlOutputClause(false));
            b.Entity<AuditLog>().ToTable(t => t.UseSqlOutputClause(false));
            b.Entity<InventoryTransaction>().ToTable(t => t.UseSqlOutputClause(false));
            b.Entity<JournalEntry>().ToTable(t => t.UseSqlOutputClause(false));
            b.Entity<JournalLine>().ToTable(t => t.UseSqlOutputClause(false));
            b.Entity<CreditAllocation>().ToTable(t=>t.UseSqlOutputClause(false));
            b.Entity<CreditReceipt>().ToTable(t=>t.UseSqlOutputClause(false));
        }
        SeedData.Configure(b);
    }
    private void GuardHistory()
    {
        if(ChangeTracker.Entries<CreditReceipt>().Any(x=>x.State is EntityState.Modified or EntityState.Deleted && x.OriginalValues.GetValue<CreditReceiptStatus>(nameof(CreditReceipt.Status))!=CreditReceiptStatus.Pending))
            throw new InvalidOperationException("Reviewed credit receipts are immutable.");
        if (ChangeTracker.Entries().Any(e => (e.Entity is Domain.TokenLedger or AuditLog or InventoryTransaction or JournalEntry or JournalLine or CreditAllocation) &&
            e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Posted history is append-only. Create a compensating entry.");
        foreach(var entry in ChangeTracker.Entries<JournalEntry>().Where(x=>x.State==EntityState.Added))
            Services.AccountingService.Validate(entry.Entity);
        if(ChangeTracker.Entries<JournalLine>().Any(x=>x.State==EntityState.Added && (x.Entity.JournalEntry==null || Entry(x.Entity.JournalEntry).State!=EntityState.Added)))
            throw new InvalidOperationException("Cannot append lines to a posted journal entry.");
    }
    public override int SaveChanges(bool acceptAllChangesOnSuccess) { GuardHistory(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    { GuardHistory(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
}
