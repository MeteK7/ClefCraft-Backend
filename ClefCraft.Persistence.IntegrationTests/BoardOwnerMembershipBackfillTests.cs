using ClefCraft.Persistence.Migrations;
using Microsoft.Data.Sqlite;
using Shouldly;
using System.Collections.Generic;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Runs the migration's exact SQL against SQLite (no Postgres is available to the tests), over
    // minimal Boards/BoardMembers tables with the columns the statement touches. Unlike the real
    // schema, OwnerUserId is nullable here so the NULL-owner guard can be exercised.
    public class BoardOwnerMembershipBackfillTests
    {
        private static SqliteConnection CreateDatabase()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            Execute(connection, @"
                CREATE TABLE ""Boards"" (""Id"" INTEGER PRIMARY KEY, ""OwnerUserId"" TEXT NULL);
                CREATE TABLE ""BoardMembers"" (
                    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                    ""BoardId"" INTEGER NOT NULL,
                    ""UserId"" TEXT NOT NULL,
                    ""DateCreated"" TEXT NULL);
                CREATE UNIQUE INDEX ""IX_BoardMembers_BoardId_UserId"" ON ""BoardMembers"" (""BoardId"", ""UserId"");

                INSERT INTO ""Boards"" (""Id"", ""OwnerUserId"") VALUES
                    (1, 'owner-1'),   -- no members at all
                    (2, 'owner-2'),   -- members, but not its owner
                    (3, 'owner-3'),   -- owner already a member
                    (4, NULL);        -- no owner

                INSERT INTO ""BoardMembers"" (""BoardId"", ""UserId"") VALUES
                    (2, 'teammate'),
                    (3, 'owner-3'),
                    (4, 'someone');");

            return connection;
        }

        private static void Execute(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static List<(long BoardId, string UserId)> Members(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT ""BoardId"", ""UserId"" FROM ""BoardMembers"" ORDER BY ""BoardId"", ""UserId""";
            using var reader = command.ExecuteReader();

            var rows = new List<(long, string)>();
            while (reader.Read())
                rows.Add((reader.GetInt64(0), reader.GetString(1)));
            return rows;
        }

        [Fact]
        public void Backfill_AddsMissingOwners_SkipsOwnerlessBoards_AndIsIdempotent()
        {
            using var connection = CreateDatabase();

            Execute(connection, BackfillBoardOwnerMemberships.BackfillSql);
            // A second run (e.g. the SQL re-applied by hand) must neither fail on the unique
            // index nor add anything.
            Execute(connection, BackfillBoardOwnerMemberships.BackfillSql);

            Members(connection).ShouldBe(new List<(long, string)>
            {
                (1, "owner-1"),
                (2, "owner-2"),
                (2, "teammate"),
                (3, "owner-3"),
                (4, "someone")
            });
        }
    }
}
