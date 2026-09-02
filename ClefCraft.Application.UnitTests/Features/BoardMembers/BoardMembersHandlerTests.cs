using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardMembers.Commands.AddMember;
using ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember;
using ClefCraft.Application.Features.BoardMembers.Queries.GetMembers;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardMembers
{
    // Regression coverage for the team-membership follow-up fix: boards are now
    // team-based via BoardMember rather than single-owner, and OwnerUserId is
    // creator metadata used only to gate membership management itself.
    public class BoardMembersHandlerTests
    {
        private const string OwnerId = "user-owner";
        private const string OtherMemberId = "user-teammate";
        private const int BoardId = 1;

        [Fact]
        public async Task AddMember_CallerIsOwner_AddsMemberAndReturnsDto()
        {
            var memberRepo = new Mock<IBoardMemberRepository>();
            memberRepo.Setup(r => r.GetByBoardAndUserAsync(BoardId, OtherMemberId)).ReturnsAsync((BoardMember?)null);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.GetUser(OtherMemberId)).ReturnsAsync(new User { Id = OtherMemberId, Firstname = "Jane", Lastname = "Doe" });

            var handler = new AddBoardMemberCommandHandler(memberRepo.Object, accessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            var result = await handler.Handle(
                new AddBoardMemberCommand { BoardId = BoardId, UserId = OtherMemberId, RequestingUserId = OwnerId },
                CancellationToken.None);

            result.UserId.ShouldBe(OtherMemberId);
            result.FullName.ShouldBe("Jane Doe");
            memberRepo.Verify(r => r.CreateAsync(It.Is<BoardMember>(m => m.BoardId == BoardId && m.UserId == OtherMemberId)), Times.Once);
        }

        [Fact]
        public async Task AddMember_CallerIsNotOwner_ThrowsForbiddenAccessException()
        {
            var memberRepo = new Mock<IBoardMemberRepository>();
            var accessService = new Mock<Contracts.Authorization.IBoardAccessService>();
            accessService.Setup(s => s.EnsureUserIsBoardOwnerAsync(BoardId, OtherMemberId)).ThrowsAsync(new ForbiddenAccessException());

            var handler = new AddBoardMemberCommandHandler(memberRepo.Object, accessService.Object, new Mock<IUserService>().Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(
                    new AddBoardMemberCommand { BoardId = BoardId, UserId = "user-new", RequestingUserId = OtherMemberId },
                    CancellationToken.None));

            memberRepo.Verify(r => r.CreateAsync(It.IsAny<BoardMember>()), Times.Never);
        }

        [Fact]
        public async Task AddMember_AlreadyAMember_ThrowsBadRequestException()
        {
            var memberRepo = new Mock<IBoardMemberRepository>();
            memberRepo.Setup(r => r.GetByBoardAndUserAsync(BoardId, OtherMemberId))
                .ReturnsAsync(new BoardMember { BoardId = BoardId, UserId = OtherMemberId });

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);

            var handler = new AddBoardMemberCommandHandler(memberRepo.Object, accessService.Object, new Mock<IUserService>().Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<BadRequestException>(() =>
                handler.Handle(
                    new AddBoardMemberCommand { BoardId = BoardId, UserId = OtherMemberId, RequestingUserId = OwnerId },
                    CancellationToken.None));

            memberRepo.Verify(r => r.CreateAsync(It.IsAny<BoardMember>()), Times.Never);
        }

        [Fact]
        public async Task RemoveMember_TargetIsBoardOwner_ThrowsBadRequestException()
        {
            var memberRepo = new Mock<IBoardMemberRepository>();
            var boardRepo = new Mock<IBoardRepository>();
            boardRepo.Setup(r => r.GetByIdReadOnlyAsync(BoardId)).ReturnsAsync(new Board { Id = BoardId, OwnerUserId = OwnerId });

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);

            var handler = new RemoveBoardMemberCommandHandler(memberRepo.Object, boardRepo.Object, accessService.Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<BadRequestException>(() =>
                handler.Handle(
                    new RemoveBoardMemberCommand { BoardId = BoardId, UserId = OwnerId, RequestingUserId = OwnerId },
                    CancellationToken.None));

            memberRepo.Verify(r => r.DeleteAsync(It.IsAny<BoardMember>()), Times.Never);
        }

        [Fact]
        public async Task RemoveMember_ValidTeammate_RemovesMembership()
        {
            var membership = new BoardMember { Id = 5, BoardId = BoardId, UserId = OtherMemberId };

            var memberRepo = new Mock<IBoardMemberRepository>();
            memberRepo.Setup(r => r.GetByBoardAndUserAsync(BoardId, OtherMemberId)).ReturnsAsync(membership);

            var boardRepo = new Mock<IBoardRepository>();
            boardRepo.Setup(r => r.GetByIdReadOnlyAsync(BoardId)).ReturnsAsync(new Board { Id = BoardId, OwnerUserId = OwnerId });

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);

            var handler = new RemoveBoardMemberCommandHandler(memberRepo.Object, boardRepo.Object, accessService.Object, new Mock<IUnitOfWork>().Object);

            await handler.Handle(
                new RemoveBoardMemberCommand { BoardId = BoardId, UserId = OtherMemberId, RequestingUserId = OwnerId },
                CancellationToken.None);

            memberRepo.Verify(r => r.DeleteAsync(membership), Times.Once);
        }

        [Fact]
        public async Task GetMembers_CallerIsAnyMember_ReturnsResolvedNames()
        {
            var members = new List<BoardMember>
            {
                new BoardMember { Id = 1, BoardId = BoardId, UserId = OwnerId },
                new BoardMember { Id = 2, BoardId = BoardId, UserId = OtherMemberId }
            };

            var memberRepo = new Mock<IBoardMemberRepository>();
            memberRepo.Setup(r => r.GetByBoardIdAsync(BoardId)).ReturnsAsync(members);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.GetUsersByIds(It.IsAny<List<string>>())).ReturnsAsync(new List<User>
            {
                new User { Id = OwnerId, Firstname = "Own", Lastname = "Er" },
                new User { Id = OtherMemberId, Firstname = "Jane", Lastname = "Doe" }
            });

            var handler = new GetBoardMembersQueryHandler(memberRepo.Object, accessService.Object, userService.Object);

            var result = await handler.Handle(new GetBoardMembersQuery { BoardId = BoardId, UserId = OtherMemberId }, CancellationToken.None);

            result.Count.ShouldBe(2);
            result.ShouldContain(m => m.UserId == OtherMemberId && m.FullName == "Jane Doe");
        }

        [Fact]
        public async Task GetMembers_CallerIsNotAMember_ThrowsForbiddenAccessException()
        {
            var memberRepo = new Mock<IBoardMemberRepository>();
            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);

            var handler = new GetBoardMembersQueryHandler(memberRepo.Object, accessService.Object, new Mock<IUserService>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetBoardMembersQuery { BoardId = BoardId, UserId = "user-outsider" }, CancellationToken.None));

            memberRepo.Verify(r => r.GetByBoardIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
