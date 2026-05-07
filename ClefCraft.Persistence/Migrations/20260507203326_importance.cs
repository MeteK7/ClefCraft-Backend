using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class importance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Importance",
                table: "CalendarEvents",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "EventTypeId",
                table: "CalendarEventExceptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Importance",
                table: "CalendarEventExceptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CalendarEventExceptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 5, 7, 23, 33, 25, 267, DateTimeKind.Local).AddTicks(6679), new DateTime(2026, 5, 7, 23, 33, 25, 267, DateTimeKind.Local).AddTicks(6695) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventTypeId",
                table: "CalendarEventExceptions");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "CalendarEventExceptions");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "CalendarEventExceptions");

            migrationBuilder.AlterColumn<string>(
                name: "Importance",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 5, 4, 21, 44, 57, 182, DateTimeKind.Local).AddTicks(103), new DateTime(2026, 5, 4, 21, 44, 57, 182, DateTimeKind.Local).AddTicks(120) });
        }
    }
}
