using AutoMapper;
using ClefCraft.Application.Features.Board.Commands.CreateBoard;
using ClefCraft.Domain;
using ClefCraft.Persistence.Repositories;
using ClefCraft.Persistence.UnitOfWork;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.IntegrationTests
{
    // IsMemberAsync has no special case for Board.OwnerUserId: an owner is a member only through
    // a BoardMembers row. That's what makes the owner assignable and what the
    // BackfillBoardOwnerMemberships migration guarantees for every board.
    public class BoardMemberRepositoryTests
    {
        [Fact]
        public async Task IsMemberAsync_OwnerOfABoardCreatedThroughTheApp_IsAMember()
        {
            var userService = DatabaseContextFactory.CreateUserServiceMock("owner-1");
            var context = DatabaseContextFactory.CreateContext("owner-1");
            var memberRepository = new BoardMemberRepository(context);
            var handler = new CreateBoardCommandHandler(
                new BoardRepository(context),
                memberRepository,
                new Mock<IMapper>().Object,
                userService.Object,
                new EfUnitOfWork(context));

            await handler.Handle(new CreateBoardCommand { Title = "Scales" }, CancellationToken.None);
            var boardId = context.Boards.Single().Id;

            (await memberRepository.IsMemberAsync(boardId, "owner-1")).ShouldBeTrue();
        }

        [Fact]
        public async Task IsMemberAsync_OwnerWithoutAMembershipRow_IsNotAMember()
        {
            var context = DatabaseContextFactory.CreateContext();
            var board = new Board { Title = "Inserted outside the app", OwnerUserId = "owner-1" };
            context.Boards.Add(board);
            await context.SaveChangesAsync();

            (await new BoardMemberRepository(context).IsMemberAsync(board.Id, "owner-1")).ShouldBeFalse();
        }
    }
}
