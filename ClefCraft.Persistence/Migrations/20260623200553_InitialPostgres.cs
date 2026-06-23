using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 6, 23, 23, 5, 52, 596, DateTimeKind.Local).AddTicks(7359), new DateTime(2026, 6, 23, 23, 5, 52, 596, DateTimeKind.Local).AddTicks(7375) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 6, 4, 21, 53, 58, 817, DateTimeKind.Local).AddTicks(2705), new DateTime(2026, 6, 4, 21, 53, 58, 817, DateTimeKind.Local).AddTicks(2794) });
        }
    }
}
