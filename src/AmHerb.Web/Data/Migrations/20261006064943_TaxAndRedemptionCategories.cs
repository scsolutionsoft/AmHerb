using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class TaxAndRedemptionCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RedemptionCategories",
                table: "TokenPolicies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "OrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Key", "Value" },
                values: new object[] { 11L, "TaxRatePercent", "0" });

            migrationBuilder.UpdateData(
                table: "TokenPolicies",
                keyColumn: "Id",
                keyValue: 1L,
                column: "RedemptionCategories",
                value: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 11L);

            migrationBuilder.DropColumn(
                name: "RedemptionCategories",
                table: "TokenPolicies");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "OrderItems");
        }
    }
}
