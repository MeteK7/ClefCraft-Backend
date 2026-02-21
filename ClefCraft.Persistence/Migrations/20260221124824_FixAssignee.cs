using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixAssignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Assignee",
                table: "BoardItems",
                newName: "AssigneeId");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 2, 21, 15, 48, 24, 255, DateTimeKind.Local).AddTicks(3448), new DateTime(2026, 2, 21, 15, 48, 24, 255, DateTimeKind.Local).AddTicks(3464) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AssigneeId",
                table: "BoardItems",
                newName: "Assignee");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 2, 3, 0, 17, 38, 714, DateTimeKind.Local).AddTicks(3109), new DateTime(2026, 2, 3, 0, 17, 38, 714, DateTimeKind.Local).AddTicks(3123) });
        }
    }
}
