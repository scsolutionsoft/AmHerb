using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TransferPurchaseOrdersAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChargeAmount",
                table: "StockTransfers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "StockTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinancialApprovedAt",
                table: "StockTransfers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinancialApprovedBy",
                table: "StockTransfers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinancialNote",
                table: "StockTransfers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FinancialStatus",
                table: "StockTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "StockTransfers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "StockTransfers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PoNumber",
                table: "StockTransfers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PriceTier",
                table: "StockTransfers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "StockTransfers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "Notifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TransferPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTransferId = table.Column<long>(type: "bigint", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Slip = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SlipType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferPayments", x => x.Id);
                    table.CheckConstraint("CK_TransferPayment_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_TransferPayments_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("UPDATE [StockTransfers] SET [PoNumber] = CONCAT('PO-LEGACY-', RIGHT(CONCAT('00000000', [Id]), 8)), [PriceTier] = 'NoCharge', [FinancialStatus] = CASE WHEN [Status] = 3 THEN 7 ELSE 6 END");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_PoNumber",
                table: "StockTransfers",
                column: "PoNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transfer_Charge",
                table: "StockTransfers",
                sql: "[UnitPrice] >= 0 AND [ChargeAmount] >= 0 AND [PaidAmount] >= 0 AND [PaidAmount] <= [ChargeAmount]");

            migrationBuilder.CreateIndex(
                name: "IX_TransferPayments_RequestKey",
                table: "TransferPayments",
                column: "RequestKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferPayments_StockTransferId_Status",
                table: "TransferPayments",
                columns: new[] { "StockTransferId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferPayments");

            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_PoNumber",
                table: "StockTransfers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transfer_Charge",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ChargeAmount",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "FinancialApprovedAt",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "FinancialApprovedBy",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "FinancialNote",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "FinancialStatus",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "PoNumber",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "PriceTier",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "Link",
                table: "Notifications");
        }
    }
}
