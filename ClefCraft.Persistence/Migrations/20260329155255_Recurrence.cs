using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Recurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "CalendarEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceRuleJson",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 3, 29, 18, 52, 54, 822, DateTimeKind.Local).AddTicks(2759), new DateTime(2026, 3, 29, 18, 52, 54, 822, DateTimeKind.Local).AddTicks(2776) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "RecurrenceRuleJson",
                table: "CalendarEvents");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 3, 10, 0, 38, 40, 171, DateTimeKind.Local).AddTicks(795), new DateTime(2026, 3, 10, 0, 38, 40, 171, DateTimeKind.Local).AddTicks(848) });
        }
    }
}
