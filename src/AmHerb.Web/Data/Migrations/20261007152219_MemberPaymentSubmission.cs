using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberPaymentSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedReference",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubmittedReference",
                table: "Payments");
        }
    }
}
