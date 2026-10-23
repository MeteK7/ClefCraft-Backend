using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardItemAskAnswerReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnswerReplyId",
                table: "BoardItemAsks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemAsks_AnswerReplyId",
                table: "BoardItemAsks",
                column: "AnswerReplyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardItemAsks_BoardItemAskReplies_AnswerReplyId",
                table: "BoardItemAsks",
                column: "AnswerReplyId",
                principalTable: "BoardItemAskReplies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Asks already done keep the answer they showed until now: the recipient's latest reply
            // since the last reopen (State 3 = Completed).
            migrationBuilder.Sql(@"
UPDATE ""BoardItemAsks"" a SET ""AnswerReplyId"" = (
    SELECT r.""Id"" FROM ""BoardItemAskReplies"" r
    WHERE r.""BoardItemAskId"" = a.""Id""
      AND r.""AuthorUserId"" = a.""RecipientUserId""
      AND (a.""LastReopenedAt"" IS NULL OR r.""DateCreated"" > a.""LastReopenedAt"")
    ORDER BY r.""Id"" DESC
    LIMIT 1)
WHERE a.""State"" = 3;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardItemAsks_BoardItemAskReplies_AnswerReplyId",
                table: "BoardItemAsks");

            migrationBuilder.DropIndex(
                name: "IX_BoardItemAsks_AnswerReplyId",
                table: "BoardItemAsks");

            migrationBuilder.DropColumn(
                name: "AnswerReplyId",
                table: "BoardItemAsks");
        }
    }
}
