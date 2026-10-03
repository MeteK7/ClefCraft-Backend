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
            Mock<IUserService>? userService = null,
            Mock<IBoardMemberRepository>? memberRepo = null,
            Mock<IUnitOfWork>? unitOfWork = null) =>
            new UpdateBoardItemCommandHandler(
                repo.Object,
                MockAccessServices.GetMockBoardAccessService(authorized: true).Object,
                new Mock<IStatusRepository>().Object,
                new Mock<IPriorityRepository>().Object,
                new Mock<ITagRepository>().Object,
                (userService ?? MakeUserService()).Object,
                lifecycleService.Object,
                (unitOfWork ?? new Mock<IUnitOfWork>()).Object,
                (memberRepo ?? new Mock<IBoardMemberRepository>()).Object);

        // Board 10 (MakeItem's board) has exactly these members.
        private static Mock<IBoardMemberRepository> MakeMemberRepo(params string[] memberIds)
        {
            var memberRepo = new Mock<IBoardMemberRepository>();
            memberRepo.Setup(m => m.IsMemberAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int boardId, string userId) => boardId == 10 && memberIds.Contains(userId));
            return memberRepo;
        }

        [Fact]
        public async Task Handle_AssigneeIsBoardMember_AssignsThem()
        {
            var item = MakeItem();
            var repo = MakeRepoReturning(item);
            var handler = MakeHandler(repo, new Mock<ITaskLifecycleService>(), memberRepo: MakeMemberRepo("member-1"));

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, AssigneeId = "member-1" }, CancellationToken.None);

            item.AssigneeId.ShouldBe("member-1");
        }

        [Fact]
        public async Task Handle_AssigneeIsNotBoardMember_ThrowsBadRequest_AndSavesNothing()
        {
            var item = MakeItem();
            item.AssigneeId = "member-1";
            var repo = MakeRepoReturning(item);
            var unitOfWork = new Mock<IUnitOfWork>();
            var handler = MakeHandler(repo, new Mock<ITaskLifecycleService>(),
                memberRepo: MakeMemberRepo("member-1"), unitOfWork: unitOfWork);

            await Should.ThrowAsync<ClefCraft.Application.Exceptions.BadRequestException>(() =>
                handler.Handle(new UpdateBoardItemCommand { Id = 1, Title = "Changed", AssigneeId = "outsider" }, CancellationToken.None));

            item.AssigneeId.ShouldBe("member-1");
            item.Title.ShouldBe("Practice scales");
            repo.Verify(r => r.UpdateBoardItem(It.IsAny<Domain.BoardItem>()), Times.Never);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnchangedAssigneeWhoLeftTheBoard_DoesNotBlockOtherEdits()
        {
            var item = MakeItem();
            item.AssigneeId = "former-member";
            var repo = MakeRepoReturning(item);
            var memberRepo = MakeMemberRepo("member-1");
            var handler = MakeHandler(repo, new Mock<ITaskLifecycleService>(), memberRepo: memberRepo);

            // The dialog sends the item's current assignee back unchanged on every save.
            await handler.Handle(new UpdateBoardItemCommand { Id = 1, Title = "Renamed", AssigneeId = "former-member" }, CancellationToken.None);

            item.Title.ShouldBe("Renamed");
            item.AssigneeId.ShouldBe("former-member");
            memberRepo.Verify(m => m.IsMemberAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NullAssignee_LeavesAssigneeUnchanged()
        {
            var item = MakeItem();
            item.AssigneeId = "member-1";
            var repo = MakeRepoReturning(item);
            repo.Setup(r => r.IsColumnOnBoardAsync(10, 5)).ReturnsAsync(true);
            var memberRepo = MakeMemberRepo();
            var handler = MakeHandler(repo, new Mock<ITaskLifecycleService>(), memberRepo: memberRepo);

            // e.g. SwitchColumn, which sends only { id, boardColumnId }.
            await handler.Handle(new UpdateBoardItemCommand { Id = 1, BoardColumnId = 5, AssigneeId = null }, CancellationToken.None);

            item.AssigneeId.ShouldBe("member-1");
            memberRepo.Verify(m => m.IsMemberAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_EmptyAssignee_ClearsAssigneeWithoutMembershipCheck(string cleared)
        {
            var item = MakeItem();
            item.AssigneeId = "member-1";
            var repo = MakeRepoReturning(item);
            var memberRepo = MakeMemberRepo();
            var handler = MakeHandler(repo, new Mock<ITaskLifecycleService>(), memberRepo: memberRepo);

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, AssigneeId = cleared }, CancellationToken.None);

            item.AssigneeId.ShouldBeNull();
            memberRepo.Verify(m => m.IsMemberAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

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

            var handler = MakeHandler(repo, lifecycleService, memberRepo: MakeMemberRepo("user-new"));

            await handler.Handle(new UpdateBoardItemCommand { Id = 1, AssigneeId = "user-new" }, CancellationToken.None);

            lifecycleService.Verify(l => l.RecordAssigneeChangeAsync(1), Times.Once);
        }
    }
}
