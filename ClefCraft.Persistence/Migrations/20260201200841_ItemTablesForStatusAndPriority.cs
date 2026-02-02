using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ItemTablesForStatusAndPriority : Migration
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

            migrationBuilder.CreateTable(
                name: "BoardItemPriorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardItemId = table.Column<int>(type: "int", nullable: false),
                    PriorityId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardItemPriorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardItemPriorities_BoardItems_BoardItemId",
                        column: x => x.BoardItemId,
                        principalTable: "BoardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardItemPriorities_Priorities_PriorityId",
                        column: x => x.PriorityId,
                        principalTable: "Priorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BoardItemStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardItemId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardItemStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardItemStatuses_BoardItems_BoardItemId",
                        column: x => x.BoardItemId,
                        principalTable: "BoardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardItemStatuses_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "LeaveTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateCreated", "DateModified" },
                values: new object[] { new DateTime(2026, 2, 1, 23, 8, 40, 171, DateTimeKind.Local).AddTicks(984), new DateTime(2026, 2, 1, 23, 8, 40, 171, DateTimeKind.Local).AddTicks(1001) });

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemPriorities_BoardItemId",
                table: "BoardItemPriorities",
                column: "BoardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemPriorities_PriorityId",
                table: "BoardItemPriorities",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemStatuses_BoardItemId",
                table: "BoardItemStatuses",
                column: "BoardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemStatuses_StatusId",
                table: "BoardItemStatuses",
                column: "StatusId");

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

            migrationBuilder.DropTable(
                name: "BoardItemPriorities");

            migrationBuilder.DropTable(
                name: "BoardItemStatuses");

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
                values: new object[] { new DateTime(2026, 2, 1, 0, 27, 1, 852, DateTimeKind.Local).AddTicks(2617), new DateTime(2026, 2, 1, 0, 27, 1, 852, DateTimeKind.Local).AddTicks(2630) });

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
