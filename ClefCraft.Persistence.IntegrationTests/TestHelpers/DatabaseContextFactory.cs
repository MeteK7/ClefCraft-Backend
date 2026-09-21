using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;

namespace ClefCraft.Persistence.IntegrationTests.TestHelpers
{
    public static class DatabaseContextFactory
    {
        public static ClefCraftDatabaseContext CreateContext(string userId = "test-user")
            => new(BuildOptions(), CreateUserServiceMock(userId).Object);

        public static Mock<IUserService> CreateUserServiceMock(string userId = "test-user")
        {
            var mock = new Mock<IUserService>();
            mock.Setup(u => u.UserId).Returns(userId);
            return mock;
        }

        private static DbContextOptions<ClefCraftDatabaseContext> BuildOptions()
            => new DbContextOptionsBuilder<ClefCraftDatabaseContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
    }
}
