using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UtcNow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2025, 1, 22, 16, 14, 26, 773, DateTimeKind.Local).AddTicks(5711), new DateTime(2025, 1, 22, 16, 14, 26, 773, DateTimeKind.Local).AddTicks(5725) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2025, 1, 22, 14, 34, 43, 969, DateTimeKind.Local).AddTicks(3880), new DateTime(2025, 1, 22, 14, 34, 43, 969, DateTimeKind.Local).AddTicks(3903) });
        }
    }
}
