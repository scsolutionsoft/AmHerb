IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorId] nvarchar(450) NOT NULL,
        [Action] nvarchar(120) NOT NULL,
        [Subject] nvarchar(100) NOT NULL,
        [Detail] nvarchar(2000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Carts] (
        [Id] bigint NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Carts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [ChannelPolicies] (
        [Id] bigint NOT NULL IDENTITY,
        [Channel] int NOT NULL,
        [AllowNetworkReward] bit NOT NULL,
        [AllowRedemption] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ChannelPolicies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NULL,
        [EventKey] nvarchar(160) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Read] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(160) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [RegistrationNumber] nvarchar(100) NOT NULL,
        [Manufacturer] nvarchar(200) NOT NULL,
        [Distributor] nvarchar(200) NOT NULL,
        [Warnings] nvarchar(2000) NOT NULL,
        [LabelDocumentUrl] nvarchar(500) NOT NULL,
        [Active] bit NOT NULL,
        [LotRequired] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [ReportSnapshots] (
        [Id] bigint NOT NULL IDENTITY,
        [Day] datetime2 NOT NULL,
        [Orders] int NOT NULL,
        [Sales] decimal(18,2) NOT NULL,
        [TokenLiability] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ReportSnapshots] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [ShippingRules] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Fee] decimal(18,2) NOT NULL,
        [FreeAbove] decimal(18,2) NOT NULL,
        [Active] bit NOT NULL,
        CONSTRAINT [PK_ShippingRules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [Id] bigint NOT NULL IDENTITY,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(1000) NOT NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [TokenPolicies] (
        [Id] bigint NOT NULL IDENTITY,
        [EffectiveFrom] datetime2 NOT NULL,
        [PendingDays] int NOT NULL,
        [ExpiryMonths] int NOT NULL,
        [SellerPercent] decimal(18,2) NOT NULL,
        [Level1Percent] decimal(18,2) NOT NULL,
        [Level2Percent] decimal(18,2) NOT NULL,
        [Level3Percent] decimal(18,2) NOT NULL,
        [MaxDepth] int NOT NULL,
        [RedemptionReferenceThb] decimal(18,2) NOT NULL,
        [MaxRedemptionPercent] decimal(18,2) NOT NULL,
        [LoyaltyEnabled] bit NOT NULL,
        [LoyaltyPercent] decimal(18,2) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        CONSTRAINT [PK_TokenPolicies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Warehouses] (
        [Id] bigint NOT NULL IDENTITY,
        [Name] nvarchar(120) NOT NULL,
        [Active] bit NOT NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Members] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Code] nvarchar(40) NOT NULL,
        [ReferralCode] nvarchar(40) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Phone] nvarchar(40) NOT NULL,
        [Address] nvarchar(1000) NOT NULL,
        [SponsorMemberId] bigint NULL,
        [Status] int NOT NULL,
        [Type] int NOT NULL,
        [ReviewRequired] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Members] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Members_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Members_Members_SponsorMemberId] FOREIGN KEY ([SponsorMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Skus] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NOT NULL,
        [Code] nvarchar(80) NOT NULL,
        [Barcode] nvarchar(80) NOT NULL,
        [Variant] nvarchar(160) NOT NULL,
        [CapsuleCount] int NULL,
        [WeightGrams] decimal(18,2) NULL,
        [Active] bit NOT NULL,
        [TokenEligible] bit NOT NULL,
        [Cost] decimal(18,2) NOT NULL,
        [LowStockThreshold] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Skus] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Skus_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [PosRegisters] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Enabled] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PosRegisters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PosRegisters_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PosRegisters_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [MemberClosures] (
        [AncestorMemberId] bigint NOT NULL,
        [DescendantMemberId] bigint NOT NULL,
        [Depth] int NOT NULL,
        CONSTRAINT [PK_MemberClosures] PRIMARY KEY ([AncestorMemberId], [DescendantMemberId]),
        CONSTRAINT [FK_MemberClosures_Members_AncestorMemberId] FOREIGN KEY ([AncestorMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberClosures_Members_DescendantMemberId] FOREIGN KEY ([DescendantMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [CartItems] (
        [Id] bigint NOT NULL IDENTITY,
        [CartId] bigint NOT NULL,
        [SkuId] bigint NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CartItems_Carts_CartId] FOREIGN KEY ([CartId]) REFERENCES [Carts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CartItems_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [InventoryBatches] (
        [Id] bigint NOT NULL IDENTITY,
        [WarehouseId] bigint NOT NULL,
        [SkuId] bigint NOT NULL,
        [LotNo] nvarchar(100) NOT NULL,
        [MfgDate] datetime2 NOT NULL,
        [ExpDate] datetime2 NOT NULL,
        [QtyReceived] int NOT NULL,
        [QtyAvailable] int NOT NULL,
        [QtyReserved] int NOT NULL,
        [Cost] decimal(18,2) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_InventoryBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Batch_Quantities] CHECK ([QtyAvailable] >= 0 AND [QtyReserved] >= 0),
        CONSTRAINT [FK_InventoryBatches_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryBatches_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [ProductPrices] (
        [Id] bigint NOT NULL IDENTITY,
        [SkuId] bigint NOT NULL,
        [Tier] nvarchar(40) NOT NULL,
        [PackQuantity] int NOT NULL,
        [Channel] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [EffectiveTo] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ProductPrices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductPrices_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Promotions] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(60) NOT NULL,
        [Kind] int NOT NULL,
        [Value] decimal(18,2) NOT NULL,
        [MinimumSpend] decimal(18,2) NOT NULL,
        [RequiredQuantity] int NOT NULL,
        [EligibleSkuId] bigint NULL,
        [GiftSkuId] bigint NULL,
        [Channel] int NULL,
        [MemberType] int NULL,
        [UsageLimit] int NOT NULL,
        [UsedCount] int NOT NULL,
        [Active] bit NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [EffectiveTo] datetime2 NOT NULL,
        [TokenMultiplier] decimal(18,2) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Promotions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Promotions_Skus_EligibleSkuId] FOREIGN KEY ([EligibleSkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Promotions_Skus_GiftSkuId] FOREIGN KEY ([GiftSkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [TokenRates] (
        [Id] bigint NOT NULL IDENTITY,
        [SkuId] bigint NOT NULL,
        [Channel] int NULL,
        [BaseToken] decimal(18,2) NOT NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [EffectiveTo] datetime2 NULL,
        CONSTRAINT [PK_TokenRates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TokenRates_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [PosSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [PosRegisterId] bigint NOT NULL,
        [OpenedAt] datetime2 NOT NULL,
        [ClosedAt] datetime2 NULL,
        [OpeningCash] decimal(18,2) NOT NULL,
        [CountedCash] decimal(18,2) NULL,
        [ExpectedCash] decimal(18,2) NULL,
        [Difference] decimal(18,2) NULL,
        [Note] nvarchar(1000) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PosSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PosSessions_PosRegisters_PosRegisterId] FOREIGN KEY ([PosRegisterId]) REFERENCES [PosRegisters] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] bigint NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [Number] nvarchar(60) NOT NULL,
        [IdempotencyKey] nvarchar(100) NOT NULL,
        [BuyerMemberId] bigint NULL,
        [SellerMemberId] bigint NULL,
        [CashierUserId] nvarchar(450) NULL,
        [PosSessionId] bigint NULL,
        [Channel] int NOT NULL,
        [ExternalOrderId] nvarchar(100) NULL,
        [Campaign] nvarchar(100) NOT NULL,
        [Creator] nvarchar(100) NOT NULL,
        [Status] int NOT NULL,
        [VerifiedRetailSale] bit NOT NULL,
        [StockLoading] bit NOT NULL,
        [RewardsCreated] bit NOT NULL,
        [CustomerName] nvarchar(160) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Phone] nvarchar(40) NOT NULL,
        [Address] nvarchar(1000) NOT NULL,
        [Merchandise] decimal(18,2) NOT NULL,
        [Discount] decimal(18,2) NOT NULL,
        [Shipping] decimal(18,2) NOT NULL,
        [Tax] decimal(18,2) NOT NULL,
        [GrandTotal] decimal(18,2) NOT NULL,
        [TokenRedemption] decimal(18,2) NOT NULL,
        [TokenValue] decimal(18,2) NOT NULL,
        [CashPayable] decimal(18,2) NOT NULL,
        [ChannelFee] decimal(18,2) NOT NULL,
        [AffiliateFee] decimal(18,2) NOT NULL,
        [TokenPolicyVersion] bigint NOT NULL,
        [PromotionId] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Members_BuyerMemberId] FOREIGN KEY ([BuyerMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Members_SellerMemberId] FOREIGN KEY ([SellerMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_PosSessions_PosSessionId] FOREIGN KEY ([PosSessionId]) REFERENCES [PosSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Promotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [Promotions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [InventoryTransactions] (
        [Id] bigint NOT NULL IDENTITY,
        [InventoryBatchId] bigint NOT NULL,
        [OrderId] bigint NULL,
        [Kind] int NOT NULL,
        [Quantity] int NOT NULL,
        [PostedAt] datetime2 NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryTransactions_InventoryBatches_InventoryBatchId] FOREIGN KEY ([InventoryBatchId]) REFERENCES [InventoryBatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryTransactions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] bigint NOT NULL,
        [SkuId] bigint NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Quantity] int NOT NULL,
        [ReturnedQuantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Discount] decimal(18,2) NOT NULL,
        [TokenValue] decimal(18,2) NOT NULL,
        [BaseToken] decimal(18,2) NOT NULL,
        [TokenMultiplier] decimal(18,2) NOT NULL,
        [TokenRateVersion] bigint NOT NULL,
        [PriceVersion] bigint NOT NULL,
        [IsGift] bit NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Item_Quantities] CHECK ([Quantity] > 0 AND [ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderItems_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] bigint NOT NULL,
        [Provider] nvarchar(50) NOT NULL,
        [TransactionRef] nvarchar(200) NULL,
        [WebhookHash] nvarchar(100) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Tendered] decimal(18,2) NOT NULL,
        [Change] decimal(18,2) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ConfirmedAt] datetime2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [ReferralVisits] (
        [Id] bigint NOT NULL IDENTITY,
        [PublicId] uniqueidentifier NOT NULL,
        [MemberId] bigint NOT NULL,
        [Source] nvarchar(100) NOT NULL,
        [Campaign] nvarchar(100) NOT NULL,
        [LandingPage] nvarchar(500) NOT NULL,
        [ConvertedOrderId] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ReferralVisits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReferralVisits_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReferralVisits_Orders_ConvertedOrderId] FOREIGN KEY ([ConvertedOrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [Shipments] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderId] bigint NOT NULL,
        [Carrier] nvarchar(100) NOT NULL,
        [TrackingNumber] nvarchar(150) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [ShippedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Shipments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [ReturnRequests] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderItemId] bigint NOT NULL,
        [Quantity] int NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Sellable] bit NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CashRefund] decimal(18,2) NOT NULL,
        [TokensRestored] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ApprovedAt] datetime2 NULL,
        [RefundReference] nvarchar(200) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ReturnRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReturnRequests_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [StockAllocations] (
        [Id] bigint NOT NULL IDENTITY,
        [OrderItemId] bigint NOT NULL,
        [InventoryBatchId] bigint NOT NULL,
        [Quantity] int NOT NULL,
        [ReturnedQuantity] int NOT NULL,
        CONSTRAINT [PK_StockAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockAllocations_InventoryBatches_InventoryBatchId] FOREIGN KEY ([InventoryBatchId]) REFERENCES [InventoryBatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockAllocations_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [TokenDistributions] (
        [Id] bigint NOT NULL IDENTITY,
        [MemberId] bigint NOT NULL,
        [OrderItemId] bigint NOT NULL,
        [SourceOrderId] bigint NOT NULL,
        [Level] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ReversedAmount] decimal(18,2) NOT NULL,
        [ExpiredAmount] decimal(18,2) NOT NULL,
        [QuantityBasis] int NOT NULL,
        [ReturnedAtIssue] int NOT NULL,
        [Status] int NOT NULL,
        [RewardPolicyVersion] bigint NOT NULL,
        [TokenRateVersion] bigint NOT NULL,
        [ReleaseAt] datetime2 NOT NULL,
        [ExpireAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TokenDistributions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TokenDistributions_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TokenDistributions_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TokenDistributions_Orders_SourceOrderId] FOREIGN KEY ([SourceOrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [TokenConsumptions] (
        [Id] bigint NOT NULL IDENTITY,
        [DistributionId] bigint NOT NULL,
        [OrderId] bigint NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_TokenConsumptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TokenConsumptions_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TokenConsumptions_TokenDistributions_DistributionId] FOREIGN KEY ([DistributionId]) REFERENCES [TokenDistributions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE TABLE [TokenLedger] (
        [Id] bigint NOT NULL IDENTITY,
        [MemberId] bigint NOT NULL,
        [SourceOrderId] bigint NULL,
        [OrderItemId] bigint NULL,
        [DistributionId] bigint NULL,
        [Kind] int NOT NULL,
        [Status] int NOT NULL,
        [PendingDelta] decimal(18,2) NOT NULL,
        [AvailableDelta] decimal(18,2) NOT NULL,
        [ReservedDelta] decimal(18,2) NOT NULL,
        [RewardPolicyVersion] bigint NOT NULL,
        [TokenRateVersion] bigint NOT NULL,
        [EventKey] nvarchar(160) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [PostedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TokenLedger] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TokenLedger_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TokenLedger_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TokenLedger_Orders_SourceOrderId] FOREIGN KEY ([SourceOrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TokenLedger_TokenDistributions_DistributionId] FOREIGN KEY ([DistributionId]) REFERENCES [TokenDistributions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowNetworkReward', N'AllowRedemption', N'Channel') AND [object_id] = OBJECT_ID(N'[ChannelPolicies]'))
        SET IDENTITY_INSERT [ChannelPolicies] ON;
    EXEC(N'INSERT INTO [ChannelPolicies] ([Id], [AllowNetworkReward], [AllowRedemption], [Channel])
    VALUES (CAST(1 AS bigint), CAST(1 AS bit), CAST(1 AS bit), 0),
    (CAST(2 AS bigint), CAST(1 AS bit), CAST(1 AS bit), 1),
    (CAST(3 AS bigint), CAST(1 AS bit), CAST(0 AS bit), 2),
    (CAST(4 AS bigint), CAST(0 AS bit), CAST(0 AS bit), 3),
    (CAST(5 AS bigint), CAST(0 AS bit), CAST(0 AS bit), 4),
    (CAST(6 AS bigint), CAST(0 AS bit), CAST(0 AS bit), 5)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AllowNetworkReward', N'AllowRedemption', N'Channel') AND [object_id] = OBJECT_ID(N'[ChannelPolicies]'))
        SET IDENTITY_INSERT [ChannelPolicies] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Active', N'Category', N'Description', N'Distributor', N'ImageUrl', N'LabelDocumentUrl', N'LotRequired', N'Manufacturer', N'Name', N'RegistrationNumber', N'Warnings') AND [object_id] = OBJECT_ID(N'[Products]'))
        SET IDENTITY_INSERT [Products] ON;
    EXEC(N'INSERT INTO [Products] ([Id], [Active], [Category], [Description], [Distributor], [ImageUrl], [LabelDocumentUrl], [LotRequired], [Manufacturer], [Name], [RegistrationNumber], [Warnings])
    VALUES (CAST(1 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''FOREVA'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค''),
    (CAST(2 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''CELL-SYNC'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค''),
    (CAST(3 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''The Ruby'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค''),
    (CAST(4 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''Detoxify Blue'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค''),
    (CAST(5 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''PHYTOSYNC'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค''),
    (CAST(6 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''Anti Neo Plus'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค''),
    (CAST(7 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''Reliva'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค''),
    (CAST(8 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''Reliva Max'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค''),
    (CAST(9 AS bigint), CAST(1 AS bit), N''สมุนไพรและผลิตภัณฑ์เสริมอาหาร'', N'''', N''AM HERB'', N'''', N'''', CAST(1 AS bit), N'''', N''FEEL GOOD CHAMANG'', N'''', N''อ่านฉลากและคำเตือนก่อนบริโภค'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Active', N'Category', N'Description', N'Distributor', N'ImageUrl', N'LabelDocumentUrl', N'LotRequired', N'Manufacturer', N'Name', N'RegistrationNumber', N'Warnings') AND [object_id] = OBJECT_ID(N'[Products]'))
        SET IDENTITY_INSERT [Products] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Active', N'Fee', N'FreeAbove', N'Name') AND [object_id] = OBJECT_ID(N'[ShippingRules]'))
        SET IDENTITY_INSERT [ShippingRules] ON;
    EXEC(N'INSERT INTO [ShippingRules] ([Id], [Active], [Fee], [FreeAbove], [Name])
    VALUES (CAST(1 AS bigint), CAST(1 AS bit), 50.0, 1500.0, N''จัดส่งมาตรฐาน'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Active', N'Fee', N'FreeAbove', N'Name') AND [object_id] = OBJECT_ID(N'[ShippingRules]'))
        SET IDENTITY_INSERT [ShippingRules] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Key', N'Value') AND [object_id] = OBJECT_ID(N'[SystemSettings]'))
        SET IDENTITY_INSERT [SystemSettings] ON;
    EXEC(N'INSERT INTO [SystemSettings] ([Id], [Key], [Value])
    VALUES (CAST(1 AS bigint), N''RewardPendingDays'', N''14''),
    (CAST(2 AS bigint), N''MaxUplineRewardDepth'', N''3''),
    (CAST(3 AS bigint), N''TokenRedemptionReferenceTHB'', N''1.00''),
    (CAST(4 AS bigint), N''TokenTransferEnabled'', N''false''),
    (CAST(5 AS bigint), N''TokenCashWithdrawalEnabled'', N''false''),
    (CAST(6 AS bigint), N''TokenExpiryMonths'', N''12''),
    (CAST(7 AS bigint), N''MaxTokenRedemptionPercent'', N''100''),
    (CAST(8 AS bigint), N''ShippingRedeemableWithToken'', N''false''),
    (CAST(9 AS bigint), N''ReferralCookieDays'', N''30''),
    (CAST(10 AS bigint), N''AbandonedCheckoutHours'', N''24'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Key', N'Value') AND [object_id] = OBJECT_ID(N'[SystemSettings]'))
        SET IDENTITY_INSERT [SystemSettings] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'EffectiveFrom', N'ExpiryMonths', N'Level1Percent', N'Level2Percent', N'Level3Percent', N'LoyaltyEnabled', N'LoyaltyPercent', N'MaxDepth', N'MaxRedemptionPercent', N'PendingDays', N'Reason', N'RedemptionReferenceThb', N'SellerPercent') AND [object_id] = OBJECT_ID(N'[TokenPolicies]'))
        SET IDENTITY_INSERT [TokenPolicies] ON;
    EXEC(N'INSERT INTO [TokenPolicies] ([Id], [EffectiveFrom], [ExpiryMonths], [Level1Percent], [Level2Percent], [Level3Percent], [LoyaltyEnabled], [LoyaltyPercent], [MaxDepth], [MaxRedemptionPercent], [PendingDays], [Reason], [RedemptionReferenceThb], [SellerPercent])
    VALUES (CAST(1 AS bigint), ''2020-01-01T00:00:00.0000000Z'', 12, 20.0, 7.0, 3.0, CAST(0 AS bit), 0.0, 3, 100.0, 14, N''Initial policy'', 1.0, 100.0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'EffectiveFrom', N'ExpiryMonths', N'Level1Percent', N'Level2Percent', N'Level3Percent', N'LoyaltyEnabled', N'LoyaltyPercent', N'MaxDepth', N'MaxRedemptionPercent', N'PendingDays', N'Reason', N'RedemptionReferenceThb', N'SellerPercent') AND [object_id] = OBJECT_ID(N'[TokenPolicies]'))
        SET IDENTITY_INSERT [TokenPolicies] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Active', N'Name') AND [object_id] = OBJECT_ID(N'[Warehouses]'))
        SET IDENTITY_INSERT [Warehouses] ON;
    EXEC(N'INSERT INTO [Warehouses] ([Id], [Active], [Name])
    VALUES (CAST(1 AS bigint), CAST(1 AS bit), N''คลังกลาง AM HERB'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Active', N'Name') AND [object_id] = OBJECT_ID(N'[Warehouses]'))
        SET IDENTITY_INSERT [Warehouses] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Active', N'Barcode', N'CapsuleCount', N'Code', N'Cost', N'LowStockThreshold', N'ProductId', N'TokenEligible', N'Variant', N'WeightGrams') AND [object_id] = OBJECT_ID(N'[Skus]'))
        SET IDENTITY_INSERT [Skus] ON;
    EXEC(N'INSERT INTO [Skus] ([Id], [Active], [Barcode], [CapsuleCount], [Code], [Cost], [LowStockThreshold], [ProductId], [TokenEligible], [Variant], [WeightGrams])
    VALUES (CAST(1 AS bigint), CAST(1 AS bit), N''FOREVA'', NULL, N''FOREVA'', 0.0, 10, CAST(1 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL),
    (CAST(2 AS bigint), CAST(1 AS bit), N''CELL-SYNC'', NULL, N''CELL-SYNC'', 0.0, 10, CAST(2 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL),
    (CAST(3 AS bigint), CAST(1 AS bit), N''RUBY'', NULL, N''RUBY'', 0.0, 10, CAST(3 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL),
    (CAST(4 AS bigint), CAST(1 AS bit), N''DETOXIFY-BLUE'', NULL, N''DETOXIFY-BLUE'', 0.0, 10, CAST(4 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL),
    (CAST(5 AS bigint), CAST(1 AS bit), N''PHYTOSYNC'', NULL, N''PHYTOSYNC'', 0.0, 10, CAST(5 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL),
    (CAST(6 AS bigint), CAST(1 AS bit), N''ANTI-NEO-PLUS'', NULL, N''ANTI-NEO-PLUS'', 0.0, 10, CAST(6 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL),
    (CAST(7 AS bigint), CAST(1 AS bit), N''RELIVA'', NULL, N''RELIVA'', 0.0, 10, CAST(7 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL),
    (CAST(8 AS bigint), CAST(1 AS bit), N''RELIVA-MAX'', NULL, N''RELIVA-MAX'', 0.0, 10, CAST(8 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL),
    (CAST(9 AS bigint), CAST(1 AS bit), N''FEEL-GOOD'', NULL, N''FEEL-GOOD'', 0.0, 10, CAST(9 AS bigint), CAST(1 AS bit), N''มาตรฐาน'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Active', N'Barcode', N'CapsuleCount', N'Code', N'Cost', N'LowStockThreshold', N'ProductId', N'TokenEligible', N'Variant', N'WeightGrams') AND [object_id] = OBJECT_ID(N'[Skus]'))
        SET IDENTITY_INSERT [Skus] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Amount', N'Channel', N'EffectiveFrom', N'EffectiveTo', N'PackQuantity', N'SkuId', N'Tier') AND [object_id] = OBJECT_ID(N'[ProductPrices]'))
        SET IDENTITY_INSERT [ProductPrices] ON;
    EXEC(N'INSERT INTO [ProductPrices] ([Id], [Amount], [Channel], [EffectiveFrom], [EffectiveTo], [PackQuantity], [SkuId], [Tier])
    VALUES (CAST(1 AS bigint), 790.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(1 AS bigint), N''Retail''),
    (CAST(2 AS bigint), 690.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(1 AS bigint), N''Promo''),
    (CAST(3 AS bigint), 495.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(1 AS bigint), N''Wholesale''),
    (CAST(4 AS bigint), 2821.5, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 6, CAST(1 AS bigint), N''Pack6''),
    (CAST(5 AS bigint), 5400.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 12, CAST(1 AS bigint), N''Pack12''),
    (CAST(6 AS bigint), 990.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(2 AS bigint), N''Retail''),
    (CAST(7 AS bigint), 890.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(2 AS bigint), N''Promo''),
    (CAST(8 AS bigint), 605.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(2 AS bigint), N''Wholesale''),
    (CAST(9 AS bigint), 3448.5, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 6, CAST(2 AS bigint), N''Pack6''),
    (CAST(10 AS bigint), 6600.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 12, CAST(2 AS bigint), N''Pack12''),
    (CAST(11 AS bigint), 1490.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(3 AS bigint), N''Retail''),
    (CAST(12 AS bigint), 1290.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(3 AS bigint), N''Promo''),
    (CAST(13 AS bigint), 935.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(3 AS bigint), N''Wholesale''),
    (CAST(14 AS bigint), 5329.5, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 6, CAST(3 AS bigint), N''Pack6''),
    (CAST(15 AS bigint), 10200.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 12, CAST(3 AS bigint), N''Pack12''),
    (CAST(16 AS bigint), 1090.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(4 AS bigint), N''Retail''),
    (CAST(17 AS bigint), 690.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(5 AS bigint), N''Retail''),
    (CAST(18 AS bigint), 590.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(5 AS bigint), N''Promo''),
    (CAST(19 AS bigint), 418.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(5 AS bigint), N''Wholesale''),
    (CAST(20 AS bigint), 2382.6, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 6, CAST(5 AS bigint), N''Pack6''),
    (CAST(21 AS bigint), 4560.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 12, CAST(5 AS bigint), N''Pack12''),
    (CAST(22 AS bigint), 1990.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(6 AS bigint), N''Retail''),
    (CAST(23 AS bigint), 1790.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(6 AS bigint), N''Promo''),
    (CAST(24 AS bigint), 1320.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(6 AS bigint), N''Wholesale''),
    (CAST(25 AS bigint), 7500.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 6, CAST(6 AS bigint), N''Pack6''),
    (CAST(26 AS bigint), 14640.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 12, CAST(6 AS bigint), N''Pack12''),
    (CAST(27 AS bigint), 249.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(7 AS bigint), N''Retail''),
    (CAST(28 AS bigint), 229.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(7 AS bigint), N''Promo''),
    (CAST(29 AS bigint), 161.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(7 AS bigint), N''Wholesale''),
    (CAST(30 AS bigint), 966.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 6, CAST(7 AS bigint), N''Pack6''),
    (CAST(31 AS bigint), 1932.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 12, CAST(7 AS bigint), N''Pack12''),
    (CAST(32 AS bigint), 299.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(8 AS bigint), N''Retail''),
    (CAST(33 AS bigint), 279.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(8 AS bigint), N''Promo''),
    (CAST(34 AS bigint), 195.5, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(8 AS bigint), N''Wholesale''),
    (CAST(35 AS bigint), 1173.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 6, CAST(8 AS bigint), N''Pack6''),
    (CAST(36 AS bigint), 2346.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 12, CAST(8 AS bigint), N''Pack12''),
    (CAST(37 AS bigint), 299.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(9 AS bigint), N''Retail''),
    (CAST(38 AS bigint), 249.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(9 AS bigint), N''Promo''),
    (CAST(39 AS bigint), 180.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 1, CAST(9 AS bigint), N''Wholesale''),
    (CAST(40 AS bigint), 1020.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 6, CAST(9 AS bigint), N''Pack6''),
    (CAST(41 AS bigint), 1980.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, 12, CAST(9 AS bigint), N''Pack12'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Amount', N'Channel', N'EffectiveFrom', N'EffectiveTo', N'PackQuantity', N'SkuId', N'Tier') AND [object_id] = OBJECT_ID(N'[ProductPrices]'))
        SET IDENTITY_INSERT [ProductPrices] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseToken', N'Channel', N'EffectiveFrom', N'EffectiveTo', N'SkuId') AND [object_id] = OBJECT_ID(N'[TokenRates]'))
        SET IDENTITY_INSERT [TokenRates] ON;
    EXEC(N'INSERT INTO [TokenRates] ([Id], [BaseToken], [Channel], [EffectiveFrom], [EffectiveTo], [SkuId])
    VALUES (CAST(1 AS bigint), 60.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bigint)),
    (CAST(2 AS bigint), 90.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(2 AS bigint)),
    (CAST(3 AS bigint), 110.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(3 AS bigint)),
    (CAST(4 AS bigint), 80.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(4 AS bigint)),
    (CAST(5 AS bigint), 50.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(5 AS bigint)),
    (CAST(6 AS bigint), 140.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(6 AS bigint)),
    (CAST(7 AS bigint), 20.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(7 AS bigint)),
    (CAST(8 AS bigint), 25.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(8 AS bigint)),
    (CAST(9 AS bigint), 20.0, NULL, ''2020-01-01T00:00:00.0000000Z'', NULL, CAST(9 AS bigint))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BaseToken', N'Channel', N'EffectiveFrom', N'EffectiveTo', N'SkuId') AND [object_id] = OBJECT_ID(N'[TokenRates]'))
        SET IDENTITY_INSERT [TokenRates] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CartItems_CartId_SkuId] ON [CartItems] ([CartId], [SkuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_CartItems_SkuId] ON [CartItems] ([SkuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Carts_PublicId] ON [Carts] ([PublicId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChannelPolicies_Channel] ON [ChannelPolicies] ([Channel]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_InventoryBatches_SkuId_ExpDate] ON [InventoryBatches] ([SkuId], [ExpDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventoryBatches_WarehouseId_SkuId_LotNo] ON [InventoryBatches] ([WarehouseId], [SkuId], [LotNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_InventoryBatchId] ON [InventoryTransactions] ([InventoryBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_InventoryTransactions_OrderId] ON [InventoryTransactions] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_MemberClosures_DescendantMemberId_Depth] ON [MemberClosures] ([DescendantMemberId], [Depth]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Members_Code] ON [Members] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Members_ReferralCode] ON [Members] ([ReferralCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Members_SponsorMemberId] ON [Members] ([SponsorMemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Members_UserId] ON [Members] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Notifications_EventKey] ON [Notifications] ([EventKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_OrderItems_SkuId] ON [OrderItems] ([SkuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Orders_BuyerMemberId] ON [Orders] ([BuyerMemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Orders_CashierUserId_CreatedAt] ON [Orders] ([CashierUserId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Orders_Channel_ExternalOrderId] ON [Orders] ([Channel], [ExternalOrderId]) WHERE [ExternalOrderId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_IdempotencyKey] ON [Orders] ([IdempotencyKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_Number] ON [Orders] ([Number]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Orders_PosSessionId] ON [Orders] ([PosSessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Orders_PromotionId] ON [Orders] ([PromotionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_PublicId] ON [Orders] ([PublicId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Orders_SellerMemberId] ON [Orders] ([SellerMemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_Provider_TransactionRef] ON [Payments] ([Provider], [TransactionRef]) WHERE [TransactionRef] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PosRegisters_UserId] ON [PosRegisters] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_PosRegisters_WarehouseId] ON [PosRegisters] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PosSessions_PosRegisterId] ON [PosSessions] ([PosRegisterId]) WHERE [ClosedAt] IS NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_ProductPrices_SkuId_Tier_Channel_EffectiveFrom] ON [ProductPrices] ([SkuId], [Tier], [Channel], [EffectiveFrom]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Promotions_Code] ON [Promotions] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Promotions_EligibleSkuId] ON [Promotions] ([EligibleSkuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Promotions_GiftSkuId] ON [Promotions] ([GiftSkuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_ReferralVisits_ConvertedOrderId] ON [ReferralVisits] ([ConvertedOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_ReferralVisits_MemberId] ON [ReferralVisits] ([MemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReferralVisits_PublicId] ON [ReferralVisits] ([PublicId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReportSnapshots_Day] ON [ReportSnapshots] ([Day]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_ReturnRequests_OrderItemId] ON [ReturnRequests] ([OrderItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Shipments_OrderId] ON [Shipments] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Skus_Code] ON [Skus] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_Skus_ProductId] ON [Skus] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_StockAllocations_InventoryBatchId] ON [StockAllocations] ([InventoryBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_StockAllocations_OrderItemId] ON [StockAllocations] ([OrderItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SystemSettings_Key] ON [SystemSettings] ([Key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TokenConsumptions_DistributionId_OrderId] ON [TokenConsumptions] ([DistributionId], [OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenConsumptions_OrderId] ON [TokenConsumptions] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenDistributions_MemberId] ON [TokenDistributions] ([MemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TokenDistributions_OrderItemId_MemberId_Level] ON [TokenDistributions] ([OrderItemId], [MemberId], [Level]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenDistributions_SourceOrderId] ON [TokenDistributions] ([SourceOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenDistributions_Status_ReleaseAt] ON [TokenDistributions] ([Status], [ReleaseAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenLedger_DistributionId] ON [TokenLedger] ([DistributionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TokenLedger_EventKey] ON [TokenLedger] ([EventKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenLedger_MemberId_PostedAt] ON [TokenLedger] ([MemberId], [PostedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenLedger_OrderItemId] ON [TokenLedger] ([OrderItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenLedger_SourceOrderId] ON [TokenLedger] ([SourceOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TokenPolicies_EffectiveFrom] ON [TokenPolicies] ([EffectiveFrom]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    CREATE INDEX [IX_TokenRates_SkuId_Channel_EffectiveFrom] ON [TokenRates] ([SkuId], [Channel], [EffectiveFrom]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063738_InitialCommerceAndUserPos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006063738_InitialCommerceAndUserPos', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063915_AppendOnlyHistory'
)
BEGIN
    EXEC(N'CREATE TRIGGER [dbo].[TR_TokenLedger_AppendOnly] ON [dbo].[TokenLedger] INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51001, ''Token ledger is append-only; use compensating entries.'', 1; END;');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063915_AppendOnlyHistory'
)
BEGIN
    EXEC(N'CREATE TRIGGER [dbo].[TR_AuditLogs_AppendOnly] ON [dbo].[AuditLogs] INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51002, ''Audit history is append-only.'', 1; END;');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063915_AppendOnlyHistory'
)
BEGIN
    EXEC(N'CREATE TRIGGER [dbo].[TR_InventoryTransactions_AppendOnly] ON [dbo].[InventoryTransactions] INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51003, ''Inventory history is append-only.'', 1; END;');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006063915_AppendOnlyHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006063915_AppendOnlyHistory', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064432_PosRedemptionConsentAndPriceTiers'
)
BEGIN
    ALTER TABLE [CartItems] ADD [Tier] nvarchar(40) NOT NULL DEFAULT N'Retail';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064432_PosRedemptionConsentAndPriceTiers'
)
BEGIN
    CREATE TABLE [RedemptionAuthorizations] (
        [Id] bigint NOT NULL IDENTITY,
        [MemberId] bigint NOT NULL,
        [PosRegisterId] bigint NOT NULL,
        [CodeHash] nvarchar(64) NOT NULL,
        [MaximumTokens] decimal(18,2) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [UsedOrderId] bigint NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RedemptionAuthorizations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RedemptionAuthorizations_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RedemptionAuthorizations_Orders_UsedOrderId] FOREIGN KEY ([UsedOrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RedemptionAuthorizations_PosRegisters_PosRegisterId] FOREIGN KEY ([PosRegisterId]) REFERENCES [PosRegisters] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064432_PosRedemptionConsentAndPriceTiers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RedemptionAuthorizations_CodeHash] ON [RedemptionAuthorizations] ([CodeHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064432_PosRedemptionConsentAndPriceTiers'
)
BEGIN
    CREATE INDEX [IX_RedemptionAuthorizations_MemberId] ON [RedemptionAuthorizations] ([MemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064432_PosRedemptionConsentAndPriceTiers'
)
BEGIN
    CREATE INDEX [IX_RedemptionAuthorizations_PosRegisterId] ON [RedemptionAuthorizations] ([PosRegisterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064432_PosRedemptionConsentAndPriceTiers'
)
BEGIN
    CREATE INDEX [IX_RedemptionAuthorizations_UsedOrderId] ON [RedemptionAuthorizations] ([UsedOrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064432_PosRedemptionConsentAndPriceTiers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006064432_PosRedemptionConsentAndPriceTiers', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064943_TaxAndRedemptionCategories'
)
BEGIN
    ALTER TABLE [TokenPolicies] ADD [RedemptionCategories] nvarchar(1000) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064943_TaxAndRedemptionCategories'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [Tax] decimal(18,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064943_TaxAndRedemptionCategories'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Key', N'Value') AND [object_id] = OBJECT_ID(N'[SystemSettings]'))
        SET IDENTITY_INSERT [SystemSettings] ON;
    EXEC(N'INSERT INTO [SystemSettings] ([Id], [Key], [Value])
    VALUES (CAST(11 AS bigint), N''TaxRatePercent'', N''0'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Key', N'Value') AND [object_id] = OBJECT_ID(N'[SystemSettings]'))
        SET IDENTITY_INSERT [SystemSettings] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064943_TaxAndRedemptionCategories'
)
BEGIN
    EXEC(N'UPDATE [TokenPolicies] SET [RedemptionCategories] = N''''
    WHERE [Id] = CAST(1 AS bigint);
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006064943_TaxAndRedemptionCategories'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006064943_TaxAndRedemptionCategories', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006075039_WarehouseOwnershipAndStockReports'
)
BEGIN
    ALTER TABLE [Warehouses] ADD [Kind] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006075039_WarehouseOwnershipAndStockReports'
)
BEGIN
    ALTER TABLE [Warehouses] ADD [OwnerMemberId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006075039_WarehouseOwnershipAndStockReports'
)
BEGIN
    EXEC(N'UPDATE [Warehouses] SET [Kind] = 0, [OwnerMemberId] = NULL
    WHERE [Id] = CAST(1 AS bigint);
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006075039_WarehouseOwnershipAndStockReports'
)
BEGIN
    CREATE INDEX [IX_Warehouses_OwnerMemberId_Kind] ON [Warehouses] ([OwnerMemberId], [Kind]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006075039_WarehouseOwnershipAndStockReports'
)
BEGIN
    EXEC(N'ALTER TABLE [Warehouses] ADD CONSTRAINT [CK_Warehouse_Owner] CHECK (([Kind] = 0 AND [OwnerMemberId] IS NULL) OR ([Kind] IN (1,2) AND [OwnerMemberId] IS NOT NULL))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006075039_WarehouseOwnershipAndStockReports'
)
BEGIN
    ALTER TABLE [Warehouses] ADD CONSTRAINT [FK_Warehouses_Members_OwnerMemberId] FOREIGN KEY ([OwnerMemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006075039_WarehouseOwnershipAndStockReports'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006075039_WarehouseOwnershipAndStockReports', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    CREATE TABLE [StockTransfers] (
        [Id] bigint NOT NULL IDENTITY,
        [RequestKey] uniqueidentifier NOT NULL,
        [SourceWarehouseId] bigint NOT NULL,
        [DestinationWarehouseId] bigint NOT NULL,
        [SkuId] bigint NOT NULL,
        [Quantity] int NOT NULL,
        [Status] int NOT NULL,
        [RequestedBy] nvarchar(450) NOT NULL,
        [DispatchedBy] nvarchar(450) NULL,
        [ReceivedBy] nvarchar(450) NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [Tracking] nvarchar(200) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DispatchedAt] datetime2 NULL,
        [ReceivedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_StockTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Transfer_Quantity] CHECK ([Quantity] > 0 AND [SourceWarehouseId] <> [DestinationWarehouseId]),
        CONSTRAINT [FK_StockTransfers_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransfers_Warehouses_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StockTransfers_Warehouses_SourceWarehouseId] FOREIGN KEY ([SourceWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    CREATE TABLE [TransferAllocations] (
        [Id] bigint NOT NULL IDENTITY,
        [StockTransferId] bigint NOT NULL,
        [SourceBatchId] bigint NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_TransferAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TransferAllocation_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_TransferAllocations_InventoryBatches_SourceBatchId] FOREIGN KEY ([SourceBatchId]) REFERENCES [InventoryBatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TransferAllocations_StockTransfers_StockTransferId] FOREIGN KEY ([StockTransferId]) REFERENCES [StockTransfers] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_DestinationWarehouseId_Status] ON [StockTransfers] ([DestinationWarehouseId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StockTransfers_RequestKey] ON [StockTransfers] ([RequestKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_SkuId] ON [StockTransfers] ([SkuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    CREATE INDEX [IX_StockTransfers_SourceWarehouseId] ON [StockTransfers] ([SourceWarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    CREATE INDEX [IX_TransferAllocations_SourceBatchId] ON [TransferAllocations] ([SourceBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TransferAllocations_StockTransferId_SourceBatchId] ON [TransferAllocations] ([StockTransferId], [SourceBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006091300_StockReplenishmentTransfers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006091300_StockReplenishmentTransfers', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE TABLE [Conversations] (
        [Id] bigint NOT NULL IDENTITY,
        [MemberId] bigint NOT NULL,
        [Subject] nvarchar(180) NOT NULL,
        [Status] int NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Conversations_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE TABLE [JournalEntries] (
        [Id] bigint NOT NULL IDENTITY,
        [EventKey] nvarchar(100) NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [OrderId] bigint NOT NULL,
        [PostedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_JournalEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JournalEntries_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE TABLE [MediaAssets] (
        [Id] bigint NOT NULL IDENTITY,
        [ProductId] bigint NULL,
        [Slot] int NOT NULL,
        [Purpose] nvarchar(30) NOT NULL,
        [Alt] nvarchar(200) NOT NULL,
        [ContentType] nvarchar(40) NOT NULL,
        [Data] varbinary(max) NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MediaAssets] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Media_Slot] CHECK ([ProductId] IS NULL OR [Slot] BETWEEN 1 AND 5),
        CONSTRAINT [FK_MediaAssets_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE TABLE [ConversationMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [ConversationId] bigint NOT NULL,
        [RequestKey] uniqueidentifier NOT NULL,
        [SenderUserId] nvarchar(450) NOT NULL,
        [FromStaff] bit NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [ReadAt] datetime2 NULL,
        CONSTRAINT [PK_ConversationMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConversationMessages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE TABLE [JournalLines] (
        [Id] bigint NOT NULL IDENTITY,
        [JournalEntryId] bigint NOT NULL,
        [Account] nvarchar(20) NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [Debit] decimal(18,2) NOT NULL,
        [Credit] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_JournalLines] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_JournalLine_Amount] CHECK (([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)),
        CONSTRAINT [FK_JournalLines_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [JournalEntries] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE TABLE [ContentPosts] (
        [Id] bigint NOT NULL IDENTITY,
        [Title] nvarchar(180) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [Link] nvarchar(300) NOT NULL,
        [Audience] int NOT NULL,
        [Kind] int NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [EndsAt] datetime2 NOT NULL,
        [Published] bit NOT NULL,
        [MediaAssetId] bigint NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ContentPosts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContentPosts_MediaAssets_MediaAssetId] FOREIGN KEY ([MediaAssetId]) REFERENCES [MediaAssets] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE INDEX [IX_ContentPosts_MediaAssetId] ON [ContentPosts] ([MediaAssetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE INDEX [IX_ContentPosts_Published_StartsAt_EndsAt] ON [ContentPosts] ([Published], [StartsAt], [EndsAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE INDEX [IX_ConversationMessages_ConversationId] ON [ConversationMessages] ([ConversationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ConversationMessages_RequestKey] ON [ConversationMessages] ([RequestKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE INDEX [IX_Conversations_MemberId_UpdatedAt] ON [Conversations] ([MemberId], [UpdatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JournalEntries_EventKey] ON [JournalEntries] ([EventKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE INDEX [IX_JournalEntries_OrderId] ON [JournalEntries] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    CREATE INDEX [IX_JournalLines_JournalEntryId] ON [JournalLines] ([JournalEntryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MediaAssets_ProductId_Slot] ON [MediaAssets] ([ProductId], [Slot]) WHERE [ProductId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    EXEC(N'CREATE TRIGGER [dbo].[TR_JournalEntries_AppendOnly] ON [dbo].[JournalEntries] INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51004, ''Sales journal is append-only.'', 1; END;');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    EXEC(N'CREATE TRIGGER [dbo].[TR_JournalLines_AppendOnly] ON [dbo].[JournalLines] INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51005, ''Sales journal lines are append-only.'', 1; END;');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006110637_DatabaseMediaContentMessagingAndSalesJournal', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE TABLE [CreditAccounts] (
        [Id] bigint NOT NULL IDENTITY,
        [MemberId] bigint NOT NULL,
        [Limit] decimal(18,2) NOT NULL,
        [TermDays] int NOT NULL,
        [Enabled] bit NOT NULL,
        [MasterDealer] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CreditAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CreditAccount_Limit] CHECK ([Limit] >= 0 AND [TermDays] BETWEEN 1 AND 365),
        CONSTRAINT [FK_CreditAccounts_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE TABLE [CreditInvoices] (
        [Id] bigint NOT NULL IDENTITY,
        [CreditAccountId] bigint NOT NULL,
        [OrderId] bigint NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Paid] decimal(18,2) NOT NULL,
        [Adjusted] decimal(18,2) NOT NULL,
        [IssuedAt] datetime2 NOT NULL,
        [DueAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CreditInvoices] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CreditInvoice_Balance] CHECK ([Amount] > 0 AND [Paid] >= 0 AND [Adjusted] >= 0 AND [Amount] >= [Paid] + [Adjusted]),
        CONSTRAINT [FK_CreditInvoices_CreditAccounts_CreditAccountId] FOREIGN KEY ([CreditAccountId]) REFERENCES [CreditAccounts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CreditInvoices_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE TABLE [CreditReceipts] (
        [Id] bigint NOT NULL IDENTITY,
        [CreditAccountId] bigint NOT NULL,
        [RequestKey] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransferredAt] datetime2 NOT NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [ReviewedAt] datetime2 NULL,
        [Reference] nvarchar(200) NOT NULL,
        [ConfirmedReference] nvarchar(200) NULL,
        [Note] nvarchar(1000) NOT NULL,
        [ReviewNote] nvarchar(1000) NOT NULL,
        [SubmittedBy] nvarchar(450) NOT NULL,
        [ReviewedBy] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [Slip] varbinary(max) NULL,
        [SlipType] nvarchar(40) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CreditReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CreditReceipt_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_CreditReceipts_CreditAccounts_CreditAccountId] FOREIGN KEY ([CreditAccountId]) REFERENCES [CreditAccounts] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE TABLE [CreditAllocations] (
        [Id] bigint NOT NULL IDENTITY,
        [CreditReceiptId] bigint NOT NULL,
        [CreditInvoiceId] bigint NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_CreditAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_CreditAllocation_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_CreditAllocations_CreditInvoices_CreditInvoiceId] FOREIGN KEY ([CreditInvoiceId]) REFERENCES [CreditInvoices] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CreditAllocations_CreditReceipts_CreditReceiptId] FOREIGN KEY ([CreditReceiptId]) REFERENCES [CreditReceipts] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditAccounts_MemberId] ON [CreditAccounts] ([MemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE INDEX [IX_CreditAllocations_CreditInvoiceId] ON [CreditAllocations] ([CreditInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditAllocations_CreditReceiptId_CreditInvoiceId] ON [CreditAllocations] ([CreditReceiptId], [CreditInvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE INDEX [IX_CreditInvoices_CreditAccountId_DueAt] ON [CreditInvoices] ([CreditAccountId], [DueAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditInvoices_OrderId] ON [CreditInvoices] ([OrderId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CreditReceipts_ConfirmedReference] ON [CreditReceipts] ([ConfirmedReference]) WHERE [ConfirmedReference] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE INDEX [IX_CreditReceipts_CreditAccountId] ON [CreditReceipts] ([CreditAccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditReceipts_RequestKey] ON [CreditReceipts] ([RequestKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    EXEC(N'CREATE TRIGGER TR_CreditAllocation_Immutable ON CreditAllocations AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51000, ''Posted credit allocations are immutable.'', 1; END')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    EXEC(N'CREATE TRIGGER TR_CreditReceipt_Reviewed ON CreditReceipts AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM deleted WHERE Status <> 0) THROW 51000, ''Reviewed credit receipts are immutable.'', 1; END')
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006121212_MemberTradeCredit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006121212_MemberTradeCredit', N'8.0.29');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    ALTER TABLE [Orders] ADD [StoreId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    ALTER TABLE [Orders] ADD [StoreName] nvarchar(160) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    ALTER TABLE [Carts] ADD [StoreId] bigint NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE TABLE [MemberStores] (
        [Id] bigint NOT NULL IDENTITY,
        [MemberId] bigint NOT NULL,
        [WarehouseId] bigint NOT NULL,
        [Slug] nvarchar(60) NOT NULL,
        [Name] nvarchar(160) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Phone] nvarchar(40) NOT NULL,
        [Published] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_MemberStores] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MemberStores_Members_MemberId] FOREIGN KEY ([MemberId]) REFERENCES [Members] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MemberStores_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE TABLE [StoreExpenses] (
        [Id] bigint NOT NULL IDENTITY,
        [StoreId] bigint NOT NULL,
        [RequestKey] uniqueidentifier NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [Reference] nvarchar(200) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ReversesId] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StoreExpenses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StoreExpenses_MemberStores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [MemberStores] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE TABLE [StoreProducts] (
        [Id] bigint NOT NULL IDENTITY,
        [StoreId] bigint NOT NULL,
        [SkuId] bigint NOT NULL,
        [Enabled] bit NOT NULL,
        CONSTRAINT [PK_StoreProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StoreProducts_MemberStores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [MemberStores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StoreProducts_Skus_SkuId] FOREIGN KEY ([SkuId]) REFERENCES [Skus] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE INDEX [IX_Orders_StoreId_CreatedAt] ON [Orders] ([StoreId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE INDEX [IX_Carts_StoreId] ON [Carts] ([StoreId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MemberStores_MemberId] ON [MemberStores] ([MemberId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MemberStores_Slug] ON [MemberStores] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE INDEX [IX_MemberStores_WarehouseId] ON [MemberStores] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_StoreExpenses_ReversesId] ON [StoreExpenses] ([ReversesId]) WHERE [ReversesId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StoreExpenses_StoreId_RequestKey] ON [StoreExpenses] ([StoreId], [RequestKey]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE INDEX [IX_StoreProducts_SkuId] ON [StoreProducts] ([SkuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StoreProducts_StoreId_SkuId] ON [StoreProducts] ([StoreId], [SkuId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    ALTER TABLE [Carts] ADD CONSTRAINT [FK_Carts_MemberStores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [MemberStores] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_MemberStores_StoreId] FOREIGN KEY ([StoreId]) REFERENCES [MemberStores] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    IF OBJECT_ID(N'dbo.AmHerbDemoBaseline_Orders', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.AmHerbDemoBaseline_Orders ADD StoreId bigint NULL, StoreName nvarchar(160) NOT NULL DEFAULT N'';
        ALTER TABLE dbo.AmHerbDemoBaseline_Carts ADD StoreId bigint NULL;
        SELECT TOP (0) * INTO dbo.AmHerbDemoBaseline_MemberStores FROM dbo.MemberStores;
        SELECT TOP (0) * INTO dbo.AmHerbDemoBaseline_StoreProducts FROM dbo.StoreProducts;
        SELECT TOP (0) * INTO dbo.AmHerbDemoBaseline_StoreExpenses FROM dbo.StoreExpenses;
    END;
    IF EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE [Key]=N'Demo.State')
    BEGIN
        INSERT INTO dbo.SystemSettings ([Key],[Value]) VALUES
            (N'Demo.Baseline.MemberStores',N'0'),(N'Demo.Baseline.StoreProducts',N'0'),(N'Demo.Baseline.StoreExpenses',N'0');
    END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007071642_MemberStores'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007071642_MemberStores', N'8.0.29');
END;
GO

COMMIT;
GO

