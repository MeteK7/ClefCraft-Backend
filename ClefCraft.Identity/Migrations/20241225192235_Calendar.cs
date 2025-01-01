using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Identity.Migrations
{
    /// <inheritdoc />
    public partial class Calendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0e2066a8-f59f-4ec6-974c-5ef55517bd27", "AQAAAAIAAYagAAAAECy2xDyG7Egprh+0WLWYUbqRa3Xxli4FzeQP5IQ0Z3bIlOdbiBzLTBR8mJWTA5eG5A==", "213349b6-3ebf-48f2-bbf8-daebdd5f9249" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f2916873-ccdb-4ea3-a054-40f46b23e4a7", "AQAAAAIAAYagAAAAEGzHAaKq+GNWSBdXqW0GmN2NsnZxSITBaKd3HVrqvQ6+I2Nqn+SRRMMAkwePI+34JQ==", "84e5e51e-8e0f-410d-822c-eaa280fc6937" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
