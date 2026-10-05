using ClefCraft.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClefCraft.Identity.Seeding
{
    /// <summary>
    /// Development-only: creates the two local accounts the demo seed SQL refers to, with fixed ids
    /// and passwords taken from user-secrets. Only ever invoked from Program.cs under IsDevelopment(),
    /// after the migrations have run.
    ///
    ///   dotnet user-secrets set "DevSeed:AdminPassword" "..." --project ClefCraft.Api
    ///   dotnet user-secrets set "DevSeed:UserPassword" "..." --project ClefCraft.Api
    ///
    /// Create-only: an account that already exists is never modified, so changing a password secret
    /// later needs a database reset. A missing secret skips that account. Identity failures (e.g. a
    /// password that fails the password policy) are logged and don't stop startup.
    /// </summary>
    public class DevelopmentUserSeeder
    {
        public const string AdminUserId = "944d0156-cb3d-466f-a1ea-5f53e3a10f8e";
        public const string StandardUserId = "9e224968-33e4-4652-b7b7-8574d048cdb9";
        public const string AdminEmail = "admin@localhost.com";
        public const string StandardUserEmail = "user@localhost.com";
        public const string AdministratorRole = "Administrator";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DevelopmentUserSeeder> _logger;

        public DevelopmentUserSeeder(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger<DevelopmentUserSeeder> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            await EnsureUserAsync(AdminUserId, AdminEmail, "Admin", "DevSeed:AdminPassword", AdministratorRole);
            await EnsureUserAsync(StandardUserId, StandardUserEmail, "User", "DevSeed:UserPassword", role: null);
        }

        private async Task EnsureUserAsync(string userId, string email, string lastName, string passwordKey, string? role)
        {
            var password = _configuration[passwordKey];
            if (string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Development seed user {Email} skipped: {PasswordKey} is not set.", email, passwordKey);
                return;
            }

            if (await _userManager.FindByIdAsync(userId) != null)
            {
                _logger.LogInformation("Development seed user {Email} already exists; left unchanged.", email);
                return;
            }

            var byEmail = await _userManager.FindByEmailAsync(email);
            if (byEmail != null)
            {
                _logger.LogWarning(
                    "Development seed user {Email} skipped: that email belongs to user {ExistingUserId}, not {UserId}.",
                    email, byEmail.Id, userId);
                return;
            }

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = lastName
            };

            var created = await _userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                LogFailure("create", email, created);
                return;
            }

            if (role != null && !await TryAddToRoleAsync(user, role))
            {
                // Don't leave a half-seeded account behind: the next run would skip it as existing.
                await _userManager.DeleteAsync(user);
                return;
            }

            _logger.LogInformation("Development seed user {Email} created.", email);
        }

        private async Task<bool> TryAddToRoleAsync(ApplicationUser user, string role)
        {
            try
            {
                var added = await _userManager.AddToRoleAsync(user, role);
                if (added.Succeeded)
                    return true;

                LogFailure($"add to role {role}", user.Email!, added);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                // The store throws (rather than failing the result) when the role doesn't exist.
                _logger.LogError(ex, "Could not add development seed user {Email} to role {Role}.", user.Email, role);
                return false;
            }
        }

        private void LogFailure(string action, string email, IdentityResult result)
        {
            _logger.LogError(
                "Could not {Action} development seed user {Email}: {Errors}",
                action,
                email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
