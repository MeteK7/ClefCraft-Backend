using ClefCraft.Identity.DbContext;
using ClefCraft.Identity.Models;
using ClefCraft.Identity.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClefCraft.Identity.UnitTests.Seeding
{
    // The seeder runs against the real Identity stack (EF InMemory store, no mocks), so these tests
    // check what ends up in the store rather than which UserManager calls were made.
    public class DevelopmentUserSeederTests
    {
        private const string AdminPassword = "Adm1n!Pass";
        private const string UserPassword = "Us3r!Pass";

        private sealed class Store
        {
            private readonly ServiceProvider _provider;

            public Store(bool seedModelData = true)
            {
                var services = new ServiceCollection();
                services.AddLogging();
                var databaseName = Guid.NewGuid().ToString();
                services.AddDbContext<ClefCraftIdentityDbContext>(o => o.UseInMemoryDatabase(databaseName));
                services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<ClefCraftIdentityDbContext>()
                    .AddDefaultTokenProviders();
                _provider = services.BuildServiceProvider();

                // EnsureCreated applies the model's HasData, i.e. the Administrator role a migrated
                // database gets from the Initial migration.
                if (seedModelData)
                {
                    using var scope = _provider.CreateScope();
                    scope.ServiceProvider.GetRequiredService<ClefCraftIdentityDbContext>().Database.EnsureCreated();
                }
            }

            // A fresh scope per call, as each application startup gets its own.
            public async Task SeedAsync(Dictionary<string, string?> settings)
            {
                using var scope = _provider.CreateScope();
                var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
                var seeder = new DevelopmentUserSeeder(
                    scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                    configuration,
                    NullLogger<DevelopmentUserSeeder>.Instance);
                await seeder.SeedAsync();
            }

            public async Task<T> WithUserManager<T>(Func<UserManager<ApplicationUser>, Task<T>> action)
            {
                using var scope = _provider.CreateScope();
                return await action(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
            }

            public Task<List<ApplicationUser>> UsersAsync() =>
                WithUserManager(m => m.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync());
        }

        private static Dictionary<string, string?> BothPasswords() => new()
        {
            ["DevSeed:AdminPassword"] = AdminPassword,
            ["DevSeed:UserPassword"] = UserPassword
        };

        [Fact]
        public async Task SeedAsync_CreatesBothUsersWithTheirFixedIds_AndMakesTheAdminAnAdministrator()
        {
            var store = new Store();

            await store.SeedAsync(BothPasswords());

            var users = await store.UsersAsync();
            users.Select(u => (u.Id, u.Email, u.UserName, u.EmailConfirmed)).ShouldBe(new (string, string?, string?, bool)[]
            {
                (DevelopmentUserSeeder.AdminUserId, "admin@localhost.com", "admin@localhost.com", true),
                (DevelopmentUserSeeder.StandardUserId, "user@localhost.com", "user@localhost.com", true)
            });

            (await store.WithUserManager(async m => await m.IsInRoleAsync((await m.FindByIdAsync(DevelopmentUserSeeder.AdminUserId))!, "Administrator")))
                .ShouldBeTrue();
            (await store.WithUserManager(async m => await m.GetRolesAsync((await m.FindByIdAsync(DevelopmentUserSeeder.StandardUserId))!)))
                .ShouldBeEmpty();
            (await store.WithUserManager(async m => await m.CheckPasswordAsync((await m.FindByIdAsync(DevelopmentUserSeeder.AdminUserId))!, AdminPassword)))
                .ShouldBeTrue();
            (await store.WithUserManager(async m => await m.CheckPasswordAsync((await m.FindByIdAsync(DevelopmentUserSeeder.StandardUserId))!, UserPassword)))
                .ShouldBeTrue();
        }

        [Fact]
        public async Task SeedAsync_SecondRun_ChangesNothing_EvenWithADifferentPassword()
        {
            var store = new Store();
            await store.SeedAsync(BothPasswords());
            var before = (await store.UsersAsync()).Select(u => (u.Id, u.ConcurrencyStamp, u.SecurityStamp, u.PasswordHash)).ToList();

            await store.SeedAsync(new Dictionary<string, string?>
            {
                ["DevSeed:AdminPassword"] = "Changed!Pass1",
                ["DevSeed:UserPassword"] = "Changed!Pass2"
            });

            (await store.UsersAsync()).Select(u => (u.Id, u.ConcurrencyStamp, u.SecurityStamp, u.PasswordHash)).ToList()
                .ShouldBe(before);
        }

        [Fact]
        public async Task SeedAsync_MissingPassword_SkipsOnlyThatUser()
        {
            var store = new Store();

            await store.SeedAsync(new Dictionary<string, string?> { ["DevSeed:UserPassword"] = UserPassword });

            (await store.UsersAsync()).Select(u => u.Id).ShouldBe(new[] { DevelopmentUserSeeder.StandardUserId });
        }

        [Fact]
        public async Task SeedAsync_EmailAlreadyUsedByAnotherId_SkipsThatUser_AndLeavesTheExistingOneUnchanged()
        {
            var store = new Store();
            var other = new ApplicationUser
            {
                Id = "someone-else",
                UserName = "admin@localhost.com",
                Email = "admin@localhost.com",
                FirstName = "Other",
                LastName = "Person"
            };
            (await store.WithUserManager(m => m.CreateAsync(other, "0ther!Pass"))).Succeeded.ShouldBeTrue();
            var before = (await store.UsersAsync()).Single();

            await store.SeedAsync(BothPasswords());

            var users = await store.UsersAsync();
            users.Select(u => u.Id).ShouldBe(new[] { "someone-else", DevelopmentUserSeeder.StandardUserId });
            var after = users.First();
            (after.FirstName, after.ConcurrencyStamp, after.SecurityStamp, after.PasswordHash)
                .ShouldBe((before.FirstName, before.ConcurrencyStamp, before.SecurityStamp, before.PasswordHash));
            (await store.WithUserManager(m => m.GetRolesAsync(after))).ShouldBeEmpty();
        }

        [Fact]
        public async Task SeedAsync_PasswordFailingThePolicy_CreatesNoUser()
        {
            var store = new Store();

            await store.SeedAsync(new Dictionary<string, string?>
            {
                ["DevSeed:AdminPassword"] = "abcdef", // no digit, upper case or symbol
                ["DevSeed:UserPassword"] = UserPassword
            });

            (await store.UsersAsync()).Select(u => u.Id).ShouldBe(new[] { DevelopmentUserSeeder.StandardUserId });
        }

        [Fact]
        public async Task SeedAsync_RoleAssignmentFails_RemovesTheNewAdmin_SoTheNextRunRetries()
        {
            var store = new Store(seedModelData: false); // no Administrator role in the store

            await store.SeedAsync(BothPasswords());

            (await store.UsersAsync()).Select(u => u.Id).ShouldBe(new[] { DevelopmentUserSeeder.StandardUserId });
        }
    }
}
