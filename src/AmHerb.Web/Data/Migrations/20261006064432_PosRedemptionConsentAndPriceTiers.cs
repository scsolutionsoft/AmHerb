using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PosRedemptionConsentAndPriceTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "CartItems",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Retail");

            migrationBuilder.CreateTable(
                name: "RedemptionAuthorizations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    PosRegisterId = table.Column<long>(type: "bigint", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MaximumTokens = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedOrderId = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedemptionAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedemptionAuthorizations_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RedemptionAuthorizations_Orders_UsedOrderId",
                        column: x => x.UsedOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RedemptionAuthorizations_PosRegisters_PosRegisterId",
                        column: x => x.PosRegisterId,
                        principalTable: "PosRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RedemptionAuthorizations_CodeHash",
                table: "RedemptionAuthorizations",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RedemptionAuthorizations_MemberId",
                table: "RedemptionAuthorizations",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_RedemptionAuthorizations_PosRegisterId",
                table: "RedemptionAuthorizations",
                column: "PosRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_RedemptionAuthorizations_UsedOrderId",
                table: "RedemptionAuthorizations",
                column: "UsedOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RedemptionAuthorizations");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "CartItems");
        }
    }
}
