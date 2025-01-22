using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WorkHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2025, 1, 22, 14, 34, 43, 969, DateTimeKind.Local).AddTicks(3880), new DateTime(2025, 1, 22, 14, 34, 43, 969, DateTimeKind.Local).AddTicks(3903) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2025, 1, 22, 14, 20, 4, 450, DateTimeKind.Local).AddTicks(3183), new DateTime(2025, 1, 22, 14, 20, 4, 450, DateTimeKind.Local).AddTicks(3196) });
        }
    }
}
