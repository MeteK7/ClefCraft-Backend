using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Identity.Migrations
{
    /// <inheritdoc />
    public partial class newmig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5916f802-9dce-46e0-a311-87ab3ac7f71c", "AQAAAAIAAYagAAAAENbiQ2nirHANfomhC5R8CNtK+rT/LzMBbVXVkKWg6DxIWwWboErhkPmrLEjARZcMBg==", "09f0bc4b-b823-40fb-8450-769888914f18" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ec7fcafb-1a7c-478c-90d3-f837b0dbe0f3", "AQAAAAIAAYagAAAAEH3HYCZshCLeIrhxMG7i50ruEhTkPdVXpJOQCFj9sFiPXs4Inh2b5mWLhzTSaSiN3Q==", "209a1ebd-a74c-4258-a00d-b99452bb09ea" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "bc39d969-dddd-49d0-bb4d-6c19085762c5", "AQAAAAIAAYagAAAAELC24AKry06ZpxyyVgxRvZnTU+Aw0Un91u+jODJLqhCwgwN3U9mfXvMJV238EIACmw==", "7897e0ea-d77b-4990-b392-0b49c7a94798" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "edd0332c-13f1-483f-b949-84b36545939e", "AQAAAAIAAYagAAAAEKoWgM2IbOf66+YtIUA2VWNqUD/1hIPdo4huEj3xE7McaWRg0MyURjaK7tynEwhvvg==", "e972dd0a-7cd0-4848-afca-6be1db8899d3" });
        }
    }
}
