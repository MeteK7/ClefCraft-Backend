using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClefCraft.Identity.UnitTests.Services
{
    // AuthService.Login counts failures (lockoutOnFailure: true); this pins the policy those
    // failures are counted against, as configured by the real AddIdentityServices.
    public class LockoutPolicyTests
    {
        private static LockoutOptions ConfiguredLockout()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Key"] = "unit-test-signing-key-needs-at-least-32-bytes"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIdentityServices(configuration);

            using var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout;
        }

        [Fact]
        public void Lockout_AppliesToNewUsers()
        {
            ConfiguredLockout().AllowedForNewUsers.ShouldBeTrue();
        }

        [Fact]
        public void Lockout_TriggersAfterFiveFailedAttempts()
        {
            ConfiguredLockout().MaxFailedAccessAttempts.ShouldBe(5);
        }

        [Fact]
        public void Lockout_LastsFifteenMinutes()
        {
            ConfiguredLockout().DefaultLockoutTimeSpan.ShouldBe(TimeSpan.FromMinutes(15));
        }
    }
}
