using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Commands.DeleteBoardItem;
using ClefCraft.Application.UnitTests.Mocks;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItem
{
    // Business-logic coverage for DeleteBoardItemCommandHandler: the guard against deleting
    // an item still linked to calendar events, and lifecycle/repository cleanup on success.
    // Authorization is already covered by BoardItemAuthorizationTests, so it's mocked as allowed here.
    public class DeleteBoardItemCommandHandlerTests
    {
        private static Domain.BoardItem MakeItem(int id = 1, int boardId = 10) =>
            new Domain.BoardItem { Id = id, BoardId = boardId, Title = "Practice scales" };

        private static DeleteBoardItemCommandHandler MakeHandler(
            Mock<IBoardItemRepository> repo,
            Mock<ICalendarEventRepository> calendarRepo,
            Mock<ITaskLifecycleService> lifecycleService,
            Mock<IUnitOfWork> unitOfWork)
        {
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("user-1");

            return new DeleteBoardItemCommandHandler(
                repo.Object,
                MockAccessServices.GetMockBoardAccessService(authorized: true).Object,
                userService.Object,
                calendarRepo.Object,
                lifecycleService.Object,
                unitOfWork.Object);
        }

        [Fact]
        public async Task Handle_ItemLinkedToCalendarEvents_ThrowsBadRequestException_AndDoesNotDelete()
        {
            var item = MakeItem();
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);

            var calendarRepo = new Mock<ICalendarEventRepository>();
            calendarRepo.Setup(r => r.GetWorkHistoryByItemIdAsync(item.Id))
                .ReturnsAsync(new List<Domain.CalendarEvent> { new Domain.CalendarEvent() });

            var lifecycleService = new Mock<ITaskLifecycleService>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = MakeHandler(repo, calendarRepo, lifecycleService, unitOfWork);

            await Should.ThrowAsync<BadRequestException>(() =>
                handler.Handle(new DeleteBoardItemCommand { Id = item.Id }, CancellationToken.None));

            repo.Verify(r => r.DeleteAsync(It.IsAny<Domain.BoardItem>()), Times.Never);
            lifecycleService.Verify(l => l.DeleteAsync(It.IsAny<int>()), Times.Never);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ItemNotLinkedToCalendarEvents_DeletesLifecycleAndItem()
        {
            var item = MakeItem();
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.GetByIdAsync(item.Id)).ReturnsAsync(item);
            repo.Setup(r => r.DeleteAsync(item)).Returns(Task.CompletedTask);

            var calendarRepo = new Mock<ICalendarEventRepository>();
            calendarRepo.Setup(r => r.GetWorkHistoryByItemIdAsync(item.Id))
                .ReturnsAsync(new List<Domain.CalendarEvent>());

            var lifecycleService = new Mock<ITaskLifecycleService>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = MakeHandler(repo, calendarRepo, lifecycleService, unitOfWork);

            await handler.Handle(new DeleteBoardItemCommand { Id = item.Id }, CancellationToken.None);

            lifecycleService.Verify(l => l.DeleteAsync(item.Id), Times.Once);
            repo.Verify(r => r.DeleteAsync(item), Times.Once);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ItemDoesNotExist_ThrowsNotFoundException()
        {
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Domain.BoardItem)null!);

            var calendarRepo = new Mock<ICalendarEventRepository>();
            var lifecycleService = new Mock<ITaskLifecycleService>();
            var unitOfWork = new Mock<IUnitOfWork>();

            var handler = MakeHandler(repo, calendarRepo, lifecycleService, unitOfWork);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new DeleteBoardItemCommand { Id = 999 }, CancellationToken.None));
        }
    }
}
