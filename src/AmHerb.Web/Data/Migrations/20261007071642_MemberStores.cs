using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StoreId",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                table: "Orders",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "StoreId",
                table: "Carts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberStores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Published = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberStores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberStores_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberStores_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreExpenses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReversesId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreExpenses_MemberStores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "MemberStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoreProducts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    SkuId = table.Column<long>(type: "bigint", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreProducts_MemberStores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "MemberStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreProducts_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId_CreatedAt",
                table: "Orders",
                columns: new[] { "StoreId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_StoreId",
                table: "Carts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberStores_MemberId",
                table: "MemberStores",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberStores_Slug",
                table: "MemberStores",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberStores_WarehouseId",
                table: "MemberStores",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenses_ReversesId",
                table: "StoreExpenses",
                column: "ReversesId",
                unique: true,
                filter: "[ReversesId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StoreExpenses_StoreId_RequestKey",
                table: "StoreExpenses",
                columns: new[] { "StoreId", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreProducts_SkuId",
                table: "StoreProducts",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreProducts_StoreId_SkuId",
                table: "StoreProducts",
                columns: new[] { "StoreId", "SkuId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_MemberStores_StoreId",
                table: "Carts",
                column: "StoreId",
                principalTable: "MemberStores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_MemberStores_StoreId",
                table: "Orders",
                column: "StoreId",
                principalTable: "MemberStores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            // Keep an existing development demo baseline structurally compatible without changing its business data.
            migrationBuilder.Sql("""
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.AmHerbDemoBaseline_MemberStores', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE dbo.AmHerbDemoBaseline_StoreExpenses;
                    DROP TABLE dbo.AmHerbDemoBaseline_StoreProducts;
                    DROP TABLE dbo.AmHerbDemoBaseline_MemberStores;
                    ALTER TABLE dbo.AmHerbDemoBaseline_Carts DROP COLUMN StoreId;
                    ALTER TABLE dbo.AmHerbDemoBaseline_Orders DROP COLUMN StoreId;
                    DECLARE @constraint sysname;
                    SELECT @constraint=dc.name FROM sys.default_constraints dc
                    JOIN sys.columns c ON c.object_id=dc.parent_object_id AND c.column_id=dc.parent_column_id
                    WHERE dc.parent_object_id=OBJECT_ID(N'dbo.AmHerbDemoBaseline_Orders') AND c.name=N'StoreName';
                    IF @constraint IS NOT NULL
                    BEGIN
                        DECLARE @dropConstraint nvarchar(max)=N'ALTER TABLE dbo.AmHerbDemoBaseline_Orders DROP CONSTRAINT '+QUOTENAME(@constraint);
                        EXEC(@dropConstraint);
                    END;
                    ALTER TABLE dbo.AmHerbDemoBaseline_Orders DROP COLUMN StoreName;
                END;
                DELETE FROM dbo.SystemSettings WHERE [Key] IN (N'Demo.Baseline.MemberStores',N'Demo.Baseline.StoreProducts',N'Demo.Baseline.StoreExpenses');
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_MemberStores_StoreId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_MemberStores_StoreId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "StoreExpenses");

            migrationBuilder.DropTable(
                name: "StoreProducts");

            migrationBuilder.DropTable(
                name: "MemberStores");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StoreId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Carts_StoreId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Carts");
        }
    }
}
