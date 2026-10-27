using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardItemAskCancelledAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "BoardItemAsks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUserId",
                table: "BoardItemAsks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Asks already cancelled take the time and actor of their latest ASK_CANCELLED history
            // entry, which every cancel has logged in the same save (State 4 = Cancelled).
            migrationBuilder.Sql(@"
UPDATE ""BoardItemAsks"" a SET ""CancelledAt"" = l.""Timestamp"", ""CancelledByUserId"" = l.""UserId""
FROM (
    SELECT DISTINCT ON (c.""AskId"") c.""AskId"", c.""Timestamp"", c.""UserId""
    FROM (
        SELECT (""MetadataJson""::jsonb ->> 'AskId')::int AS ""AskId"", ""Timestamp"", ""UserId""
        FROM ""ActivityLogs""
        WHERE ""EntityType"" = 'BoardItem' AND ""ActionType"" = 'ASK_CANCELLED' AND ""MetadataJson"" IS NOT NULL
    ) c
    WHERE c.""AskId"" IS NOT NULL
    ORDER BY c.""AskId"", c.""Timestamp"" DESC
) l
WHERE a.""State"" = 4 AND a.""CancelledAt"" IS NULL AND a.""Id"" = l.""AskId"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "BoardItemAsks");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "BoardItemAsks");
        }
    }
}
