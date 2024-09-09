using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BoardItemUpdated2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 9, 10, 1, 45, 6, 477, DateTimeKind.Local).AddTicks(2233), new DateTime(2024, 9, 10, 1, 45, 6, 477, DateTimeKind.Local).AddTicks(2247) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 9, 10, 1, 40, 13, 278, DateTimeKind.Local).AddTicks(5083), new DateTime(2024, 9, 10, 1, 40, 13, 278, DateTimeKind.Local).AddTicks(5098) });
        }
    }
}
