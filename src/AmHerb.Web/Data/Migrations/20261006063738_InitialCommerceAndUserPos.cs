using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommerceAndUserPos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    AllowNetworkReward = table.Column<bool>(type: "bit", nullable: false),
                    AllowRedemption = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EventKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Read = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Distributor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Warnings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LabelDocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    LotRequired = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Day = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Orders = table.Column<int>(type: "int", nullable: false),
                    Sales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TokenLiability = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FreeAbove = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenPolicies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PendingDays = table.Column<int>(type: "int", nullable: false),
                    ExpiryMonths = table.Column<int>(type: "int", nullable: false),
                    SellerPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Level1Percent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Level2Percent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Level3Percent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxDepth = table.Column<int>(type: "int", nullable: false),
                    RedemptionReferenceThb = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxRedemptionPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LoyaltyEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LoyaltyPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReferralCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SponsorMemberId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ReviewRequired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Members_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Members_Members_SponsorMemberId",
                        column: x => x.SponsorMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Skus",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Variant = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CapsuleCount = table.Column<int>(type: "int", nullable: true),
                    WeightGrams = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    TokenEligible = table.Column<bool>(type: "bit", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LowStockThreshold = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skus_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosRegisters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosRegisters_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosRegisters_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberClosures",
                columns: table => new
                {
                    AncestorMemberId = table.Column<long>(type: "bigint", nullable: false),
                    DescendantMemberId = table.Column<long>(type: "bigint", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberClosures", x => new { x.AncestorMemberId, x.DescendantMemberId });
                    table.ForeignKey(
                        name: "FK_MemberClosures_Members_AncestorMemberId",
                        column: x => x.AncestorMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberClosures_Members_DescendantMemberId",
                        column: x => x.DescendantMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<long>(type: "bigint", nullable: false),
                    SkuId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CartItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    SkuId = table.Column<long>(type: "bigint", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MfgDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QtyReceived = table.Column<int>(type: "int", nullable: false),
                    QtyAvailable = table.Column<int>(type: "int", nullable: false),
                    QtyReserved = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBatches", x => x.Id);
                    table.CheckConstraint("CK_Batch_Quantities", "[QtyAvailable] >= 0 AND [QtyReserved] >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryBatches_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductPrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkuId = table.Column<long>(type: "bigint", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PackQuantity = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPrices_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumSpend = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RequiredQuantity = table.Column<int>(type: "int", nullable: false),
                    EligibleSkuId = table.Column<long>(type: "bigint", nullable: true),
                    GiftSkuId = table.Column<long>(type: "bigint", nullable: true),
                    Channel = table.Column<int>(type: "int", nullable: true),
                    MemberType = table.Column<int>(type: "int", nullable: true),
                    UsageLimit = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenMultiplier = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Promotions_Skus_EligibleSkuId",
                        column: x => x.EligibleSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Promotions_Skus_GiftSkuId",
                        column: x => x.GiftSkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokenRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkuId = table.Column<long>(type: "bigint", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: true),
                    BaseToken = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenRates_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PosRegisterId = table.Column<long>(type: "bigint", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CountedCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosSessions_PosRegisters_PosRegisterId",
                        column: x => x.PosRegisterId,
                        principalTable: "PosRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BuyerMemberId = table.Column<long>(type: "bigint", nullable: true),
                    SellerMemberId = table.Column<long>(type: "bigint", nullable: true),
                    CashierUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PosSessionId = table.Column<long>(type: "bigint", nullable: true),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    ExternalOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Campaign = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Creator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerifiedRetailSale = table.Column<bool>(type: "bit", nullable: false),
                    StockLoading = table.Column<bool>(type: "bit", nullable: false),
                    RewardsCreated = table.Column<bool>(type: "bit", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Merchandise = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Shipping = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TokenRedemption = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TokenValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashPayable = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ChannelFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AffiliateFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TokenPolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    PromotionId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Members_BuyerMemberId",
                        column: x => x.BuyerMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Members_SellerMemberId",
                        column: x => x.SellerMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_PosSessions_PosSessionId",
                        column: x => x.PosSessionId,
                        principalTable: "PosSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryBatchId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InventoryBatches_InventoryBatchId",
                        column: x => x.InventoryBatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    SkuId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReturnedQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TokenValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BaseToken = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TokenMultiplier = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TokenRateVersion = table.Column<long>(type: "bigint", nullable: false),
                    PriceVersion = table.Column<long>(type: "bigint", nullable: false),
                    IsGift = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.CheckConstraint("CK_Item_Quantities", "[Quantity] > 0 AND [ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionRef = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WebhookHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tendered = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReferralVisits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Campaign = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LandingPage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConvertedOrderId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralVisits_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReferralVisits_Orders_ConvertedOrderId",
                        column: x => x.ConvertedOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Sellable = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CashRefund = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TokensRestored = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    InventoryBatchId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReturnedQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAllocations_InventoryBatches_InventoryBatchId",
                        column: x => x.InventoryBatchId,
                        principalTable: "InventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockAllocations_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokenDistributions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    SourceOrderId = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReversedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpiredAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantityBasis = table.Column<int>(type: "int", nullable: false),
                    ReturnedAtIssue = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RewardPolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    TokenRateVersion = table.Column<long>(type: "bigint", nullable: false),
                    ReleaseAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenDistributions_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenDistributions_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenDistributions_Orders_SourceOrderId",
                        column: x => x.SourceOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokenConsumptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistributionId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenConsumptions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenConsumptions_TokenDistributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "TokenDistributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokenLedger",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    SourceOrderId = table.Column<long>(type: "bigint", nullable: true),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: true),
                    DistributionId = table.Column<long>(type: "bigint", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PendingDelta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AvailableDelta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReservedDelta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RewardPolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    TokenRateVersion = table.Column<long>(type: "bigint", nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenLedger_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenLedger_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenLedger_Orders_SourceOrderId",
                        column: x => x.SourceOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TokenLedger_TokenDistributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "TokenDistributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ChannelPolicies",
                columns: new[] { "Id", "AllowNetworkReward", "AllowRedemption", "Channel" },
                values: new object[,]
                {
                    { 1L, true, true, 0 },
                    { 2L, true, true, 1 },
                    { 3L, true, false, 2 },
                    { 4L, false, false, 3 },
                    { 5L, false, false, 4 },
                    { 6L, false, false, 5 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Active", "Category", "Description", "Distributor", "ImageUrl", "LabelDocumentUrl", "LotRequired", "Manufacturer", "Name", "RegistrationNumber", "Warnings" },
                values: new object[,]
                {
                    { 1L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "FOREVA", "", "อ่านฉลากและคำเตือนก่อนบริโภค" },
                    { 2L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "CELL-SYNC", "", "อ่านฉลากและคำเตือนก่อนบริโภค" },
                    { 3L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "The Ruby", "", "อ่านฉลากและคำเตือนก่อนบริโภค" },
                    { 4L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "Detoxify Blue", "", "อ่านฉลากและคำเตือนก่อนบริโภค" },
                    { 5L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "PHYTOSYNC", "", "อ่านฉลากและคำเตือนก่อนบริโภค" },
                    { 6L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "Anti Neo Plus", "", "อ่านฉลากและคำเตือนก่อนบริโภค" },
                    { 7L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "Reliva", "", "อ่านฉลากและคำเตือนก่อนบริโภค" },
                    { 8L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "Reliva Max", "", "อ่านฉลากและคำเตือนก่อนบริโภค" },
                    { 9L, true, "สมุนไพรและผลิตภัณฑ์เสริมอาหาร", "", "AM HERB", "", "", true, "", "FEEL GOOD CHAMANG", "", "อ่านฉลากและคำเตือนก่อนบริโภค" }
                });

            migrationBuilder.InsertData(
                table: "ShippingRules",
                columns: new[] { "Id", "Active", "Fee", "FreeAbove", "Name" },
                values: new object[] { 1L, true, 50m, 1500m, "จัดส่งมาตรฐาน" });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[,]
                {
                    { 1L, "RewardPendingDays", "14" },
                    { 2L, "MaxUplineRewardDepth", "3" },
                    { 3L, "TokenRedemptionReferenceTHB", "1.00" },
                    { 4L, "TokenTransferEnabled", "false" },
                    { 5L, "TokenCashWithdrawalEnabled", "false" },
                    { 6L, "TokenExpiryMonths", "12" },
                    { 7L, "MaxTokenRedemptionPercent", "100" },
                    { 8L, "ShippingRedeemableWithToken", "false" },
                    { 9L, "ReferralCookieDays", "30" },
                    { 10L, "AbandonedCheckoutHours", "24" }
                });

            migrationBuilder.InsertData(
                table: "TokenPolicies",
                columns: new[] { "Id", "EffectiveFrom", "ExpiryMonths", "Level1Percent", "Level2Percent", "Level3Percent", "LoyaltyEnabled", "LoyaltyPercent", "MaxDepth", "MaxRedemptionPercent", "PendingDays", "Reason", "RedemptionReferenceThb", "SellerPercent" },
                values: new object[] { 1L, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 12, 20m, 7m, 3m, false, 0m, 3, 100m, 14, "Initial policy", 1m, 100m });

            migrationBuilder.InsertData(
                table: "Warehouses",
                columns: new[] { "Id", "Active", "Name" },
                values: new object[] { 1L, true, "คลังกลาง AM HERB" });

            migrationBuilder.InsertData(
                table: "Skus",
                columns: new[] { "Id", "Active", "Barcode", "CapsuleCount", "Code", "Cost", "LowStockThreshold", "ProductId", "TokenEligible", "Variant", "WeightGrams" },
                values: new object[,]
                {
                    { 1L, true, "FOREVA", null, "FOREVA", 0m, 10, 1L, true, "มาตรฐาน", null },
                    { 2L, true, "CELL-SYNC", null, "CELL-SYNC", 0m, 10, 2L, true, "มาตรฐาน", null },
                    { 3L, true, "RUBY", null, "RUBY", 0m, 10, 3L, true, "มาตรฐาน", null },
                    { 4L, true, "DETOXIFY-BLUE", null, "DETOXIFY-BLUE", 0m, 10, 4L, true, "มาตรฐาน", null },
                    { 5L, true, "PHYTOSYNC", null, "PHYTOSYNC", 0m, 10, 5L, true, "มาตรฐาน", null },
                    { 6L, true, "ANTI-NEO-PLUS", null, "ANTI-NEO-PLUS", 0m, 10, 6L, true, "มาตรฐาน", null },
                    { 7L, true, "RELIVA", null, "RELIVA", 0m, 10, 7L, true, "มาตรฐาน", null },
                    { 8L, true, "RELIVA-MAX", null, "RELIVA-MAX", 0m, 10, 8L, true, "มาตรฐาน", null },
                    { 9L, true, "FEEL-GOOD", null, "FEEL-GOOD", 0m, 10, 9L, true, "มาตรฐาน", null }
                });

            migrationBuilder.InsertData(
                table: "ProductPrices",
                columns: new[] { "Id", "Amount", "Channel", "EffectiveFrom", "EffectiveTo", "PackQuantity", "SkuId", "Tier" },
                values: new object[,]
                {
                    { 1L, 790m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 1L, "Retail" },
                    { 2L, 690m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 1L, "Promo" },
                    { 3L, 495m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 1L, "Wholesale" },
                    { 4L, 2821.50m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, 1L, "Pack6" },
                    { 5L, 5400m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 1L, "Pack12" },
                    { 6L, 990m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 2L, "Retail" },
                    { 7L, 890m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 2L, "Promo" },
                    { 8L, 605m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 2L, "Wholesale" },
                    { 9L, 3448.50m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, 2L, "Pack6" },
                    { 10L, 6600m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 2L, "Pack12" },
                    { 11L, 1490m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 3L, "Retail" },
                    { 12L, 1290m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 3L, "Promo" },
                    { 13L, 935m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 3L, "Wholesale" },
                    { 14L, 5329.50m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, 3L, "Pack6" },
                    { 15L, 10200m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 3L, "Pack12" },
                    { 16L, 1090m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 4L, "Retail" },
                    { 17L, 690m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 5L, "Retail" },
                    { 18L, 590m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 5L, "Promo" },
                    { 19L, 418m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 5L, "Wholesale" },
                    { 20L, 2382.60m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, 5L, "Pack6" },
                    { 21L, 4560m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 5L, "Pack12" },
                    { 22L, 1990m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 6L, "Retail" },
                    { 23L, 1790m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 6L, "Promo" },
                    { 24L, 1320m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 6L, "Wholesale" },
                    { 25L, 7500m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, 6L, "Pack6" },
                    { 26L, 14640m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 6L, "Pack12" },
                    { 27L, 249m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 7L, "Retail" },
                    { 28L, 229m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 7L, "Promo" },
                    { 29L, 161m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 7L, "Wholesale" },
                    { 30L, 966m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, 7L, "Pack6" },
                    { 31L, 1932m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 7L, "Pack12" },
                    { 32L, 299m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 8L, "Retail" },
                    { 33L, 279m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 8L, "Promo" },
                    { 34L, 195.50m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 8L, "Wholesale" },
                    { 35L, 1173m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, 8L, "Pack6" },
                    { 36L, 2346m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 8L, "Pack12" },
                    { 37L, 299m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 9L, "Retail" },
                    { 38L, 249m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 9L, "Promo" },
                    { 39L, 180m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 9L, "Wholesale" },
                    { 40L, 1020m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6, 9L, "Pack6" },
                    { 41L, 1980m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 9L, "Pack12" }
                });

            migrationBuilder.InsertData(
                table: "TokenRates",
                columns: new[] { "Id", "BaseToken", "Channel", "EffectiveFrom", "EffectiveTo", "SkuId" },
                values: new object[,]
                {
                    { 1L, 60m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1L },
                    { 2L, 90m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 2L },
                    { 3L, 110m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 3L },
                    { 4L, 80m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4L },
                    { 5L, 50m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 5L },
                    { 6L, 140m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 6L },
                    { 7L, 20m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 7L },
                    { 8L, 25m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 8L },
                    { 9L, 20m, null, new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 9L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_SkuId",
                table: "CartItems",
                columns: new[] { "CartId", "SkuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_SkuId",
                table: "CartItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_PublicId",
                table: "Carts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelPolicies_Channel",
                table: "ChannelPolicies",
                column: "Channel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_SkuId_ExpDate",
                table: "InventoryBatches",
                columns: new[] { "SkuId", "ExpDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryBatches_WarehouseId_SkuId_LotNo",
                table: "InventoryBatches",
                columns: new[] { "WarehouseId", "SkuId", "LotNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_InventoryBatchId",
                table: "InventoryTransactions",
                column: "InventoryBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_OrderId",
                table: "InventoryTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberClosures_DescendantMemberId_Depth",
                table: "MemberClosures",
                columns: new[] { "DescendantMemberId", "Depth" });

            migrationBuilder.CreateIndex(
                name: "IX_Members_Code",
                table: "Members",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_ReferralCode",
                table: "Members",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_SponsorMemberId",
                table: "Members",
                column: "SponsorMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_UserId",
                table: "Members",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_EventKey",
                table: "Notifications",
                column: "EventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SkuId",
                table: "OrderItems",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BuyerMemberId",
                table: "Orders",
                column: "BuyerMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CashierUserId_CreatedAt",
                table: "Orders",
                columns: new[] { "CashierUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Channel_ExternalOrderId",
                table: "Orders",
                columns: new[] { "Channel", "ExternalOrderId" },
                unique: true,
                filter: "[ExternalOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IdempotencyKey",
                table: "Orders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Number",
                table: "Orders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PosSessionId",
                table: "Orders",
                column: "PosSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PromotionId",
                table: "Orders",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PublicId",
                table: "Orders",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SellerMemberId",
                table: "Orders",
                column: "SellerMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_TransactionRef",
                table: "Payments",
                columns: new[] { "Provider", "TransactionRef" },
                unique: true,
                filter: "[TransactionRef] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PosRegisters_UserId",
                table: "PosRegisters",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosRegisters_WarehouseId",
                table: "PosRegisters",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSessions_PosRegisterId",
                table: "PosSessions",
                column: "PosRegisterId",
                unique: true,
                filter: "[ClosedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_SkuId_Tier_Channel_EffectiveFrom",
                table: "ProductPrices",
                columns: new[] { "SkuId", "Tier", "Channel", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Code",
                table: "Promotions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_EligibleSkuId",
                table: "Promotions",
                column: "EligibleSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_GiftSkuId",
                table: "Promotions",
                column: "GiftSkuId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralVisits_ConvertedOrderId",
                table: "ReferralVisits",
                column: "ConvertedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralVisits_MemberId",
                table: "ReferralVisits",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralVisits_PublicId",
                table: "ReferralVisits",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportSnapshots_Day",
                table: "ReportSnapshots",
                column: "Day",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_OrderItemId",
                table: "ReturnRequests",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Skus_Code",
                table: "Skus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skus_ProductId",
                table: "Skus",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_InventoryBatchId",
                table: "StockAllocations",
                column: "InventoryBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockAllocations_OrderItemId",
                table: "StockAllocations",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenConsumptions_DistributionId_OrderId",
                table: "TokenConsumptions",
                columns: new[] { "DistributionId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenConsumptions_OrderId",
                table: "TokenConsumptions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenDistributions_MemberId",
                table: "TokenDistributions",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenDistributions_OrderItemId_MemberId_Level",
                table: "TokenDistributions",
                columns: new[] { "OrderItemId", "MemberId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenDistributions_SourceOrderId",
                table: "TokenDistributions",
                column: "SourceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenDistributions_Status_ReleaseAt",
                table: "TokenDistributions",
                columns: new[] { "Status", "ReleaseAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenLedger_DistributionId",
                table: "TokenLedger",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenLedger_EventKey",
                table: "TokenLedger",
                column: "EventKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenLedger_MemberId_PostedAt",
                table: "TokenLedger",
                columns: new[] { "MemberId", "PostedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenLedger_OrderItemId",
                table: "TokenLedger",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenLedger_SourceOrderId",
                table: "TokenLedger",
                column: "SourceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenPolicies_EffectiveFrom",
                table: "TokenPolicies",
                column: "EffectiveFrom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenRates_SkuId_Channel_EffectiveFrom",
                table: "TokenRates",
                columns: new[] { "SkuId", "Channel", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "ChannelPolicies");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "MemberClosures");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "ProductPrices");

            migrationBuilder.DropTable(
                name: "ReferralVisits");

            migrationBuilder.DropTable(
                name: "ReportSnapshots");

            migrationBuilder.DropTable(
                name: "ReturnRequests");

            migrationBuilder.DropTable(
                name: "Shipments");

            migrationBuilder.DropTable(
                name: "ShippingRules");

            migrationBuilder.DropTable(
                name: "StockAllocations");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TokenConsumptions");

            migrationBuilder.DropTable(
                name: "TokenLedger");

            migrationBuilder.DropTable(
                name: "TokenPolicies");

            migrationBuilder.DropTable(
                name: "TokenRates");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "InventoryBatches");

            migrationBuilder.DropTable(
                name: "TokenDistributions");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "PosSessions");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "PosRegisters");

            migrationBuilder.DropTable(
                name: "Skus");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
