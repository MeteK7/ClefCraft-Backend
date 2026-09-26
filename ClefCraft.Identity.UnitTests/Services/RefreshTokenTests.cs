using ClefCraft.Application.Models.Identity;
using ClefCraft.Identity.DbContext;
using ClefCraft.Identity.Models;
using ClefCraft.Identity.Services;
using ClefCraft.Identity.UnitTests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ClefCraft.Identity.UnitTests.Services
{
    public class RefreshTokenTests
    {
        private const string Password = "correct";

        private readonly ApplicationUser _user = new() { Id = "user-1", Email = "a@test.com", UserName = "auser" };
        private readonly ClefCraftIdentityDbContext _context = new(
            new DbContextOptionsBuilder<ClefCraftIdentityDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        private readonly AuthService _service;

        public RefreshTokenTests()
        {
            _service = MakeServiceFor(_context);
        }

        private Task<AuthResponse> Login() =>
            _service.Login(new AuthRequest { Email = _user.Email!, Password = Password });

        private static string Hash(string raw) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        private RefreshToken Row(string raw) =>
            _context.RefreshTokens.AsNoTracking().Single(t => t.TokenHash == Hash(raw));

        [Fact]
        public async Task Login_IssuesRefreshToken_AndStoresOnlyItsHash()
        {
            var response = await Login();

            response.RefreshToken.ShouldNotBeNullOrEmpty();
            response.RefreshTokenExpiresAt.ShouldBeInRange(DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(15));
            response.SessionExpiresAt.ShouldBeInRange(DateTime.UtcNow.AddHours(8).AddMinutes(-1), DateTime.UtcNow.AddHours(8));

            var row = _context.RefreshTokens.Single();
            row.UserId.ShouldBe(_user.Id);
            row.TokenHash.ShouldBe(Hash(response.RefreshToken));
            row.TokenHash.ShouldNotBe(response.RefreshToken);
            row.RevokedAt.ShouldBeNull();
        }

        [Fact]
        public async Task Refresh_ValidToken_RotatesAndRevokesTheOldOne()
        {
            var login = await Login();

            var refreshed = await _service.Refresh(login.RefreshToken);

            refreshed.ShouldNotBeNull();
            refreshed.Token.ShouldNotBeNullOrEmpty();
            refreshed.RefreshToken.ShouldNotBe(login.RefreshToken);

            var old = Row(login.RefreshToken);
            old.RevokedAt.ShouldNotBeNull();
            old.ReplacedByTokenHash.ShouldBe(Hash(refreshed.RefreshToken));
            Row(refreshed.RefreshToken).RevokedAt.ShouldBeNull();
        }

        [Fact]
        public async Task Refresh_ReusedRevokedToken_RevokesEveryTokenOfThatUser()
        {
            var deviceA = await Login();
            var deviceB = await Login();
            var rotatedA = await _service.Refresh(deviceA.RefreshToken);

            // deviceA's original token was rotated away — presenting it again signals theft.
            var reuse = await _service.Refresh(deviceA.RefreshToken);

            reuse.ShouldBeNull();
            Row(rotatedA!.RefreshToken).RevokedAt.ShouldNotBeNull();
            Row(deviceB.RefreshToken).RevokedAt.ShouldNotBeNull();
            (await _service.Refresh(deviceB.RefreshToken)).ShouldBeNull();
        }

        [Fact]
        public async Task Refresh_IdleExpiredToken_ReturnsNull()
        {
            var login = await Login();
            var row = _context.RefreshTokens.Single();
            row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await _context.SaveChangesAsync();

            (await _service.Refresh(login.RefreshToken)).ShouldBeNull();
        }

        [Fact]
        public async Task Refresh_AfterTheSessionCap_ReturnsNull_EvenIfRecentlyActive()
        {
            var login = await Login();
            var row = _context.RefreshTokens.Single();
            row.ExpiresAt = DateTime.UtcNow.AddMinutes(10);        // still within the idle window
            row.SessionExpiresAt = DateTime.UtcNow.AddSeconds(-1); // but past the 8-hour cap
            await _context.SaveChangesAsync();

            (await _service.Refresh(login.RefreshToken)).ShouldBeNull();
        }

        [Fact]
        public async Task Refresh_KeepsTheSessionCap_AndNeverExtendsTheIdleDeadlinePastIt()
        {
            var login = await Login();
            var capSoon = DateTime.UtcNow.AddMinutes(5); // less than one idle window left
            var row = _context.RefreshTokens.Single();
            row.SessionExpiresAt = capSoon;
            await _context.SaveChangesAsync();

            var refreshed = await _service.Refresh(login.RefreshToken);

            refreshed.ShouldNotBeNull();
            refreshed.SessionExpiresAt.ShouldBe(capSoon);
            refreshed.RefreshTokenExpiresAt.ShouldBe(capSoon);
            Row(refreshed.RefreshToken).SessionExpiresAt.ShouldBe(capSoon);
        }

        [Theory]
        [InlineData("not-a-real-token")]
        [InlineData("")]
        public async Task Refresh_UnknownOrEmptyToken_ReturnsNull(string token)
        {
            await Login();

            (await _service.Refresh(token)).ShouldBeNull();
        }

        [Fact]
        public async Task Logout_RevokesToken_AndIsIdempotent()
        {
            var login = await Login();

            await _service.Logout(login.RefreshToken);
            await _service.Logout(login.RefreshToken);
            await _service.Logout("unknown-token");

            Row(login.RefreshToken).RevokedAt.ShouldNotBeNull();
            (await _service.Refresh(login.RefreshToken)).ShouldBeNull();
        }

        [Fact]
        public async Task Refresh_LosingAConcurrentRotation_ReturnsNull_WithoutRevokingOtherSessions()
        {
            var dbName = Guid.NewGuid().ToString();
            DbContextOptions<ClefCraftIdentityDbContext> OptionsWith(params IInterceptor[] i) =>
                new DbContextOptionsBuilder<ClefCraftIdentityDbContext>().UseInMemoryDatabase(dbName).AddInterceptors(i).Options;

            // Seed two sessions for the user through a plain context.
            var seedService = MakeServiceFor(new ClefCraftIdentityDbContext(OptionsWith()));
            var session = await seedService.Login(new AuthRequest { Email = _user.Email!, Password = Password });
            var otherDevice = await seedService.Login(new AuthRequest { Email = _user.Email!, Password = Password });

            // Just before this refresh commits, another request rotates the same token.
            var racer = new RotateFirstInterceptor(() => MakeServiceFor(new ClefCraftIdentityDbContext(OptionsWith())).Refresh(session.RefreshToken));
            var losingService = MakeServiceFor(new ClefCraftIdentityDbContext(OptionsWith(racer)));

            (await losingService.Refresh(session.RefreshToken)).ShouldBeNull();

            using var verify = new ClefCraftIdentityDbContext(OptionsWith());
            verify.RefreshTokens.Single(t => t.TokenHash == Hash(otherDevice.RefreshToken)).RevokedAt.ShouldBeNull();
        }

        private AuthService MakeServiceFor(ClefCraftIdentityDbContext context)
        {
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByEmailAsync(_user.Email!)).ReturnsAsync(_user);
            userManager.Setup(m => m.FindByIdAsync(_user.Id)).ReturnsAsync(_user);
            userManager.Setup(m => m.GetClaimsAsync(_user)).ReturnsAsync(new List<Claim>());
            userManager.Setup(m => m.GetRolesAsync(_user)).ReturnsAsync(new List<string>());
            var signInManager = IdentityMocks.MockSignInManager(userManager.Object);
            signInManager.Setup(s => s.CheckPasswordSignInAsync(_user, Password, false)).ReturnsAsync(SignInResult.Success);

            return new AuthService(userManager.Object, signInManager.Object, Options.Create(new JwtSettings
            {
                Key = "unit-test-signing-key-needs-at-least-32-bytes",
                Issuer = "ClefCraft.Api.Tests",
                Audience = "ClefCraftUser.Tests",
                DurationInMinutes = 15,
                RefreshTokenIdleMinutes = 15,
                SessionMaxHours = 8
            }), context);
        }

        private sealed class RotateFirstInterceptor : SaveChangesInterceptor
        {
            private readonly Func<Task> _competingRefresh;
            private bool _fired;

            public RotateFirstInterceptor(Func<Task> competingRefresh) => _competingRefresh = competingRefresh;

            public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                if (!_fired)
                {
                    _fired = true;
                    await _competingRefresh();
                }
                return result;
            }
        }

        [Fact]
        public async Task Login_PurgesThatUsersExpiredTokens_Only()
        {
            _context.RefreshTokens.AddRange(
                new RefreshToken { UserId = _user.Id, TokenHash = "expired-mine", ExpiresAt = DateTime.UtcNow.AddDays(-1) },
                new RefreshToken { UserId = "someone-else", TokenHash = "expired-theirs", ExpiresAt = DateTime.UtcNow.AddDays(-1) });
            await _context.SaveChangesAsync();

            await Login();

            var hashes = _context.RefreshTokens.Select(t => t.TokenHash).ToList();
            hashes.ShouldNotContain("expired-mine");
            hashes.ShouldContain("expired-theirs");
            hashes.Count.ShouldBe(2);
        }
    }
}
