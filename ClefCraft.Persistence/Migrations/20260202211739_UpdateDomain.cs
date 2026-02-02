using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 2, 3, 0, 17, 38, 714, DateTimeKind.Local).AddTicks(3109), new DateTime(2026, 2, 3, 0, 17, 38, 714, DateTimeKind.Local).AddTicks(3123) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 2, 2, 0, 48, 51, 36, DateTimeKind.Local).AddTicks(5777), new DateTime(2026, 2, 2, 0, 48, 51, 36, DateTimeKind.Local).AddTicks(5792) });
        }
    }
}
