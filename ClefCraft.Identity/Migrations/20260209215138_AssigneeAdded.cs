using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AssigneeAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "057587ad-010f-40f2-8ba2-c150db4ca12e", "AQAAAAIAAYagAAAAEBLJDlOEP3dQmTW+JGmUmu/285ZtOpUaedoVczYlFtujXZbYi+NXQc6JZ0d56s2z0A==", "fb467629-ece9-4b3c-af33-4e61e6715a5f" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3ccbcf38-39ab-4cb5-abaa-0a9035f198b7", "AQAAAAIAAYagAAAAEOdX8lCLk3ONg0ohidu9r7quFyEuxov00kvD4g+56lHP0fRnn036KMOgfiSe6Diqag==", "c2b4c731-3fa6-4950-887a-409d6c823aa5" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "cc8e8886-8d8b-48a9-81d7-c2ccf57fbc43", "AQAAAAIAAYagAAAAEPSn1Cy0cxtHkLV7YE4AdAWaNeliVk8yyue+TpOJrTG3RsPTuBOneG/rgC/4/RPD1w==", "c1dccb56-a8c4-4521-8278-b0fa3e85539c" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "24998a2a-fee4-4d36-aee8-32571f7f6de0", "AQAAAAIAAYagAAAAEHtEVxLqGw4Du4ZxlOQTYRiQ9yOzQFQEQ3NFaCd/MafXQqBNC81e+7sduhngHNFCbw==", "c1bace1e-b863-48e9-bed3-3879110135f2" });
        }
    }
}
