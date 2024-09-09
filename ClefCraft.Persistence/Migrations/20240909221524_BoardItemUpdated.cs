using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BoardItemUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 9, 10, 1, 15, 24, 502, DateTimeKind.Local).AddTicks(5148), new DateTime(2024, 9, 10, 1, 15, 24, 502, DateTimeKind.Local).AddTicks(5167) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 8, 25, 22, 43, 18, 247, DateTimeKind.Local).AddTicks(2021), new DateTime(2024, 8, 25, 22, 43, 18, 247, DateTimeKind.Local).AddTicks(2037) });
        }
    }
}
