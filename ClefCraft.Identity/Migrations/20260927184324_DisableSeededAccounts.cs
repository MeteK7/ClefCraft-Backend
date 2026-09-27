using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClefCraft.Identity.Migrations
{
    /// <summary>
    /// The admin@localhost.com / user@localhost.com accounts used to be seeded with HasData and
    /// passwords written in source, so every database (production included) accepted those
    /// passwords. Removing the seed would normally scaffold a DeleteData, but the rows are kept:
    /// the seed SQL scripts reference both ids as owners of boards, items and events. Instead this
    /// clears the password (password sign-in fails for a null hash), rotates the security stamp,
    /// and revokes every refresh token the accounts hold. Access tokens already issued expire on
    /// their own (JwtSettings:DurationInMinutes). The admin's Administrator role row is untouched.
    /// </summary>
    public partial class DisableSeededAccounts : Migration
    {
        private const string SeededUserIds =
            "'944d0156-cb3d-466f-a1ea-5f53e3a10f8e', '9e224968-33e4-4652-b7b7-8574d048cdb9'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                UPDATE ""AspNetUsers""
                SET ""PasswordHash"" = NULL,
                    ""SecurityStamp"" = md5(random()::text || clock_timestamp()::text),
                    ""ConcurrencyStamp"" = md5(random()::text || clock_timestamp()::text)
                WHERE ""Id"" IN ({SeededUserIds});");

            migrationBuilder.Sql($@"
                DELETE FROM ""RefreshTokens""
                WHERE ""UserId"" IN ({SeededUserIds});");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: restoring the old password hashes would re-open the accounts.
        }
    }
}
