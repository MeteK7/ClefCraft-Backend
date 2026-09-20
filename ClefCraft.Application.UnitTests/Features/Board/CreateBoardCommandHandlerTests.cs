using AutoMapper;
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
        ) MakeHandler()
        {
            var boardRepo = new Mock<IBoardRepository>();
            boardRepo.Setup(r => r.CreateAsync(It.IsAny<ClefCraft.Domain.Board>()))
                .Callback<ClefCraft.Domain.Board>(b => b.Id = 42)
                .Returns(Task.CompletedTask);

            var memberRepo = new Mock<IBoardMemberRepository>();

            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<BoardDto>(It.IsAny<ClefCraft.Domain.Board>()))
                .Returns((ClefCraft.Domain.Board b) => new BoardDto { Id = b.Id, Title = b.Title, OwnerUserId = b.OwnerUserId });

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(OwnerId);

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new CreateBoardCommandHandler(
                boardRepo.Object, memberRepo.Object, mapper.Object, userService.Object, unitOfWork.Object);

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
