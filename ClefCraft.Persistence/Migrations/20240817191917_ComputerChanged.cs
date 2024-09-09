using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ComputerChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 8, 17, 22, 19, 17, 6, DateTimeKind.Local).AddTicks(8454), new DateTime(2024, 8, 17, 22, 19, 17, 6, DateTimeKind.Local).AddTicks(8468) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 6, 27, 15, 57, 30, 676, DateTimeKind.Local).AddTicks(3208), new DateTime(2024, 6, 27, 15, 57, 30, 676, DateTimeKind.Local).AddTicks(3221) });
        }
    }
}
