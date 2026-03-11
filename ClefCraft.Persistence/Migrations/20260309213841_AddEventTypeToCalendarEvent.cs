using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTypeToCalendarEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventTypeId",
                table: "CalendarEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 3, 10, 0, 38, 40, 171, DateTimeKind.Local).AddTicks(795), new DateTime(2026, 3, 10, 0, 38, 40, 171, DateTimeKind.Local).AddTicks(848) });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_EventTypeId",
                table: "CalendarEvents",
                column: "EventTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_EventTypes_EventTypeId",
                table: "CalendarEvents",
                column: "EventTypeId",
                principalTable: "EventTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_EventTypes_EventTypeId",
                table: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "EventTypes");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_EventTypeId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "EventTypeId",
                table: "CalendarEvents");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 3, 6, 22, 28, 54, 219, DateTimeKind.Local).AddTicks(536), new DateTime(2026, 3, 6, 22, 28, 54, 219, DateTimeKind.Local).AddTicks(551) });
        }
    }
}
