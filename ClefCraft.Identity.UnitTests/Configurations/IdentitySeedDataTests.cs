using ClefCraft.Identity.DbContext;
using ClefCraft.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace ClefCraft.Identity.UnitTests.Configurations
{
    // HasData is applied to every database by migrations, production included, so the identity
    // model must never seed an account (or anything that grants one a role).
    public class IdentitySeedDataTests
    {
        private static IModel DesignTimeModel()
        {
            var options = new DbContextOptionsBuilder<ClefCraftIdentityDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            using var context = new ClefCraftIdentityDbContext(options);

            // Seed data is only kept on the design-time model, not the runtime context.Model.
            return context.GetService<IDesignTimeModel>().Model;
        }

        [Fact]
        public void Model_SeedsNoUsers()
        {
            var seed = DesignTimeModel().FindEntityType(typeof(ApplicationUser))!.GetSeedData();

            seed.ShouldBeEmpty();
        }

        [Fact]
        public void Model_SeedsNoUserRoleAssignments()
        {
            var seed = DesignTimeModel().FindEntityType(typeof(IdentityUserRole<string>))!.GetSeedData();

            seed.ShouldBeEmpty();
        }

        [Fact]
        public void Model_StillSeedsAdministratorRole()
        {
            var seed = DesignTimeModel().FindEntityType(typeof(IdentityRole))!.GetSeedData();

            seed.ShouldContain(r => (string)r[nameof(IdentityRole.NormalizedName)]! == "ADMINISTRATOR");
        }

        // A seed value that changes between model builds counts as a pending model change, which
        // makes Migrate() throw at startup.
        [Fact]
        public void Model_SeedsTheAdministratorRoleIdenticallyOnEveryBuild()
        {
            static object? Stamp() => DesignTimeModel().FindEntityType(typeof(IdentityRole))!.GetSeedData()
                .Single(r => (string)r[nameof(IdentityRole.NormalizedName)]! == "ADMINISTRATOR")[nameof(IdentityRole.ConcurrencyStamp)];

            var first = Stamp();

            first.ShouldNotBeNull();
            Stamp().ShouldBe(first);
        }
    }
}
