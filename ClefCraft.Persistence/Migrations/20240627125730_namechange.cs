using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class namechange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 6, 27, 15, 57, 30, 676, DateTimeKind.Local).AddTicks(3208), new DateTime(2024, 6, 27, 15, 57, 30, 676, DateTimeKind.Local).AddTicks(3221) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 6, 25, 17, 37, 0, 631, DateTimeKind.Local).AddTicks(7987), new DateTime(2024, 6, 25, 17, 37, 0, 631, DateTimeKind.Local).AddTicks(8020) });
        }
    }
}
