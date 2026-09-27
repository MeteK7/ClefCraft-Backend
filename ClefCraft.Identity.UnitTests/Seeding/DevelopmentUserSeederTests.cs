using ClefCraft.Identity.Models;
using ClefCraft.Identity.Seeding;
using ClefCraft.Identity.UnitTests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ClefCraft.Identity.UnitTests.Seeding
{
    public class DevelopmentUserSeederTests
    {
        private static DevelopmentUserSeeder MakeSeeder(
            Mock<UserManager<ApplicationUser>> userManager,
            Dictionary<string, string?> settings)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            return new DevelopmentUserSeeder(userManager.Object, configuration, NullLogger<DevelopmentUserSeeder>.Instance);
        }

        [Fact]
        public async Task SeedAsync_DoesNothing_WhenNoPasswordsConfigured()
        {
            var userManager = IdentityMocks.MockUserManager();
            var seeder = MakeSeeder(userManager, new Dictionary<string, string?>());

            await seeder.SeedAsync();

            userManager.Verify(m => m.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SeedAsync_SetsPassword_WhenAccountHasNone()
        {
            var admin = new ApplicationUser { Id = DevelopmentUserSeeder.AdminUserId };
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByIdAsync(DevelopmentUserSeeder.AdminUserId)).ReturnsAsync(admin);
            userManager.Setup(m => m.CheckPasswordAsync(admin, "Dev-Pass-1")).ReturnsAsync(false);
            userManager.Setup(m => m.HasPasswordAsync(admin)).ReturnsAsync(false);
            userManager.Setup(m => m.AddPasswordAsync(admin, "Dev-Pass-1")).ReturnsAsync(IdentityResult.Success);
            var seeder = MakeSeeder(userManager, new Dictionary<string, string?> { ["DevSeed:AdminPassword"] = "Dev-Pass-1" });

            await seeder.SeedAsync();

            userManager.Verify(m => m.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
            userManager.Verify(m => m.AddPasswordAsync(admin, "Dev-Pass-1"), Times.Once);
            userManager.Verify(m => m.FindByIdAsync(DevelopmentUserSeeder.StandardUserId), Times.Never);
        }

        [Fact]
        public async Task SeedAsync_ReplacesPassword_WhenConfiguredOneDiffers()
        {
            var user = new ApplicationUser { Id = DevelopmentUserSeeder.StandardUserId };
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByIdAsync(DevelopmentUserSeeder.StandardUserId)).ReturnsAsync(user);
            userManager.Setup(m => m.CheckPasswordAsync(user, "Dev-Pass-2")).ReturnsAsync(false);
            userManager.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
            userManager.Setup(m => m.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(m => m.AddPasswordAsync(user, "Dev-Pass-2")).ReturnsAsync(IdentityResult.Success);
            var seeder = MakeSeeder(userManager, new Dictionary<string, string?> { ["DevSeed:UserPassword"] = "Dev-Pass-2" });

            await seeder.SeedAsync();

            userManager.Verify(m => m.RemovePasswordAsync(user), Times.Once);
            userManager.Verify(m => m.AddPasswordAsync(user, "Dev-Pass-2"), Times.Once);
        }

        [Fact]
        public async Task SeedAsync_LeavesPasswordAlone_WhenItAlreadyMatches()
        {
            var admin = new ApplicationUser { Id = DevelopmentUserSeeder.AdminUserId };
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByIdAsync(DevelopmentUserSeeder.AdminUserId)).ReturnsAsync(admin);
            userManager.Setup(m => m.CheckPasswordAsync(admin, "Dev-Pass-1")).ReturnsAsync(true);
            var seeder = MakeSeeder(userManager, new Dictionary<string, string?> { ["DevSeed:AdminPassword"] = "Dev-Pass-1" });

            await seeder.SeedAsync();

            userManager.Verify(m => m.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
            userManager.Verify(m => m.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SeedAsync_Skips_WhenAccountDoesNotExist()
        {
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByIdAsync(DevelopmentUserSeeder.AdminUserId)).ReturnsAsync((ApplicationUser?)null);
            var seeder = MakeSeeder(userManager, new Dictionary<string, string?> { ["DevSeed:AdminPassword"] = "Dev-Pass-1" });

            await seeder.SeedAsync();

            userManager.Verify(m => m.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }
    }
}
