using ClefCraft.Persistence.Migrations;
using ClefCraft.Persistence.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Runs the reference-data SQL of the Initial migration against SQLite (no Postgres is available to
    // the tests), over minimal tables with the columns the statements touch, and checks that Initial
    // really applies exactly that SQL.
    public class StandardReferenceDataTests
    {
        private const string TaxonomySchema = @"
            CREATE TABLE ""Statuses"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""Name"" TEXT NOT NULL, ""DateCreated"" TEXT NULL);
            CREATE TABLE ""Priorities"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""Name"" TEXT NOT NULL, ""DateCreated"" TEXT NULL);
            CREATE TABLE ""BoardStatuses"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""BoardId"" INTEGER NULL, ""StatusId"" INTEGER NOT NULL, ""DateCreated"" TEXT NULL);
            CREATE TABLE ""BoardPriorities"" (""Id"" INTEGER PRIMARY KEY AUTOINCREMENT, ""BoardId"" INTEGER NULL, ""PriorityId"" INTEGER NOT NULL, ""DateCreated"" TEXT NULL);";

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

        private static List<string> GlobalNames(SqliteConnection connection, string availability, string values, string idColumn) =>
            Rows(connection,
                $"SELECT v.\"Name\" FROM \"{availability}\" a JOIN \"{values}\" v ON v.\"Id\" = a.\"{idColumn}\" " +
                "WHERE a.\"BoardId\" IS NULL ORDER BY v.\"Id\"");

        [Fact]
        public void InsertStatements_OnAnEmptyDatabase_CreateEveryStandardValueWithExactlyOneGlobalRow()
        {
            using var db = new SqliteConnection("Data Source=:memory:");
            db.Open();
            Execute(db, TaxonomySchema);

            foreach (var statement in StandardReferenceData.InsertStatements)
                Execute(db, statement);

            GlobalNames(db, "BoardStatuses", "Statuses", "StatusId")
                .ShouldBe(new List<string> { "Backlog", "To Do", "In Progress", "In Review", "Done" });
            GlobalNames(db, "BoardPriorities", "Priorities", "PriorityId")
                .ShouldBe(new List<string> { "Critical", "High", "Medium", "Low" });
            Rows(db, "SELECT COUNT(*) FROM \"Statuses\"").Single().ShouldBe("5");
            Rows(db, "SELECT COUNT(*) FROM \"Priorities\"").Single().ShouldBe("4");
            Rows(db, "SELECT COUNT(*) FROM \"BoardStatuses\"").Single().ShouldBe("5");
            Rows(db, "SELECT COUNT(*) FROM \"BoardPriorities\"").Single().ShouldBe("4");
        }

        [Fact]
        public void InitialMigration_EndsWithExactlyTheReferenceDataSql()
        {
            var sqlOperations = new Initial().UpOperations.OfType<SqlOperation>().Select(o => o.Sql).ToList();
            var expected = StandardReferenceData.InsertStatements.ToList();

            sqlOperations.ShouldBe(expected);
            new Initial().UpOperations.TakeLast(expected.Count).ShouldAllBe(o => o is SqlOperation);
        }
    }
}
