using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Queries.GetUserFullName;
using ClefCraft.Application.Models.Identity;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItem
{
    // Regression coverage for the P0 follow-up: GetUserFullName let any authenticated user
    // resolve any other user's name by ID with no relationship check at all.
    public class GetUserFullNameHandlerTests
    {
        private const string CallerUserId = "user-1";

        [Fact]
        public async Task Handle_TargetSharesNoBoardWithCaller_ThrowsForbiddenAccessException()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            var boardMemberRepo = new Mock<IBoardMemberRepository>();
            boardMemberRepo.Setup(r => r.ShareAnyBoardAsync(CallerUserId, "stranger")).ReturnsAsync(false);

            var handler = new GetUserFullNameHandler(userService.Object, boardMemberRepo.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetUserFullNameQuery("stranger"), CancellationToken.None));

            userService.Verify(u => u.GetUser(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_TargetSharesABoardWithCaller_ReturnsFullName()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            userService.Setup(u => u.GetUser("teammate")).ReturnsAsync(new User
            {
                Id = "teammate",
                Firstname = "Ada",
                Lastname = "Lovelace"
            });
            var boardMemberRepo = new Mock<IBoardMemberRepository>();
            boardMemberRepo.Setup(r => r.ShareAnyBoardAsync(CallerUserId, "teammate")).ReturnsAsync(true);

            var handler = new GetUserFullNameHandler(userService.Object, boardMemberRepo.Object);

            var result = await handler.Handle(new GetUserFullNameQuery("teammate"), CancellationToken.None);

            result.ShouldBe("Ada Lovelace");
        }

        [Fact]
        public async Task Handle_TargetIsCaller_NeverChecksSharedBoards()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            userService.Setup(u => u.GetUser(CallerUserId)).ReturnsAsync(new User
            {
                Id = CallerUserId,
                Firstname = "Self",
                Lastname = "User"
            });
            var boardMemberRepo = new Mock<IBoardMemberRepository>();

            var handler = new GetUserFullNameHandler(userService.Object, boardMemberRepo.Object);

            var result = await handler.Handle(new GetUserFullNameQuery(CallerUserId), CancellationToken.None);

            result.ShouldBe("Self User");
            boardMemberRepo.Verify(r => r.ShareAnyBoardAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
