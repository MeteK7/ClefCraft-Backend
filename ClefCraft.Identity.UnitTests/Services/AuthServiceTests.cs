using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Identity.Models;
using ClefCraft.Identity.Services;
using ClefCraft.Identity.UnitTests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClefCraft.Identity.UnitTests.Services
{
    public class AuthServiceTests
    {
        private static JwtSettings MakeJwtSettings() => new JwtSettings
        {
            Key = "unit-test-signing-key-needs-at-least-32-bytes",
            Issuer = "ClefCraft.Api.Tests",
            Audience = "ClefCraftUser.Tests",
            DurationInMinutes = 15
        };

        private static AuthService MakeService(
            Mock<UserManager<ApplicationUser>> userManager,
            Mock<SignInManager<ApplicationUser>> signInManager) =>
            new AuthService(userManager.Object, signInManager.Object, Options.Create(MakeJwtSettings()));

        [Fact]
        public async Task Login_UserNotFound_ThrowsNotFoundException()
        {
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByEmailAsync("missing@test.com")).ReturnsAsync((ApplicationUser)null!);
            var signInManager = IdentityMocks.MockSignInManager(userManager.Object);

            var service = MakeService(userManager, signInManager);

            await Should.ThrowAsync<NotFoundException>(() =>
                service.Login(new AuthRequest { Email = "missing@test.com", Password = "whatever" }));
        }

        [Fact]
        public async Task Login_WrongPassword_ThrowsBadRequestException()
        {
            var user = new ApplicationUser { Id = "user-1", Email = "a@test.com", UserName = "auser" };
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            var signInManager = IdentityMocks.MockSignInManager(userManager.Object);
            signInManager.Setup(s => s.CheckPasswordSignInAsync(user, "wrong", false)).ReturnsAsync(SignInResult.Failed);

            var service = MakeService(userManager, signInManager);

            await Should.ThrowAsync<BadRequestException>(() =>
                service.Login(new AuthRequest { Email = user.Email!, Password = "wrong" }));
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsTokenCarryingUidAndRoleClaims()
        {
            var user = new ApplicationUser { Id = "user-42", Email = "a@test.com", UserName = "auser" };
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
            userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Employee" });
            var signInManager = IdentityMocks.MockSignInManager(userManager.Object);
            signInManager.Setup(s => s.CheckPasswordSignInAsync(user, "correct", false)).ReturnsAsync(SignInResult.Success);

            var service = MakeService(userManager, signInManager);

            var response = await service.Login(new AuthRequest { Email = user.Email!, Password = "correct" });

            response.Id.ShouldBe(user.Id);
            response.Token.ShouldNotBeNullOrEmpty();

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
            jwt.Claims.ShouldContain(c => c.Type == "uid" && c.Value == user.Id);
            jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == "Employee");
            jwt.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        }

        [Fact]
        public async Task Register_Success_AssignsEmployeeRoleAndReturnsUserId()
        {
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Str0ng!Pass")).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Employee")).ReturnsAsync(IdentityResult.Success);
            var signInManager = IdentityMocks.MockSignInManager(userManager.Object);

            var service = MakeService(userManager, signInManager);

            var response = await service.Register(new RegistrationRequest
            {
                FirstName = "A",
                LastName = "B",
                Email = "new@test.com",
                UserName = "newuser",
                Password = "Str0ng!Pass"
            });

            response.UserId.ShouldNotBeNullOrEmpty();
            userManager.Verify(m => m.AddToRoleAsync(
                It.Is<ApplicationUser>(u => u.Email == "new@test.com"), "Employee"), Times.Once);
        }

        [Fact]
        public async Task Register_CreateFails_ThrowsBadRequestExceptionAndDoesNotAssignRole()
        {
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError
                {
                    Code = "DuplicateEmail",
                    Description = "Email 'dup@test.com' is already taken."
                }));
            var signInManager = IdentityMocks.MockSignInManager(userManager.Object);

            var service = MakeService(userManager, signInManager);

            var request = new RegistrationRequest
            {
                FirstName = "A",
                LastName = "B",
                Email = "dup@test.com",
                UserName = "dupuser",
                Password = "Str0ng!Pass"
            };

            var ex = await Should.ThrowAsync<BadRequestException>(() => service.Register(request));

            ex.Message.ShouldContain("already taken");
            userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }
    }
}
