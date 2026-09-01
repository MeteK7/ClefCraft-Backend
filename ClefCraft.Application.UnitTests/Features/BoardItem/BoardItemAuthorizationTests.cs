using AutoMapper;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem;
using ClefCraft.Application.Features.BoardItem.Commands.DeleteBoardItem;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItem
{
    // Regression coverage for the P0 authorization fix: a BoardItem's owner is
    // resolved transitively through its parent Board (Board.OwnerUserId), so
    // every handler here must reject access from a user who isn't the caller
    // ("user-1") before touching any other dependency.
    public class BoardItemAuthorizationTests
    {
        private const string CallerUserId = "user-1";

        private static Domain.BoardItem MakeItem(int id = 1, int boardId = 10) =>
            new Domain.BoardItem { Id = id, BoardId = boardId, Title = "Practice scales" };

        [Fact]
        public async Task GetBoardItemById_NotOwnedByCaller_ThrowsForbiddenAccessException()
        {
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.GetBoardItemById(1)).ReturnsAsync(MakeItem());

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            var mapper = new Mock<IMapper>();

            var handler = new GetBoardItemByIdHandler(repo.Object, accessService.Object, userService.Object, mapper.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetBoardItemByIdQuery(1), CancellationToken.None));

            repo.Verify(r => r.GetBoardItemById(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetBoardItemById_OwnedByCaller_ReturnsMappedItem()
        {
            var item = MakeItem();
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.GetBoardItemById(1)).ReturnsAsync(item);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<BoardItemByIdDto>(item)).Returns(new BoardItemByIdDto { Id = item.Id });

            var handler = new GetBoardItemByIdHandler(repo.Object, accessService.Object, userService.Object, mapper.Object);

            var result = await handler.Handle(new GetBoardItemByIdQuery(1), CancellationToken.None);

            result.Id.ShouldBe(1);
        }

        [Fact]
        public async Task UpdateBoardItem_NotOwnedByCaller_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var item = MakeItem();
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.GetBoardItemById(1)).ReturnsAsync(item);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateBoardItemCommandHandler(
                repo.Object,
                accessService.Object,
                new Mock<IStatusRepository>().Object,
                new Mock<IPriorityRepository>().Object,
                new Mock<ITagRepository>().Object,
                new Mock<IMapper>().Object,
                userService.Object,
                new Mock<ITaskLifecycleService>().Object,
                new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new UpdateBoardItemCommand { Id = 1, Title = "Hacked title" }, CancellationToken.None));

            repo.Verify(r => r.UpdateBoardItem(It.IsAny<Domain.BoardItem>()), Times.Never);
        }

        [Fact]
        public async Task DeleteBoardItem_NotOwnedByCaller_ThrowsForbiddenAccessException_BeforeAnyDelete()
        {
            var item = MakeItem();
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new DeleteBoardItemCommandHandler(
                repo.Object,
                accessService.Object,
                userService.Object,
                new Mock<Application.Contracts.Calendar.ICalendarEventRepository>().Object,
                new Mock<ITaskLifecycleService>().Object,
                new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new DeleteBoardItemCommand { Id = 1 }, CancellationToken.None));

            repo.Verify(r => r.DeleteAsync(It.IsAny<Domain.BoardItem>()), Times.Never);
        }

        [Fact]
        public async Task CreateBoardItem_TargetBoardNotOwnedByCaller_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var repo = new Mock<IBoardItemRepository>();
            var accessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new CreateBoardItemCommandHandler(
                repo.Object,
                accessService.Object,
                new Mock<IMapper>().Object,
                userService.Object,
                new Mock<ITaskLifecycleService>().Object,
                new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new CreateBoardItemCommand { Title = "New item", BoardId = 999 }, CancellationToken.None));

            repo.Verify(r => r.AddBoardItem(It.IsAny<Domain.BoardItem>()), Times.Never);
        }
    }
}
