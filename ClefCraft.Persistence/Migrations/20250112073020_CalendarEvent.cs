using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CalendarEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2025, 1, 12, 10, 30, 19, 541, DateTimeKind.Local).AddTicks(9484), new DateTime(2025, 1, 12, 10, 30, 19, 541, DateTimeKind.Local).AddTicks(9499) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2025, 1, 10, 15, 45, 21, 248, DateTimeKind.Local).AddTicks(8153), new DateTime(2025, 1, 10, 15, 45, 21, 248, DateTimeKind.Local).AddTicks(8165) });
        }
    }
}
