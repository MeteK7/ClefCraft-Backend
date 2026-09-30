using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <summary>
    /// One-time data fix: boards created before CreateBoardCommandHandler added default columns
    /// have no columns at all, so no item can be created on them. Every board with ZERO
    /// BoardColumnMapping rows gets the five columns below, as new BoardColumn rows of its own
    /// (columns are per board, linked through BoardColumnMapping). Boards that already have any
    /// column are not touched.
    ///
    /// Deliberately a migration, not a startup routine: it runs exactly once, so columns a future
    /// management feature removes on purpose are never restored.
    ///
    /// Self-contained on purpose: the titles are fixed literals here, not read from the
    /// application's BoardColumnDefaults, so this historical change never moves if the
    /// application defaults change later.
    ///
    /// Each new column is tied to its board through a temporary "BackfillBoardId" column that is
    /// added, used for the mapping insert and dropped again — no audit column is borrowed and no
    /// value is parsed. Everything runs in the migration's transaction. All statements are plain
    /// SQL valid in PostgreSQL and SQLite (3.35+ for DROP COLUMN), so the tests run exactly this.
    /// Idempotent: afterwards every affected board has mappings, so a re-run matches no board.
    /// </summary>
    public partial class BackfillDefaultBoardColumns : Migration
    {
        public const string AddTemporaryColumnSql =
            "ALTER TABLE \"BoardColumns\" ADD COLUMN \"BackfillBoardId\" integer NULL;";

        // One statement per title, in lane order, so each board's new column ids ascend in that
        // order. Every title is inserted before any mapping exists, so each statement selects the
        // same boards: those with no BoardColumnMapping rows at all.
        public static readonly string[] InsertColumnsSql =
        {
            InsertColumnSql("Backlog"),
            InsertColumnSql("To Do"),
            InsertColumnSql("In Progress"),
            InsertColumnSql("In Review"),
            InsertColumnSql("Done")
        };

        public const string InsertMappingsSql =
            "INSERT INTO \"BoardColumnMappings\" (\"BoardId\", \"BoardColumnId\", \"DateCreated\") " +
            "SELECT c.\"BackfillBoardId\", c.\"Id\", CURRENT_TIMESTAMP FROM \"BoardColumns\" c " +
            "WHERE c.\"BackfillBoardId\" IS NOT NULL;";

        public const string DropTemporaryColumnSql =
            "ALTER TABLE \"BoardColumns\" DROP COLUMN \"BackfillBoardId\";";

        /// <summary>The whole backfill, in execution order.</summary>
        public static IEnumerable<string> AllStatements =>
            new[] { AddTemporaryColumnSql }
                .Concat(InsertColumnsSql)
                .Append(InsertMappingsSql)
                .Append(DropTemporaryColumnSql);

        private static string InsertColumnSql(string title) =>
            "INSERT INTO \"BoardColumns\" (\"Title\", \"DateCreated\", \"BackfillBoardId\") " +
            $"SELECT '{title}', CURRENT_TIMESTAMP, b.\"Id\" FROM \"Boards\" b " +
            "WHERE NOT EXISTS (SELECT 1 FROM \"BoardColumnMappings\" m WHERE m.\"BoardId\" = b.\"Id\") " +
            "ORDER BY b.\"Id\";";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in AllStatements)
                migrationBuilder.Sql(statement);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: the added columns may hold items by now.
        }
    }
}
