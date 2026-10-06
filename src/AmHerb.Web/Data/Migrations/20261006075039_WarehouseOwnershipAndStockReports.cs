using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class WarehouseOwnershipAndStockReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "OwnerMemberId",
                table: "Warehouses",
                type: "bigint",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Warehouses",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "Kind", "OwnerMemberId" },
                values: new object[] { 0, null });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_OwnerMemberId_Kind",
                table: "Warehouses",
                columns: new[] { "OwnerMemberId", "Kind" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Warehouse_Owner",
                table: "Warehouses",
                sql: "([Kind] = 0 AND [OwnerMemberId] IS NULL) OR ([Kind] IN (1,2) AND [OwnerMemberId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_Members_OwnerMemberId",
                table: "Warehouses",
                column: "OwnerMemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_Members_OwnerMemberId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_OwnerMemberId_Kind",
                table: "Warehouses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Warehouse_Owner",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OwnerMemberId",
                table: "Warehouses");
        }
    }
}
