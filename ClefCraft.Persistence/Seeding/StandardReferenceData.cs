namespace ClefCraft.Persistence.Seeding
{
    /// <summary>
    /// The standard statuses and priorities every board offers, inserted by the Initial migration.
    ///
    /// Model: Status/Priority are shared value definitions with no BoardId. Which values a board
    /// offers comes from BoardStatus/BoardPriority rows, read by StatusRepository/PriorityRepository
    /// as "BoardId == board OR BoardId IS NULL":
    ///   BoardId IS NULL  -> available on every board (global)
    ///   BoardId = X      -> available on board X only
    /// For each standard name this creates the value definition and ONE global availability row.
    ///
    /// Plain SQL rather than HasData: the values get their ids from the identity columns, so the
    /// sequences advance and the application's first insert can't collide with a seeded id.
    ///
    /// Frozen: this is what the Initial migration applies to every new database. Changing it does
    /// not change existing databases, so a change to the reference data needs a new migration.
    /// </summary>
    public static class StandardReferenceData
    {
        public static readonly string[] StandardStatuses = { "Backlog", "To Do", "In Progress", "In Review", "Done" };
        public static readonly string[] StandardPriorities = { "Critical", "High", "Medium", "Low" };

        /// <summary>Creates the value definition when no definition with exactly this name exists.</summary>
        public static string InsertValueSql(string valueTable, string name) =>
            $"INSERT INTO \"{valueTable}\" (\"Name\", \"DateCreated\") " +
            $"SELECT '{name}', CURRENT_TIMESTAMP " +
            $"WHERE NOT EXISTS (SELECT 1 FROM \"{valueTable}\" WHERE \"Name\" = '{name}');";

        /// <summary>
        /// Adds the single global availability row for the value with this exact name. Only rows with
        /// BoardId IS NULL count as "already global"; board-specific rows are ignored.
        /// </summary>
        public static string InsertGlobalRowSql(string availabilityTable, string valueTable, string valueIdColumn, string name) =>
            $"INSERT INTO \"{availabilityTable}\" (\"BoardId\", \"{valueIdColumn}\", \"DateCreated\") " +
            $"SELECT NULL, v.\"Id\", CURRENT_TIMESTAMP FROM \"{valueTable}\" v " +
            $"WHERE v.\"Name\" = '{name}' " +
            $"AND NOT EXISTS (SELECT 1 FROM \"{availabilityTable}\" a WHERE a.\"BoardId\" IS NULL AND a.\"{valueIdColumn}\" = v.\"Id\");";

        /// <summary>Every statement, in execution order.</summary>
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
    }
}
