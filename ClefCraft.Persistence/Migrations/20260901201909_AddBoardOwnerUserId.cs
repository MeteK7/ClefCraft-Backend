using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added nullable first: no ownership data existed before this migration,
            // so existing rows are backfilled from the audit CreatedBy column (populated
            // automatically for every board by ClefCraftDatabaseContext.SaveChangesAsync)
            // before the NOT NULL constraint is applied. If any row still has a null
            // CreatedBy, the AlterColumn below fails the migration outright rather than
            // silently assigning a bogus empty-string owner.
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Boards",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Boards\" SET \"OwnerUserId\" = \"CreatedBy\" WHERE \"OwnerUserId\" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerUserId",
                table: "Boards",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Boards");
        }
    }
}
