using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Identity.Migrations
{
    /// <inheritdoc />
    public partial class Updated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "006cdb64-2484-4306-b56e-5ce0f6de9d6c", "AQAAAAIAAYagAAAAEAY/mGxlGZRABjHGv+IFCG68y5P2kXnrukHOs+xNIof/HSVq/SJuD4dJ7A1y3wZ/8g==", "9b3c9c07-cb9d-4168-ab23-783368f5bd12" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0ad775bc-b861-40ce-a9ab-cca53a298592", "AQAAAAIAAYagAAAAEP1Kz+eaYa9xEe2+Pu7dC2BJ7paXH0sgpqFj+oAQdn+C/liCEJiWOfATdzltkzrcmg==", "95b4a956-c158-47a1-810e-036c589ab7c5" });
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
