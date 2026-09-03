using ClefCraft.Identity.Models;
using ClefCraft.Identity.Services;
using ClefCraft.Identity.UnitTests.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Shouldly;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClefCraft.Identity.UnitTests.Services
{
    public class UserServiceTests
    {
        private static (UserService service, Mock<IHttpContextAccessor> contextAccessor) MakeService(
            Mock<UserManager<ApplicationUser>> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            return (new UserService(userManager.Object, contextAccessor.Object), contextAccessor);
        }

        [Fact]
        public void UserId_ReadsFromUidClaim_WhenHttpContextHasIt()
        {
            var userManager = IdentityMocks.MockUserManager();
            var (service, contextAccessor) = MakeService(userManager);

            var identity = new ClaimsIdentity(new[] { new Claim("uid", "user-99") });
            contextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            });

            service.UserId.ShouldBe("user-99");
        }

        [Fact]
        public void UserId_ReturnsNull_WhenHttpContextIsNull()
        {
            var userManager = IdentityMocks.MockUserManager();
            var (service, contextAccessor) = MakeService(userManager);

            contextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null!);

            service.UserId.ShouldBeNull();
        }

        [Fact]
        public void UserId_ReturnsNull_WhenUidClaimIsAbsent()
        {
            var userManager = IdentityMocks.MockUserManager();
            var (service, contextAccessor) = MakeService(userManager);

            contextAccessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            });

            service.UserId.ShouldBeNull();
        }

        [Fact]
        public async Task GetUser_MapsUserManagerResultToApplicationUser()
        {
            var user = new ApplicationUser { Id = "user-7", Email = "seven@test.com", FirstName = "Seven", LastName = "Samurai" };
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByIdAsync("user-7")).ReturnsAsync(user);
            var (service, _) = MakeService(userManager);

            var result = await service.GetUser("user-7");

            result.Id.ShouldBe("user-7");
            result.Email.ShouldBe("seven@test.com");
            result.Firstname.ShouldBe("Seven");
            result.Lastname.ShouldBe("Samurai");
        }

        [Fact]
        public async Task GetAssignee_ReturnsNull_WhenUserDoesNotExist()
        {
            var userManager = IdentityMocks.MockUserManager();
            userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser)null!);
            var (service, _) = MakeService(userManager);

            var result = await service.GetAssignee("missing");

            result.ShouldBeNull();
        }
    }
}
