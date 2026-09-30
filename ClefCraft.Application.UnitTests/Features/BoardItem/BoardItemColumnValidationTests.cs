using AutoMapper;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.UnitTests.Mocks;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItem
{
    // An item lives on a column of its own board. Columns belong to boards only through
    // BoardColumnMapping, so a column id from another board must be refused. Status and priority
    // are deliberately not checked here (their availability is a separate taxonomy concern).
    public class BoardItemColumnValidationTests
    {
        private const int BoardId = 10;
        private const int OwnColumn = 3;
        private const int OtherOwnColumn = 4;
        private const int OtherBoardsColumn = 99;

        private static Mock<IBoardItemRepository> RepoWithColumnsOfBoard10()
        {
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.IsColumnOnBoardAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((int boardId, int columnId) => boardId == BoardId && (columnId == OwnColumn || columnId == OtherOwnColumn));
            return repo;
        }

        private static Mock<IUserService> Caller()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("user-1");
            return userService;
        }

        private static UpdateBoardItemCommandHandler UpdateHandler(Mock<IBoardItemRepository> repo, Mock<IUnitOfWork> unitOfWork) =>
            new(
                repo.Object,
                MockAccessServices.GetMockBoardAccessService(authorized: true).Object,
                new Mock<IStatusRepository>().Object,
                new Mock<IPriorityRepository>().Object,
                new Mock<ITagRepository>().Object,
                new Mock<IMapper>().Object,
                Caller().Object,
                new Mock<ITaskLifecycleService>().Object,
                unitOfWork.Object,
                new Mock<IBoardMemberRepository>().Object);

        private static Domain.BoardItem ItemOnBoard(int boardId, int columnId) =>
            new() { Id = 1, BoardId = boardId, BoardColumnId = columnId, Title = "Etude" };

        [Fact]
        public async Task Create_InAnotherBoardsColumn_Returns400_AndSavesNothing()
        {
            var repo = RepoWithColumnsOfBoard10();
            var unitOfWork = new Mock<IUnitOfWork>();
            var handler = new CreateBoardItemCommandHandler(
                repo.Object, MockAccessServices.GetMockBoardAccessService(authorized: true).Object,
                new Mock<IMapper>().Object, Caller().Object, new Mock<ITaskLifecycleService>().Object, unitOfWork.Object);

            var ex = await Should.ThrowAsync<BadRequestException>(() => handler.Handle(
                new CreateBoardItemCommand { Title = "Etude", BoardId = BoardId, BoardColumnId = OtherBoardsColumn, StatusId = 1, PriorityId = 1 },
                CancellationToken.None));

            ex.Message.ShouldBe("The column does not belong to this board.");
            repo.Verify(r => r.AddBoardItem(It.IsAny<Domain.BoardItem>()), Times.Never);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Move_ToAnotherBoardsColumn_Returns400_AndSavesNothing()
        {
            var item = ItemOnBoard(BoardId, OwnColumn);
            var repo = RepoWithColumnsOfBoard10();
            repo.Setup(r => r.GetBoardItemById(1)).ReturnsAsync(item);
            var unitOfWork = new Mock<IUnitOfWork>();

            await Should.ThrowAsync<BadRequestException>(() => UpdateHandler(repo, unitOfWork).Handle(
                new UpdateBoardItemCommand { Id = 1, BoardColumnId = OtherBoardsColumn }, CancellationToken.None));

            item.BoardColumnId.ShouldBe(OwnColumn);
            repo.Verify(r => r.UpdateBoardItem(It.IsAny<Domain.BoardItem>()), Times.Never);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Move_WithinTheItemsOwnBoard_IsAllowed()
        {
            var item = ItemOnBoard(BoardId, OwnColumn);
            var repo = RepoWithColumnsOfBoard10();
            repo.Setup(r => r.GetBoardItemById(1)).ReturnsAsync(item);

            await UpdateHandler(repo, new Mock<IUnitOfWork>()).Handle(
                new UpdateBoardItemCommand { Id = 1, BoardColumnId = OtherOwnColumn }, CancellationToken.None);

            item.BoardColumnId.ShouldBe(OtherOwnColumn);
        }

        [Fact]
        public async Task ExistingOrphanedItem_WithItsColumnUnchanged_StaysEditable()
        {
            // Like the orphaned local items: on board 5 but in board 10's column 3. Editing it
            // without moving it must keep working; its column is not re-checked.
            var item = ItemOnBoard(5, OwnColumn);
            var repo = RepoWithColumnsOfBoard10();
            repo.Setup(r => r.GetBoardItemById(1)).ReturnsAsync(item);

            await UpdateHandler(repo, new Mock<IUnitOfWork>()).Handle(
                new UpdateBoardItemCommand { Id = 1, Title = "Renamed", BoardColumnId = OwnColumn }, CancellationToken.None);

            item.Title.ShouldBe("Renamed");
            item.BoardColumnId.ShouldBe(OwnColumn);
            repo.Verify(r => r.IsColumnOnBoardAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }
    }
}
