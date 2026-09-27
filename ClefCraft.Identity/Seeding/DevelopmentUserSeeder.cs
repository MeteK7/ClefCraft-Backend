using ClefCraft.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClefCraft.Identity.Seeding
{
    /// <summary>
    /// Development-only: gives the two formerly seeded accounts a password taken from
    /// user-secrets, since the DisableSeededAccounts migration clears it on every database.
    /// Only ever invoked from Program.cs under IsDevelopment(); an unset secret is a no-op.
    ///
    ///   dotnet user-secrets set "DevSeed:AdminPassword" "..." --project ClefCraft.Api
    ///   dotnet user-secrets set "DevSeed:UserPassword" "..." --project ClefCraft.Api
    /// </summary>
    public class DevelopmentUserSeeder
    {
        public const string AdminUserId = "944d0156-cb3d-466f-a1ea-5f53e3a10f8e";
        public const string StandardUserId = "9e224968-33e4-4652-b7b7-8574d048cdb9";

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
            await SetPasswordAsync(AdminUserId, _configuration["DevSeed:AdminPassword"]);
            await SetPasswordAsync(StandardUserId, _configuration["DevSeed:UserPassword"]);
        }

        private async Task SetPasswordAsync(string userId, string? password)
        {
            if (string.IsNullOrEmpty(password))
                return;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Development seed user {UserId} does not exist; skipping.", userId);
                return;
            }

            if (await _userManager.CheckPasswordAsync(user, password))
                return;

            if (await _userManager.HasPasswordAsync(user))
            {
                var removed = await _userManager.RemovePasswordAsync(user);
                if (!removed.Succeeded)
                {
                    LogFailure(userId, removed);
                    return;
                }
            }

            var added = await _userManager.AddPasswordAsync(user, password);
            if (!added.Succeeded)
            {
                LogFailure(userId, added);
                return;
            }

            _logger.LogInformation("Development password set for seed user {UserId}.", userId);
        }

        private void LogFailure(string userId, IdentityResult result)
        {
            _logger.LogWarning(
                "Could not set development password for seed user {UserId}: {Errors}",
                userId,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
