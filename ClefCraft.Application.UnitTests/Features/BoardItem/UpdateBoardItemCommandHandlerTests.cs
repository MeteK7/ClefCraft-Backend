using AutoMapper;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItem
{
    // Business-logic coverage for UpdateBoardItemCommandHandler: tag diffing and the
    // COMPLETED_STATUS_ID (3) completion/reopen lifecycle transitions. Authorization
    // is already covered by BoardItemAuthorizationTests, so it's mocked as allowed here.
    public class UpdateBoardItemCommandHandlerTests
    {
        private const int CompletedStatusId = 3;

        private static Domain.BoardItem MakeItem(int statusId = 1, params int[] tagIds) => new Domain.BoardItem
        {
            Id = 1,
            BoardId = 10,
            Title = "Practice scales",
            BoardItemStatus = new BoardItemStatus { BoardItemId = 1, StatusId = statusId },
            BoardItemTags = tagIds.Select(t => new BoardItemTag { BoardItemId = 1, TagId = t }).ToList()
        };

        private static UpdateBoardItemCommandHandler MakeHandler(
            Mock<IBoardItemRepository> repo,
            Mock<ITaskLifecycleService> lifecycleService,
            Mock<IUserService>? userService = null) =>
            new UpdateBoardItemCommandHandler(
                repo.Object,
                MockAccessServices.GetMockBoardAccessService(authorized: true).Object,
                new Mock<IStatusRepository>().Object,
                new Mock<IPriorityRepository>().Object,
                new Mock<ITagRepository>().Object,
                new Mock<IMapper>().Object,
                (userService ?? MakeUserService()).Object,
                lifecycleService.Object,
                new Mock<IUnitOfWork>().Object);

        private static Mock<IUserService> MakeUserService()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("user-1");
            return userService;
        }

        private static Mock<IBoardItemRepository> MakeRepoReturning(Domain.BoardItem item)
        {
            var repo = new Mock<IBoardItemRepository>();
            repo.Setup(r => r.GetBoardItemById(item.Id)).ReturnsAsync(item);
            repo.Setup(r => r.UpdateBoardItem(It.IsAny<Domain.BoardItem>())).Returns(Task.CompletedTask);
            return repo;
        }

        [Fact]
        public async Task Handle_TagIdsProvided_AddsNewTagsAndRemovesMissingOnes()
        {
            var item = MakeItem(tagIds: new[] { 1, 2 });
            var repo = MakeRepoReturning(item);
            var lifecycleService = new Mock<ITaskLifecycleService>();

            var handler = MakeHandler(repo, lifecycleService);

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, TagIds = new List<int> { 2, 3 } }, CancellationToken.None);

            item.BoardItemTags.Select(t => t.TagId).OrderBy(id => id).ShouldBe(new[] { 2, 3 });
        }

        [Fact]
        public async Task Handle_TagIdsNotProvided_LeavesExistingTagsUnchanged()
        {
            var item = MakeItem(tagIds: new[] { 1, 2 });
            var repo = MakeRepoReturning(item);
            var lifecycleService = new Mock<ITaskLifecycleService>();

            var handler = MakeHandler(repo, lifecycleService);

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, TagIds = null }, CancellationToken.None);

            item.BoardItemTags.Select(t => t.TagId).OrderBy(id => id).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public async Task Handle_StatusChangesToCompleted_RecordsCompletionButNotReopen()
        {
            var item = MakeItem(statusId: 1);
            var repo = MakeRepoReturning(item);
            var lifecycleService = new Mock<ITaskLifecycleService>();

            var handler = MakeHandler(repo, lifecycleService);

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, StatusId = CompletedStatusId }, CancellationToken.None);

            lifecycleService.Verify(l => l.RecordCompletionAsync(1), Times.Once);
            lifecycleService.Verify(l => l.RecordReopenAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_StatusChangesFromCompletedToOther_RecordsReopenButNotCompletion()
        {
            var item = MakeItem(statusId: CompletedStatusId);
            var repo = MakeRepoReturning(item);
            var lifecycleService = new Mock<ITaskLifecycleService>();

            var handler = MakeHandler(repo, lifecycleService);

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, StatusId = 1 }, CancellationToken.None);

            lifecycleService.Verify(l => l.RecordReopenAsync(1), Times.Once);
            lifecycleService.Verify(l => l.RecordCompletionAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NonStatusFieldUpdate_DoesNotTriggerCompletionOrReopen()
        {
            var item = MakeItem(statusId: 1);
            var repo = MakeRepoReturning(item);
            var lifecycleService = new Mock<ITaskLifecycleService>();

            var handler = MakeHandler(repo, lifecycleService);

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, Title = "Renamed", StatusId = null }, CancellationToken.None);

            lifecycleService.Verify(l => l.RecordCompletionAsync(It.IsAny<int>()), Times.Never);
            lifecycleService.Verify(l => l.RecordReopenAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AssigneeChanges_RecordsAssigneeChange()
        {
            var item = MakeItem();
            item.AssigneeId = "user-old";
            var repo = MakeRepoReturning(item);
            var lifecycleService = new Mock<ITaskLifecycleService>();

            var handler = MakeHandler(repo, lifecycleService);

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, AssigneeId = "user-new" }, CancellationToken.None);

            lifecycleService.Verify(l => l.RecordAssigneeChangeAsync(1), Times.Once);
        }
    }
}
