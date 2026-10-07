using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrderSlipsAndDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Carrier",
                table: "Shipments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "AddressExtra",
                table: "Orders",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Orders",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SelectedCarrier",
                table: "Orders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SlipRequired",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubdistrictCode",
                table: "Orders",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VillageCode",
                table: "Orders",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VillageName",
                table: "Orders",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            // Existing unpaid online bank transfers also require proof; paid history is unchanged.
            migrationBuilder.Sql("UPDATE o SET SlipRequired = 1 FROM Orders o WHERE o.Channel = 0 AND o.Status = 1 AND o.CashPayable > 0 AND EXISTS (SELECT 1 FROM Payments p WHERE p.OrderId = o.Id AND p.Provider = 'ManualBankTransfer')");
            migrationBuilder.CreateTable(
                name: "PaymentSlips",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSlips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentSlips_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShippingProviders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoreShippingOptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    ShippingProviderId = table.Column<long>(type: "bigint", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreShippingOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreShippingOptions_MemberStores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "MemberStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoreShippingOptions_ShippingProviders_ShippingProviderId",
                        column: x => x.ShippingProviderId,
                        principalTable: "ShippingProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ShippingProviders",
                columns: new[] { "Id", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { 1L, "ไปรษณีย์ไทย", "THAILANDPOST" },
                    { 2L, "Kerry / KEX", "KEX" },
                    { 3L, "Flash Express", "FLASH" },
                    { 4L, "J&T Express", "JT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSlips_OrderId_Hash_Reference",
                table: "PaymentSlips",
                columns: new[] { "OrderId", "Hash", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingProviders_NormalizedName",
                table: "ShippingProviders",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreShippingOptions_ShippingProviderId",
                table: "StoreShippingOptions",
                column: "ShippingProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreShippingOptions_StoreId_ShippingProviderId",
                table: "StoreShippingOptions",
                columns: new[] { "StoreId", "ShippingProviderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentSlips");

            migrationBuilder.DropTable(
                name: "StoreShippingOptions");

            migrationBuilder.DropTable(
                name: "ShippingProviders");

            migrationBuilder.DropColumn(
                name: "AddressExtra",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SelectedCarrier",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SlipRequired",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SubdistrictCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VillageCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VillageName",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "Carrier",
                table: "Shipments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);
        }
    }
}
