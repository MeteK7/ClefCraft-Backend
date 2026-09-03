using ClefCraft.Identity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ClefCraft.Identity.UnitTests.Mocks
{
    // UserManager<T>/SignInManager<T> have no interfaces, so AuthService/UserService
    // depend on the concrete classes directly. Both are mockable because every member
    // used in production code is virtual - this just satisfies their constructors with
    // the minimum non-null args they each require.
    public static class IdentityMocks
    {
        public static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        public static Mock<SignInManager<ApplicationUser>> MockSignInManager(UserManager<ApplicationUser> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            return new Mock<SignInManager<ApplicationUser>>(
                userManager, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }
    }
}
