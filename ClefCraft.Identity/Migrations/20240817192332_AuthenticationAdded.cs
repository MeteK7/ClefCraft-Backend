using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AuthenticationAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ec5e5036-8566-4b7f-b959-d8c8f4dbba42", "AQAAAAIAAYagAAAAEPKBfVJEfvV6wVCC7q7RcCMvzPdKb8qjkXJ/aTg9KFcMVX1h/txPp8Y1Tye2/5AggA==", "0026650b-5a77-4520-bb60-6a3c0b030e4a" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0babed99-c4cb-4afd-9232-ee5d9656c66e", "AQAAAAIAAYagAAAAEPc1cFvFVmaA1jVAtr8tvK/t1KQz0VlYX6cl/EQtHNMIQxligc/V2R4Ml6Ofpaq4fA==", "9384d9e4-db78-4630-aef1-6ec3d7a1d2b5" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7650df90-636f-431d-b71c-5b0c64902340", "AQAAAAIAAYagAAAAECi43/Hm4Yuvfqm/vbcjvCGj46p3rnV2O3tzX0WRwrS7UEStPgxlsgEWti+ttJ9voQ==", "b39b7fc6-82df-4354-bbf5-7b87c32d79a0" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "13743291-af17-4ea9-894d-db1b37e3f71f", "AQAAAAIAAYagAAAAEG6onvgJ5ZHUfTTuxZSNDXYoA1pSN9G66YwzhSC8zT4qOK/Smhu6lLYYWddVGwBkdQ==", "b2bee395-c49d-4cf1-afb6-99cffe861994" });
        }
    }
}
