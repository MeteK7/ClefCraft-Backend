using ClefCraft.Identity.DbContext;
using ClefCraft.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace ClefCraft.Identity.UnitTests.Services
{
    // IdentityServicesRegistration.AddIdentityServices calls services.AddIdentity<ApplicationUser, IdentityRole>()
    // without overriding IdentityOptions.Password, so ASP.NET Identity's *default* password
    // policy applies (digit + upper + lower + non-alphanumeric + length >= 6). That's stricter
    // than RegistrationRequest's [MinLength(6)] DTO validation, so a 6-character all-lowercase
    // password passes model validation but is rejected by UserManager.CreateAsync. These tests
    // exercise the real Identity stack (EF InMemory store, no mocks) to document that gap.
    public class PasswordPolicyTests
    {
        private static UserManager<ApplicationUser> BuildRealUserManager()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<ClefCraftIdentityDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ClefCraftIdentityDbContext>()
                .AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            return provider.CreateScope().ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        [Fact]
        public async Task CreateAsync_SixCharAllLowercasePassword_PassesDtoValidationButFailsIdentityPolicy()
        {
            var userManager = BuildRealUserManager();
            var user = new ApplicationUser { UserName = "weakuser", Email = "weak@test.com", FirstName = "A", LastName = "B" };

            // "abcdef" satisfies RegistrationRequest's [MinLength(6)], but not Identity's default policy.
            var result = await userManager.CreateAsync(user, "abcdef");

            result.Succeeded.ShouldBeFalse();
        }

        [Fact]
        public async Task CreateAsync_PasswordMeetingIdentityDefaultPolicy_Succeeds()
        {
            var userManager = BuildRealUserManager();
            var user = new ApplicationUser { UserName = "stronguser", Email = "strong@test.com", FirstName = "A", LastName = "B" };

            var result = await userManager.CreateAsync(user, "Str0ng!Pass");

            result.Succeeded.ShouldBeTrue();
        }
    }
}
