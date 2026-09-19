using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Identity.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmployeeRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "cac43a6e-f7bb-4448-baaf-1add431ccbbf", "9e224968-33e4-4652-b7b7-8574d048cdb9" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "cac43a6e-f7bb-4448-baaf-1add431ccbbf");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "944d0156-cb3d-466f-a1ea-5f53e3a10f8e",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7bba2a3b-cab1-4d44-b7b3-1f97bb3ba528", "AQAAAAIAAYagAAAAEHRuSBZdfNsPmwrB1VK0tG0dLc6RIixkxryXrmCzJzVgZOplhKEwZQKKuyynvnkfyQ==", "25b80fc1-b896-4a2c-8795-e040512e2f03" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "41ee4152-4ba0-44b0-b3ff-2c2ce1461156", "AQAAAAIAAYagAAAAECL6eBCqhCYDzEs50PdobOeVh8gtZ9aS7Md3XwyfuEyY/7tcoIiIHUCQxWtfK+y7UA==", "7c967d80-7918-4468-b319-97c5dab9f8ce" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "cac43a6e-f7bb-4448-baaf-1add431ccbbf", null, "Employee", "EMPLOYEE" });

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

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "cac43a6e-f7bb-4448-baaf-1add431ccbbf", "9e224968-33e4-4652-b7b7-8574d048cdb9" });
        }
    }
}
