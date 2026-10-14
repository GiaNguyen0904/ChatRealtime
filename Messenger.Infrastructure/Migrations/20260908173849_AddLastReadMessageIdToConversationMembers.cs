using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Messenger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastReadMessageIdToConversationMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastReadMessageId",
                table: "conversation_members",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversation_members_LastReadMessageId",
                table: "conversation_members",
                column: "LastReadMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_conversation_members_messages_LastReadMessageId",
                table: "conversation_members",
                column: "LastReadMessageId",
                principalTable: "messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversation_members_messages_LastReadMessageId",
                table: "conversation_members");

            migrationBuilder.DropIndex(
                name: "IX_conversation_members_LastReadMessageId",
                table: "conversation_members");

            migrationBuilder.DropColumn(
                name: "LastReadMessageId",
                table: "conversation_members");
        }
    }
}
