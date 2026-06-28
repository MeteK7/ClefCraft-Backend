using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Identity.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9cc1229c-652b-4272-b769-c01f6bffce03", "AQAAAAIAAYagAAAAEICs9L7UoMpDY5DzP8y5I545e/fX5lFrqUcbfiElEVGk2M1LEn0aQWRX67EhxE5Zuw==", "bac4045e-6a4a-4feb-9958-2f00b8d94076" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "90a9a0d5-1727-4b37-bf8d-847e4a262132", "AQAAAAIAAYagAAAAEB+ZfZecGmRHmpwFbJ2e9x3owf9Ypy72GL/AfktYXIpUkN8wN87fgumK1SvAI6MWoQ==", "ceeeb30a-fe7f-45c2-989c-88d531bb2a21" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3eb70d9b-7513-4fa5-a016-0940c140b865", "AQAAAAIAAYagAAAAEM78mVlOBOgtKdFsoxE1Fb9zY0zdvkQ2KELZshmONBRZ39r0VRspLuE1ep7qc7q2Ig==", "78852ca5-fac8-4e63-8d61-e301cde209c6" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5e51ff84-841b-4e4c-bf7a-90b44efc3fca", "AQAAAAIAAYagAAAAEEXGm6/YORCY/HG2GISz1xlmF5JbdO+tIRnvoX60fx1N7gsx38Usd6fXfZ9/KuHjWQ==", "e215cd77-fe8d-4384-b0d1-520fa5d45c3d" });
        }
    }
}
