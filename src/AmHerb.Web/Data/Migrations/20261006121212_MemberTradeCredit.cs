using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberTradeCredit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    Limit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TermDays = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    MasterDealer = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAccounts", x => x.Id);
                    table.CheckConstraint("CK_CreditAccount_Limit", "[Limit] >= 0 AND [TermDays] BETWEEN 1 AND 365");
                    table.ForeignKey(
                        name: "FK_CreditAccounts_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditInvoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditAccountId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Paid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Adjusted = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditInvoices", x => x.Id);
                    table.CheckConstraint("CK_CreditInvoice_Balance", "[Amount] > 0 AND [Paid] >= 0 AND [Adjusted] >= 0 AND [Amount] >= [Paid] + [Adjusted]");
                    table.ForeignKey(
                        name: "FK_CreditInvoices_CreditAccounts_CreditAccountId",
                        column: x => x.CreditAccountId,
                        principalTable: "CreditAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditInvoices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditReceipts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditAccountId = table.Column<long>(type: "bigint", nullable: false),
                    RequestKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransferredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConfirmedReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Slip = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SlipType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditReceipts", x => x.Id);
                    table.CheckConstraint("CK_CreditReceipt_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_CreditReceipts_CreditAccounts_CreditAccountId",
                        column: x => x.CreditAccountId,
                        principalTable: "CreditAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditReceiptId = table.Column<long>(type: "bigint", nullable: false),
                    CreditInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditAllocations", x => x.Id);
                    table.CheckConstraint("CK_CreditAllocation_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_CreditAllocations_CreditInvoices_CreditInvoiceId",
                        column: x => x.CreditInvoiceId,
                        principalTable: "CreditInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditAllocations_CreditReceipts_CreditReceiptId",
                        column: x => x.CreditReceiptId,
                        principalTable: "CreditReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditAccounts_MemberId",
                table: "CreditAccounts",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditAllocations_CreditInvoiceId",
                table: "CreditAllocations",
                column: "CreditInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditAllocations_CreditReceiptId_CreditInvoiceId",
                table: "CreditAllocations",
                columns: new[] { "CreditReceiptId", "CreditInvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditInvoices_CreditAccountId_DueAt",
                table: "CreditInvoices",
                columns: new[] { "CreditAccountId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditInvoices_OrderId",
                table: "CreditInvoices",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditReceipts_ConfirmedReference",
                table: "CreditReceipts",
                column: "ConfirmedReference",
                unique: true,
                filter: "[ConfirmedReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CreditReceipts_CreditAccountId",
                table: "CreditReceipts",
                column: "CreditAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditReceipts_RequestKey",
                table: "CreditReceipts",
                column: "RequestKey",
                unique: true);
            migrationBuilder.Sql("EXEC(N'CREATE TRIGGER TR_CreditAllocation_Immutable ON CreditAllocations AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51000, ''Posted credit allocations are immutable.'', 1; END')");
            migrationBuilder.Sql("EXEC(N'CREATE TRIGGER TR_CreditReceipt_Reviewed ON CreditReceipts AFTER UPDATE, DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS (SELECT 1 FROM deleted WHERE Status <> 0) THROW 51000, ''Reviewed credit receipts are immutable.'', 1; END')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditAllocations");

            migrationBuilder.DropTable(
                name: "CreditInvoices");

            migrationBuilder.DropTable(
                name: "CreditReceipts");

            migrationBuilder.DropTable(
                name: "CreditAccounts");
        }
    }
}
