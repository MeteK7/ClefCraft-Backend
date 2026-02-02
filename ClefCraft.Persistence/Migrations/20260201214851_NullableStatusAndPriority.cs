using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NullableStatusAndPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardItems_Priorities_PriorityId",
                table: "BoardItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardItems_Statuses_StatusId",
                table: "BoardItems");

            migrationBuilder.DropIndex(
                name: "IX_BoardItemStatuses_BoardItemId",
                table: "BoardItemStatuses");

            migrationBuilder.DropIndex(
                name: "IX_BoardItemPriorities_BoardItemId",
                table: "BoardItemPriorities");

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "BoardItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PriorityId",
                table: "BoardItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 2, 2, 0, 48, 51, 36, DateTimeKind.Local).AddTicks(5777), new DateTime(2026, 2, 2, 0, 48, 51, 36, DateTimeKind.Local).AddTicks(5792) });

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemStatuses_BoardItemId",
                table: "BoardItemStatuses",
                column: "BoardItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemPriorities_BoardItemId",
                table: "BoardItemPriorities",
                column: "BoardItemId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardItems_Priorities_PriorityId",
                table: "BoardItems",
                column: "PriorityId",
                principalTable: "Priorities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardItems_Statuses_StatusId",
                table: "BoardItems",
                column: "StatusId",
                principalTable: "Statuses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardItems_Priorities_PriorityId",
                table: "BoardItems");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardItems_Statuses_StatusId",
                table: "BoardItems");

            migrationBuilder.DropIndex(
                name: "IX_BoardItemStatuses_BoardItemId",
                table: "BoardItemStatuses");

            migrationBuilder.DropIndex(
                name: "IX_BoardItemPriorities_BoardItemId",
                table: "BoardItemPriorities");

            migrationBuilder.AlterColumn<int>(
                name: "StatusId",
                table: "BoardItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PriorityId",
                table: "BoardItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 2, 2, 0, 22, 29, 577, DateTimeKind.Local).AddTicks(2078), new DateTime(2026, 2, 2, 0, 22, 29, 577, DateTimeKind.Local).AddTicks(2098) });

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemStatuses_BoardItemId",
                table: "BoardItemStatuses",
                column: "BoardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemPriorities_BoardItemId",
                table: "BoardItemPriorities",
                column: "BoardItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardItems_Priorities_PriorityId",
                table: "BoardItems",
                column: "PriorityId",
                principalTable: "Priorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardItems_Statuses_StatusId",
                table: "BoardItems",
                column: "StatusId",
                principalTable: "Statuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
