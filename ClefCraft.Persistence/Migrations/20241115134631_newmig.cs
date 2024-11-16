using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class newmig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 11, 15, 16, 46, 28, 151, DateTimeKind.Local).AddTicks(317), new DateTime(2024, 11, 15, 16, 46, 28, 151, DateTimeKind.Local).AddTicks(331) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2024, 10, 6, 14, 36, 10, 399, DateTimeKind.Local).AddTicks(4577), new DateTime(2024, 10, 6, 14, 36, 10, 399, DateTimeKind.Local).AddTicks(4592) });
        }
    }
}
