using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Board.Commands.CreateBoard;
using ClefCraft.Application.Features.Board.Queries.GetBoards;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Board
{
    public class CreateBoardCommandHandlerTests
    {
        private const string OwnerId = "user-owner";

        private static (
            CreateBoardCommandHandler Handler,
            Mock<IBoardRepository> BoardRepo,
            Mock<IBoardMemberRepository> MemberRepo
        ) MakeHandler() => MakeHandler(new Mock<IGenericRepository<BoardColumnMapping>>());

        private static (
            CreateBoardCommandHandler Handler,
            Mock<IBoardRepository> BoardRepo,
            Mock<IBoardMemberRepository> MemberRepo
        ) MakeHandler(Mock<IGenericRepository<BoardColumnMapping>> columnMappingRepo)
        {
            var boardRepo = new Mock<IBoardRepository>();
            boardRepo.Setup(r => r.CreateAsync(It.IsAny<ClefCraft.Domain.Board>()))
                .Callback<ClefCraft.Domain.Board>(b => b.Id = 42)
                .Returns(Task.CompletedTask);

            var memberRepo = new Mock<IBoardMemberRepository>();


            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(OwnerId);

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new CreateBoardCommandHandler(
                boardRepo.Object, memberRepo.Object, userService.Object, unitOfWork.Object, columnMappingRepo.Object);

            return (handler, boardRepo, memberRepo);
        }

        [Fact]
        public async Task Handle_ValidTitle_CreatesBoardOwnedByCaller_AndAddsCallerAsMember()
        {
            var (handler, boardRepo, memberRepo) = MakeHandler();

            var result = await handler.Handle(new CreateBoardCommand { Title = "New Board" }, CancellationToken.None);

            boardRepo.Verify(r => r.CreateAsync(It.Is<ClefCraft.Domain.Board>(b =>
                b.Title == "New Board" && b.OwnerUserId == OwnerId)), Times.Once);

            memberRepo.Verify(r => r.CreateAsync(It.Is<BoardMember>(m =>
                m.BoardId == 42 && m.UserId == OwnerId)), Times.Once);

            result.Id.ShouldBe(42);
            result.Title.ShouldBe("New Board");
            result.OwnerUserId.ShouldBe(OwnerId);
        }

        [Fact]
        public async Task Handle_GivesTheNewBoardItsOwnFiveDefaultColumns_InLaneOrder()
        {
            var added = new List<BoardColumnMapping>();
            var columnMappingRepo = new Mock<IGenericRepository<BoardColumnMapping>>();
            columnMappingRepo.Setup(r => r.CreateAsync(It.IsAny<BoardColumnMapping>()))
                .Callback<BoardColumnMapping>(added.Add)
                .Returns(Task.CompletedTask);
            var (handler, _, _) = MakeHandler(columnMappingRepo);

            await handler.Handle(new CreateBoardCommand { Title = "New Board" }, CancellationToken.None);

            added.Select(m => m.BoardId).ShouldAllBe(id => id == 42);
            added.Select(m => m.BoardColumn.Title).ShouldBe(new[] { "Backlog", "To Do", "In Progress", "In Review", "Done" });
            added.Select(m => m.BoardColumn).Distinct().Count().ShouldBe(5); // new rows of its own, never shared
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Handle_MissingTitle_ThrowsBadRequestException_BeforeAnyWrite(string? title)
        {
            var (handler, boardRepo, memberRepo) = MakeHandler();

            await Should.ThrowAsync<BadRequestException>(() =>
                handler.Handle(new CreateBoardCommand { Title = title! }, CancellationToken.None));

            boardRepo.Verify(r => r.CreateAsync(It.IsAny<ClefCraft.Domain.Board>()), Times.Never);
            memberRepo.Verify(r => r.CreateAsync(It.IsAny<BoardMember>()), Times.Never);
        }
    }
}
