using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecurrenceImproved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEventExceptions_CalendarEvents_CalendarEventId",
                table: "CalendarEventExceptions");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEventExceptions_CalendarEventId_OccurrenceDate",
                table: "CalendarEventExceptions");

            migrationBuilder.DropColumn(
                name: "CalendarEventId",
                table: "CalendarEventExceptions");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "CalendarEventExceptions");

            migrationBuilder.AddColumn<string>(
                name: "SeriesUid",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeriesUid",
                table: "CalendarEventExceptions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 5, 27, 3, 21, 5, 713, DateTimeKind.Local).AddTicks(15), new DateTime(2026, 5, 27, 3, 21, 5, 713, DateTimeKind.Local).AddTicks(32) });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventExceptions_SeriesUid_OccurrenceDate",
                table: "CalendarEventExceptions",
                columns: new[] { "SeriesUid", "OccurrenceDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CalendarEventExceptions_SeriesUid_OccurrenceDate",
                table: "CalendarEventExceptions");

            migrationBuilder.DropColumn(
                name: "SeriesUid",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "SeriesUid",
                table: "CalendarEventExceptions");

            migrationBuilder.AddColumn<int>(
                name: "CalendarEventId",
                table: "CalendarEventExceptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Importance",
                table: "CalendarEventExceptions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 5, 7, 23, 33, 25, 267, DateTimeKind.Local).AddTicks(6679), new DateTime(2026, 5, 7, 23, 33, 25, 267, DateTimeKind.Local).AddTicks(6695) });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEventExceptions_CalendarEventId_OccurrenceDate",
                table: "CalendarEventExceptions",
                columns: new[] { "CalendarEventId", "OccurrenceDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEventExceptions_CalendarEvents_CalendarEventId",
                table: "CalendarEventExceptions",
                column: "CalendarEventId",
                principalTable: "CalendarEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
