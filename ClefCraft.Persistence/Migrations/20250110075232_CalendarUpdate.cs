using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CalendarUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2025, 1, 10, 10, 52, 32, 242, DateTimeKind.Local).AddTicks(5145), new DateTime(2025, 1, 10, 10, 52, 32, 242, DateTimeKind.Local).AddTicks(5163) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2025, 1, 4, 22, 29, 4, 612, DateTimeKind.Local).AddTicks(8374), new DateTime(2025, 1, 4, 22, 29, 4, 612, DateTimeKind.Local).AddTicks(8389) });
        }
    }
}
