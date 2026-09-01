using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Regression coverage for the P0 authorization fix: GetBoards used to return
    // every board in the system to every authenticated user with no filter at all.
    public class BoardRepositoryTests
    {
        private static ClefCraftDatabaseContext CreateContext(string userId = "test-user")
        {
            var options = new DbContextOptionsBuilder<ClefCraftDatabaseContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(u => u.UserId).Returns(userId);

            return new ClefCraftDatabaseContext(options, userServiceMock.Object);
        }

        [Fact]
        public async Task GetBoards_ReturnsOnlyBoardsOwnedByRequestingUser()
        {
            var context = CreateContext();
            var repository = new BoardRepository(context);

            var ownBoard = new Board { Title = "My Practice Log", OwnerUserId = "user-a" };
            var otherBoard = new Board { Title = "Someone Else's Board", OwnerUserId = "user-b" };
            await context.Boards.AddRangeAsync(ownBoard, otherBoard);
            await context.SaveChangesAsync();

            var result = await repository.GetBoards("user-a");

            result.ShouldContain(b => b.Id == ownBoard.Id);
            result.ShouldNotContain(b => b.Id == otherBoard.Id);
        }

        [Fact]
        public async Task GetBoards_UserWithNoBoards_ReturnsEmptyList()
        {
            var context = CreateContext();
            var repository = new BoardRepository(context);

            await context.Boards.AddAsync(new Board { Title = "Someone Else's Board", OwnerUserId = "user-b" });
            await context.SaveChangesAsync();

            var result = await repository.GetBoards("user-a");

            result.ShouldBeEmpty();
        }
    }
}
