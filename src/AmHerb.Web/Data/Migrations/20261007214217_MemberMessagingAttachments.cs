using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberMessagingAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OtherMemberId",
                table: "Conversations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentContentType",
                table: "ConversationMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "AttachmentData",
                table: "ConversationMessages",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentName",
                table: "ConversationMessages",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_OtherMemberId_UpdatedAt",
                table: "Conversations",
                columns: new[] { "OtherMemberId", "UpdatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Members_OtherMemberId",
                table: "Conversations",
                column: "OtherMemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Members_OtherMemberId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_OtherMemberId_UpdatedAt",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "OtherMemberId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "AttachmentContentType",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentData",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentName",
                table: "ConversationMessages");
        }
    }
}
