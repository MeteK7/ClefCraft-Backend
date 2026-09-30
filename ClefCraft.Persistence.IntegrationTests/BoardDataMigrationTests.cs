using ClefCraft.Persistence.Migrations;
using Microsoft.Data.Sqlite;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Runs the two item-7 data migrations' exact SQL against SQLite (no Postgres is available to the
    // tests), over minimal tables with the columns the statements touch. The Postgres-only RAISE
    // wrapper around the ambiguity check is covered by testing the portable detection query itself.
    public class BoardDataMigrationTests
    {
        private static SqliteConnection Open(string schemaAndData)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            Execute(connection, schemaAndData);
            return connection;
        }

        private static void Execute(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static List<string> Rows(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
                rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "null" : reader.GetValue(i).ToString())));
            return rows;
        }

        // ── BackfillDefaultBoardColumns ─────────────────────────────────────────────────────────

        private const string BoardSchema = @"
            CREATE TABLE ""Boards"" (""Id"" INTEGER PRIMARY KEY);
            CREATE TABLE ""BoardColumns"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""Title"" TEXT NOT NULL, ""DateCreated"" TEXT NULL, ""CreatedBy"" TEXT NULL);
            CREATE TABLE ""BoardColumnMappings"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""BoardId"" INTEGER NOT NULL, ""BoardColumnId"" INTEGER NOT NULL, ""DateCreated"" TEXT NULL);

            INSERT INTO ""Boards"" (""Id"") VALUES (1), (4), (5);
            -- Board 1 already has (custom) columns; boards 4 and 5 have none.
            INSERT INTO ""BoardColumns"" (""Id"", ""Title"", ""CreatedBy"") VALUES (1, 'Ideas', 'someone'), (2, 'Shipped', 'someone');
            INSERT INTO ""BoardColumnMappings"" (""BoardId"", ""BoardColumnId"") VALUES (1, 1), (1, 2);";

        private static void RunColumnBackfill(SqliteConnection connection)
        {
            foreach (var statement in BackfillDefaultBoardColumns.AllStatements)
                Execute(connection, statement);
        }

        private static List<string> ColumnTitlesInOrder(SqliteConnection connection, int boardId) =>
            Rows(connection,
                "SELECT c.\"Title\" FROM \"BoardColumns\" c JOIN \"BoardColumnMappings\" m ON m.\"BoardColumnId\" = c.\"Id\" " +
                $"WHERE m.\"BoardId\" = {boardId} ORDER BY c.\"Id\"");

        [Fact]
        public void ColumnBackfill_GivesEachBoardWithoutColumnsItsOwnFiveColumns_InLaneOrder()
        {
            using var db = Open(BoardSchema);

            RunColumnBackfill(db);

            var lanes = new List<string> { "Backlog", "To Do", "In Progress", "In Review", "Done" };
            ColumnTitlesInOrder(db, 4).ShouldBe(lanes);
            ColumnTitlesInOrder(db, 5).ShouldBe(lanes);

            // Each board got new rows of its own: no column is mapped to two boards.
            Rows(db, "SELECT \"BoardColumnId\" FROM \"BoardColumnMappings\" GROUP BY \"BoardColumnId\" HAVING COUNT(*) > 1").ShouldBeEmpty();
        }

        [Fact]
        public void ColumnBackfill_LeavesABoardThatHasColumnsUntouched()
        {
            using var db = Open(BoardSchema);

            RunColumnBackfill(db);

            ColumnTitlesInOrder(db, 1).ShouldBe(new List<string> { "Ideas", "Shipped" });
            Rows(db, "SELECT \"Id\", \"Title\", \"CreatedBy\" FROM \"BoardColumns\" WHERE \"Id\" IN (1, 2) ORDER BY \"Id\"")
                .ShouldBe(new List<string> { "1|Ideas|someone", "2|Shipped|someone" });
        }

        [Fact]
        public void ColumnBackfill_IsIdempotent_AndRemovesItsTemporaryColumn()
        {
            using var db = Open(BoardSchema);

            RunColumnBackfill(db);
            var afterFirstRun = Rows(db, "SELECT * FROM \"BoardColumnMappings\" ORDER BY \"Id\"");
            RunColumnBackfill(db);

            Rows(db, "SELECT * FROM \"BoardColumnMappings\" ORDER BY \"Id\"").ShouldBe(afterFirstRun);
            Rows(db, "SELECT COUNT(*) FROM \"BoardColumns\"").Single().ShouldBe("12"); // 2 existing + 2 boards × 5
            Rows(db, "SELECT name FROM pragma_table_info('BoardColumns')").ShouldNotContain("BackfillBoardId");
        }

        // ── AddGlobalStandardStatusesAndPriorities ──────────────────────────────────────────────

        private const string TaxonomySchema = @"
            CREATE TABLE ""Statuses"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""Name"" TEXT NOT NULL, ""DateCreated"" TEXT NULL);
            CREATE TABLE ""Priorities"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""Name"" TEXT NOT NULL, ""DateCreated"" TEXT NULL);
            CREATE TABLE ""BoardStatuses"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""BoardId"" INTEGER NULL, ""StatusId"" INTEGER NOT NULL, ""DateCreated"" TEXT NULL);
            CREATE TABLE ""BoardPriorities"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""BoardId"" INTEGER NULL, ""PriorityId"" INTEGER NOT NULL, ""DateCreated"" TEXT NULL);";

        private static void RunGlobalValues(SqliteConnection connection)
        {
            foreach (var statement in AddGlobalStandardStatusesAndPriorities.InsertStatements)
                Execute(connection, statement);
        }

        private static List<string> GlobalNames(SqliteConnection connection, string availability, string values, string idColumn) =>
            Rows(connection,
                $"SELECT v.\"Name\" FROM \"{availability}\" a JOIN \"{values}\" v ON v.\"Id\" = a.\"{idColumn}\" " +
                "WHERE a.\"BoardId\" IS NULL ORDER BY v.\"Id\"");

        [Fact]
        public void GlobalValues_OnAnEmptyDatabase_CreatesEveryStandardValueWithExactlyOneGlobalRow()
        {
            using var db = Open(TaxonomySchema);

            RunGlobalValues(db);

            GlobalNames(db, "BoardStatuses", "Statuses", "StatusId")
                .ShouldBe(new List<string> { "Backlog", "To Do", "In Progress", "In Review", "Done" });
            GlobalNames(db, "BoardPriorities", "Priorities", "PriorityId")
                .ShouldBe(new List<string> { "Critical", "High", "Medium", "Low" });
            Rows(db, "SELECT COUNT(*) FROM \"BoardStatuses\"").Single().ShouldBe("5");
            Rows(db, "SELECT COUNT(*) FROM \"BoardPriorities\"").Single().ShouldBe("4");
        }

        [Fact]
        public void GlobalValues_ABoardSpecificDone_DoesNotPreventTheGlobalDone_AndStaysUntouched()
        {
            using var db = Open(TaxonomySchema + @"
                INSERT INTO ""Statuses"" (""Id"", ""Name"") VALUES (5, 'Done');
                INSERT INTO ""BoardStatuses"" (""Id"", ""BoardId"", ""StatusId"", ""DateCreated"") VALUES (40, 7, 5, 'seeded');");

            RunGlobalValues(db);

            // The existing value is reused (not re-created by name)...
            Rows(db, "SELECT \"Id\" FROM \"Statuses\" WHERE \"Name\" = 'Done'").ShouldBe(new List<string> { "5" });
            // ...gets its own global row...
            Rows(db, "SELECT COUNT(*) FROM \"BoardStatuses\" WHERE \"BoardId\" IS NULL AND \"StatusId\" = 5").Single().ShouldBe("1");
            // ...and the board-specific row is exactly as it was.
            Rows(db, "SELECT \"Id\", \"BoardId\", \"StatusId\", \"DateCreated\" FROM \"BoardStatuses\" WHERE \"BoardId\" IS NOT NULL")
                .ShouldBe(new List<string> { "40|7|5|seeded" });
        }

        [Fact]
        public void GlobalValues_NeverDuplicateAGlobalRow_IncludingOnASecondRun()
        {
            using var db = Open(TaxonomySchema + @"
                INSERT INTO ""Priorities"" (""Id"", ""Name"") VALUES (2, 'High');
                INSERT INTO ""BoardPriorities"" (""BoardId"", ""PriorityId"") VALUES (NULL, 2);");

            RunGlobalValues(db);
            RunGlobalValues(db);

            Rows(db, "SELECT \"PriorityId\", COUNT(*) FROM \"BoardPriorities\" WHERE \"BoardId\" IS NULL GROUP BY \"PriorityId\" HAVING COUNT(*) > 1").ShouldBeEmpty();
            Rows(db, "SELECT \"StatusId\", COUNT(*) FROM \"BoardStatuses\" WHERE \"BoardId\" IS NULL GROUP BY \"StatusId\" HAVING COUNT(*) > 1").ShouldBeEmpty();
            Rows(db, "SELECT COUNT(*) FROM \"Priorities\" WHERE \"Name\" = 'High'").Single().ShouldBe("1");
            Rows(db, "SELECT COUNT(*) FROM \"BoardPriorities\"").Single().ShouldBe("4");
        }

        [Fact]
        public void AmbiguityCheck_ReportsDuplicatesAndDifferentSpellings_ButNotCleanOrNonStandardNames()
        {
            using var db = Open(TaxonomySchema + @"
                INSERT INTO ""Statuses"" (""Name"") VALUES ('Done'), ('Done'), ('backlog'), ('To Do'), ('Blocked'), ('Blocked');");

            var ambiguous = Rows(db, AddGlobalStandardStatusesAndPriorities.AmbiguousNamesSql(
                "Statuses", AddGlobalStandardStatusesAndPriorities.StandardStatuses));

            // Two "Done" definitions and a lower-case "backlog" are ambiguous; a single exact "To Do"
            // and the non-standard (even duplicated) "Blocked" are not the migration's concern.
            ambiguous.OrderBy(n => n).ShouldBe(new List<string> { "Backlog", "Done" });
        }

        [Fact]
        public void AmbiguityCheck_ReportsNothingOnCleanData()
        {
            using var db = Open(TaxonomySchema + @"
                INSERT INTO ""Statuses"" (""Name"") VALUES ('Backlog'), ('To Do'), ('In Progress'), ('In Review'), ('Done');
                INSERT INTO ""Priorities"" (""Name"") VALUES ('Critical'), ('High'), ('Medium'), ('Low');");

            Rows(db, AddGlobalStandardStatusesAndPriorities.AmbiguousNamesSql("Statuses", AddGlobalStandardStatusesAndPriorities.StandardStatuses)).ShouldBeEmpty();
            Rows(db, AddGlobalStandardStatusesAndPriorities.AmbiguousNamesSql("Priorities", AddGlobalStandardStatusesAndPriorities.StandardPriorities)).ShouldBeEmpty();
        }
    }
}
