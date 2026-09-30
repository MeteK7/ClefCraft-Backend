using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <summary>
    /// One-time data fix: makes the standard statuses and priorities available on every board.
    ///
    /// Model (unchanged): Status/Priority are shared value definitions with no BoardId. Which values
    /// a board offers comes from BoardStatus/BoardPriority rows, read by
    /// StatusRepository/PriorityRepository as "BoardId == board OR BoardId IS NULL":
    ///   BoardId IS NULL  -> available on every board (global)
    ///   BoardId = X      -> available on board X only
    /// Nothing seeded the values or any global row, so a database built from migrations has none,
    /// and boards without board-specific rows (every newly created board) offer no status or
    /// priority, which blocks creating items.
    ///
    /// For each standard name this:
    ///   1. creates the value definition if no definition with that exact name exists;
    ///   2. adds ONE global (BoardId IS NULL) availability row for it, if there isn't one.
    /// Board-specific rows are never used for matching, never changed and never copied: a
    /// board-specific "Done" does not stop the global "Done" from being created.
    ///
    /// Names have no unique constraint, so a name could be defined twice (or with other casing).
    /// Rather than silently picking one, a guard aborts the migration — rolling back its
    /// transaction — and names the ambiguous values, which then need a human decision.
    ///
    /// Self-contained on purpose: the names are fixed literals here, independent of application code.
    /// Idempotent: a re-run finds every value and global row and inserts nothing.
    /// </summary>
    public partial class AddGlobalStandardStatusesAndPriorities : Migration
    {
        public static readonly string[] StandardStatuses = { "Backlog", "To Do", "In Progress", "In Review", "Done" };
        public static readonly string[] StandardPriorities = { "Critical", "High", "Medium", "Low" };

        /// <summary>
        /// Portable query returning one row per standard name that is ambiguous in
        /// <paramref name="valueTable"/>: more than one definition when compared case-insensitively
        /// and ignoring surrounding spaces, or exactly one that isn't spelled exactly the same.
        /// No rows means every standard name has zero or exactly one exact definition.
        /// </summary>
        public static string AmbiguousNamesSql(string valueTable, IEnumerable<string> names) =>
            $"SELECT n.\"Name\" FROM ({NamesAsRows(names)}) n " +
            $"WHERE (SELECT COUNT(*) FROM \"{valueTable}\" v WHERE LOWER(TRIM(v.\"Name\")) = LOWER(n.\"Name\")) > 1 " +
            $"OR EXISTS (SELECT 1 FROM \"{valueTable}\" v WHERE LOWER(TRIM(v.\"Name\")) = LOWER(n.\"Name\") AND v.\"Name\" <> n.\"Name\")";

        /// <summary>Creates the value definition when no definition with exactly this name exists.</summary>
        public static string InsertValueSql(string valueTable, string name) =>
            $"INSERT INTO \"{valueTable}\" (\"Name\", \"DateCreated\") " +
            $"SELECT '{name}', CURRENT_TIMESTAMP " +
            $"WHERE NOT EXISTS (SELECT 1 FROM \"{valueTable}\" WHERE \"Name\" = '{name}');";

        /// <summary>
        /// Adds the single global availability row for the value with this exact name. Only rows with
        /// BoardId IS NULL count as "already global"; board-specific rows are ignored. After the
        /// guard exactly one value matches, so at most one row is inserted.
        /// </summary>
        public static string InsertGlobalRowSql(string availabilityTable, string valueTable, string valueIdColumn, string name) =>
            $"INSERT INTO \"{availabilityTable}\" (\"BoardId\", \"{valueIdColumn}\", \"DateCreated\") " +
            $"SELECT NULL, v.\"Id\", CURRENT_TIMESTAMP FROM \"{valueTable}\" v " +
            $"WHERE v.\"Name\" = '{name}' " +
            $"AND NOT EXISTS (SELECT 1 FROM \"{availabilityTable}\" a WHERE a.\"BoardId\" IS NULL AND a.\"{valueIdColumn}\" = v.\"Id\");";

        /// <summary>Everything after the guard, in execution order.</summary>
        public static IEnumerable<string> InsertStatements =>
            StandardStatuses.SelectMany(name => new[]
            {
                InsertValueSql("Statuses", name),
                InsertGlobalRowSql("BoardStatuses", "Statuses", "StatusId", name)
            })
            .Concat(StandardPriorities.SelectMany(name => new[]
            {
                InsertValueSql("Priorities", name),
                InsertGlobalRowSql("BoardPriorities", "Priorities", "PriorityId", name)
            }));

        // PostgreSQL wrapper around the portable detection queries: stop with a clear message.
        private static string GuardSql =>
            "DO $$ DECLARE ambiguous text; BEGIN " +
            $"SELECT string_agg(\"Name\", ', ') INTO ambiguous FROM ({AmbiguousNamesSql("Statuses", StandardStatuses)}) s; " +
            "IF ambiguous IS NOT NULL THEN RAISE EXCEPTION 'Ambiguous standard status definitions: %. Resolve the duplicate or differently spelled values before this migration.', ambiguous; END IF; " +
            $"SELECT string_agg(\"Name\", ', ') INTO ambiguous FROM ({AmbiguousNamesSql("Priorities", StandardPriorities)}) p; " +
            "IF ambiguous IS NOT NULL THEN RAISE EXCEPTION 'Ambiguous standard priority definitions: %. Resolve the duplicate or differently spelled values before this migration.', ambiguous; END IF; " +
            "END $$;";

        private static string NamesAsRows(IEnumerable<string> names) =>
            string.Join(" UNION ALL ", names.Select(name => $"SELECT '{name}' AS \"Name\""));

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(GuardSql);

            foreach (var statement in InsertStatements)
                migrationBuilder.Sql(statement);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: items may already use these values.
        }
    }
}
