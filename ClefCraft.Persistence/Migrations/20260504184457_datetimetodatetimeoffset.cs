using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class datetimetodatetimeoffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserInteractionSignals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SignalType",
                table: "UserInteractionSignals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "UserInteractionSignals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "StatusChangeCount",
                table: "TaskLifecycles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "ReopenCount",
                table: "TaskLifecycles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AssigneeChangeCount",
                table: "TaskLifecycles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "EntitySnapshots",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "OccurrenceDate",
                table: "CalendarEventExceptions",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ActivityLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "ActivityLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "ActivityLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 5, 4, 21, 44, 57, 182, DateTimeKind.Local).AddTicks(103), new DateTime(2026, 5, 4, 21, 44, 57, 182, DateTimeKind.Local).AddTicks(120) });

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractionSignals_EntityType_EntityId",
                table: "UserInteractionSignals",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractionSignals_UserId",
                table: "UserInteractionSignals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskLifecycles_BoardItemId",
                table: "TaskLifecycles",
                column: "BoardItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntitySnapshots_EntityType_EntityId",
                table: "EntitySnapshots",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_EntityType_EntityId",
                table: "ActivityLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Timestamp",
                table: "ActivityLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserInteractionSignals_EntityType_EntityId",
                table: "UserInteractionSignals");

            migrationBuilder.DropIndex(
                name: "IX_UserInteractionSignals_UserId",
                table: "UserInteractionSignals");

            migrationBuilder.DropIndex(
                name: "IX_TaskLifecycles_BoardItemId",
                table: "TaskLifecycles");

            migrationBuilder.DropIndex(
                name: "IX_EntitySnapshots_EntityType_EntityId",
                table: "EntitySnapshots");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_EntityType_EntityId",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_Timestamp",
                table: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserInteractionSignals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SignalType",
                table: "UserInteractionSignals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "UserInteractionSignals",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "StatusChangeCount",
                table: "TaskLifecycles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ReopenCount",
                table: "TaskLifecycles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "AssigneeChangeCount",
                table: "TaskLifecycles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "EntitySnapshots",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "OccurrenceDate",
                table: "CalendarEventExceptions",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "ActivityLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 4, 6, 23, 27, 21, 396, DateTimeKind.Local).AddTicks(7661), new DateTime(2026, 4, 6, 23, 27, 21, 396, DateTimeKind.Local).AddTicks(7674) });
        }
    }
}
