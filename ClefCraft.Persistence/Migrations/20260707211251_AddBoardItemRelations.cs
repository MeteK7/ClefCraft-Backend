using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardItemRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardItemRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SourceBoardItemId = table.Column<int>(type: "integer", nullable: false),
                    TargetBoardItemId = table.Column<int>(type: "integer", nullable: false),
                    RelationType = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardItemRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardItemRelations_BoardItems_SourceBoardItemId",
                        column: x => x.SourceBoardItemId,
                        principalTable: "BoardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardItemRelations_BoardItems_TargetBoardItemId",
                        column: x => x.TargetBoardItemId,
                        principalTable: "BoardItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemRelations_SourceBoardItemId_TargetBoardItemId_Rela~",
                table: "BoardItemRelations",
                columns: new[] { "SourceBoardItemId", "TargetBoardItemId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardItemRelations_TargetBoardItemId",
                table: "BoardItemRelations",
                column: "TargetBoardItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardItemRelations");
        }
    }
}
