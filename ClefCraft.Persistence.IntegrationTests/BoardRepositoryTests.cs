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
    // Regression coverage for the P0 authorization fix (GetBoards used to return every
    // board in the system to every user with no filter) and the follow-up team-membership
    // fix (Boards are team-based via BoardMember, not single-owner — OwnerUserId is
    // creator metadata only and no longer gates visibility).
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
        public async Task GetBoards_ReturnsOnlyBoardsTheUserIsAMemberOf()
        {
            var context = CreateContext();
            var repository = new BoardRepository(context);

            var ownBoard = new Board { Title = "My Practice Log", OwnerUserId = "user-a" };
            var otherBoard = new Board { Title = "Someone Else's Board", OwnerUserId = "user-b" };
            await context.Boards.AddRangeAsync(ownBoard, otherBoard);
            await context.SaveChangesAsync();

            await context.BoardMembers.AddRangeAsync(
                new BoardMember { BoardId = ownBoard.Id, UserId = "user-a" },
                new BoardMember { BoardId = otherBoard.Id, UserId = "user-b" });
            await context.SaveChangesAsync();

            var result = await repository.GetBoards("user-a");

            result.ShouldContain(b => b.Id == ownBoard.Id);
            result.ShouldNotContain(b => b.Id == otherBoard.Id);
        }

        [Fact]
        public async Task GetBoards_NonOwnerTeamMember_CanSeeTheBoard()
        {
            // The actual regression this fix targets: a teammate who isn't the
            // OwnerUserId must still see a board they've been added to.
            var context = CreateContext();
            var repository = new BoardRepository(context);

            var teamBoard = new Board { Title = "AI Platform Sprint", OwnerUserId = "user-owner" };
            await context.Boards.AddAsync(teamBoard);
            await context.SaveChangesAsync();

            await context.BoardMembers.AddRangeAsync(
                new BoardMember { BoardId = teamBoard.Id, UserId = "user-owner" },
                new BoardMember { BoardId = teamBoard.Id, UserId = "user-teammate" });
            await context.SaveChangesAsync();

            var result = await repository.GetBoards("user-teammate");

            result.ShouldContain(b => b.Id == teamBoard.Id);
        }

        [Fact]
        public async Task GetBoards_UserWithNoMemberships_ReturnsEmptyList()
        {
            var context = CreateContext();
            var repository = new BoardRepository(context);

            var otherBoard = new Board { Title = "Someone Else's Board", OwnerUserId = "user-b" };
            await context.Boards.AddAsync(otherBoard);
            await context.SaveChangesAsync();

            await context.BoardMembers.AddAsync(new BoardMember { BoardId = otherBoard.Id, UserId = "user-b" });
            await context.SaveChangesAsync();

            var result = await repository.GetBoards("user-a");

            result.ShouldBeEmpty();
        }
    }
}
