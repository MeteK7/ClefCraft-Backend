using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <summary>
    /// Board access, the assignee picker and the assignee membership check all go through
    /// BoardMembers rows; the owner has no implicit access. AddBoardMembers backfilled owners for
    /// the boards that existed then and CreateBoardCommandHandler adds one for new boards, but any
    /// board inserted another way (e.g. seed SQL) could be left without its owner as a member.
    /// Idempotent: boards whose owner is already a member are skipped, and so is any board with no
    /// owner, so a NULL can never fail this migration (which runs at API startup).
    /// </summary>
    public partial class BackfillBoardOwnerMemberships : Migration
    {
        // Standard SQL only (CURRENT_TIMESTAMP rather than NOW()) so the persistence tests can run
        // the exact same statement against SQLite.
        public const string BackfillSql =
            "INSERT INTO \"BoardMembers\" (\"BoardId\", \"UserId\", \"DateCreated\") " +
            "SELECT b.\"Id\", b.\"OwnerUserId\", CURRENT_TIMESTAMP FROM \"Boards\" b " +
            "WHERE b.\"OwnerUserId\" IS NOT NULL " +
            "AND NOT EXISTS (SELECT 1 FROM \"BoardMembers\" m " +
            "WHERE m.\"BoardId\" = b.\"Id\" AND m.\"UserId\" = b.\"OwnerUserId\");";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BackfillSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the added rows can't be told apart from owners added any other way.
        }
    }
}
