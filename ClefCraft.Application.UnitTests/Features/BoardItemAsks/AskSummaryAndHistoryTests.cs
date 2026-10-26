using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class AskSummaryAndHistoryTests
    {
        private static User MakeUser(string id) => new() { Id = id, Firstname = id, Lastname = "Tester", Email = $"{id}@test" };

        [Fact]
        public async Task BoardItems_CarryAnAskSummary_FromOneAskQueryAndTheExistingSingleUserLookup()
        {
            var first = new Domain.BoardItem { Id = 1, BoardId = 10, Title = "One", CreatedBy = "creator" };
            var second = new Domain.BoardItem { Id = 2, BoardId = 10, Title = "Two" };
            var column = new Domain.BoardColumn { Id = 1, Title = "To Do", BoardItems = new List<Domain.BoardItem> { first, second } };

            var itemRepo = new Mock<IBoardItemRepository>();
            itemRepo.Setup(r => r.GetBoardColumnsWithBoardItems(10)).ReturnsAsync(new List<Domain.BoardColumn> { column });

            var asks = new List<BoardItemAsk>
            {
                new() { Id = 1, BoardItemId = 1, RecipientUserId = "pending-recipient", RequestedByUserId = "r", Note = "a", State = BoardItemAskState.Pending },
                new() { Id = 2, BoardItemId = 1, RecipientUserId = "accepted-recipient", RequestedByUserId = "r", Note = "b", State = BoardItemAskState.Accepted, IsRequired = true },
                new() { Id = 3, BoardItemId = 1, RecipientUserId = "declined-recipient", RequestedByUserId = "r", Note = "c", State = BoardItemAskState.Declined, IsRequired = true },
                new() { Id = 4, BoardItemId = 1, RecipientUserId = "done-recipient", RequestedByUserId = "r", Note = "d", State = BoardItemAskState.Completed, IsRequired = true }
            };
            var askRepo = new Mock<IBoardItemAskRepository>();
            askRepo.Setup(r => r.GetByItemIdsReadOnlyAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> ids) => asks.Where(a => ids.Contains(a.BoardItemId)).ToList());

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("caller");
            userService.Setup(u => u.GetUsersByIds(It.IsAny<List<string>>()))
                .ReturnsAsync((List<string> ids) => ids.Select(MakeUser).ToList());

            var handler = new GetBoardItemsHandler(itemRepo.Object, MockAccessServices.GetMockBoardAccessService().Object, userService.Object, askRepo.Object);

            var result = await handler.Handle(new GetBoardItemsQuery(10), CancellationToken.None);

            var summary = result.Single().BoardItems.Single(i => i.Id == 1).AskSummary;
            summary.OpenCount.ShouldBe(2);
            summary.UnmetRequiredCount.ShouldBe(2);
            summary.CompletedCount.ShouldBe(1);
            summary.OpenAsks.Select(a => a.RecipientFullName).ShouldBe(new[] { "pending-recipient Tester", "accepted-recipient Tester" });

            result.Single().BoardItems.Single(i => i.Id == 2).AskSummary.OpenCount.ShouldBe(0);

            askRepo.Verify(r => r.GetByItemIdsReadOnlyAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
            userService.Verify(u => u.GetUsersByIds(It.Is<List<string>>(ids =>
                ids.Contains("creator") && ids.Contains("pending-recipient") && ids.Contains("accepted-recipient"))), Times.Once);
        }

        [Theory]
        [InlineData("pending-recipient", 1)]
        [InlineData("accepted-recipient", 1)]
        // The requester: the declined required ask is theirs to resolve, the completed one isn't.
        [InlineData("r", 1)]
        [InlineData("someone-else", 0)]
        public async Task BoardItems_SayHowManyAsksAreTheCallersMove_FromTheSameAskQuery(string callerId, int onFirst)
        {
            var first = new Domain.BoardItem { Id = 1, BoardId = 10, Title = "One" };
            var doneItem = new Domain.BoardItem
            {
                Id = 2, BoardId = 10, Title = "Two",
                BoardItemStatus = new BoardItemStatus { StatusId = 5, Status = new Status { Id = 5, Name = "Done" } }
            };
            var column = new Domain.BoardColumn { Id = 1, Title = "To Do", BoardItems = new List<Domain.BoardItem> { first, doneItem } };

            var itemRepo = new Mock<IBoardItemRepository>();
            itemRepo.Setup(r => r.GetBoardColumnsWithBoardItems(10)).ReturnsAsync(new List<Domain.BoardColumn> { column });

            var asks = new List<BoardItemAsk>
            {
                new() { Id = 1, BoardItemId = 1, RecipientUserId = "pending-recipient", RequestedByUserId = "r", Note = "a", State = BoardItemAskState.Pending },
                new() { Id = 2, BoardItemId = 1, RecipientUserId = "accepted-recipient", RequestedByUserId = "r", Note = "b", State = BoardItemAskState.Accepted },
                new() { Id = 3, BoardItemId = 1, RecipientUserId = "declined-recipient", RequestedByUserId = "r", Note = "c", State = BoardItemAskState.Declined, IsRequired = true },
                new() { Id = 4, BoardItemId = 1, RecipientUserId = "done-recipient", RequestedByUserId = "r", Note = "d", State = BoardItemAskState.Completed, IsRequired = true },
                // On a Done item nothing is anyone's move, required or not.
                new() { Id = 5, BoardItemId = 2, RecipientUserId = "declined-recipient", RequestedByUserId = "r", Note = "e", State = BoardItemAskState.Declined, IsRequired = true }
            };
            var askRepo = new Mock<IBoardItemAskRepository>();
            askRepo.Setup(r => r.GetByItemIdsReadOnlyAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((IEnumerable<int> ids) => asks.Where(a => ids.Contains(a.BoardItemId)).ToList());

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(callerId);
            userService.Setup(u => u.GetUsersByIds(It.IsAny<List<string>>()))
                .ReturnsAsync((List<string> ids) => ids.Select(MakeUser).ToList());

            var handler = new GetBoardItemsHandler(itemRepo.Object, MockAccessServices.GetMockBoardAccessService().Object, userService.Object, askRepo.Object);

            var items = (await handler.Handle(new GetBoardItemsQuery(10), CancellationToken.None)).Single().BoardItems;

            items.Single(i => i.Id == 1).AskAttention.YourMove.ShouldBe(onFirst);
            items.Single(i => i.Id == 2).AskAttention.YourMove.ShouldBe(0);
            askRepo.Verify(r => r.GetByItemIdsReadOnlyAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
        }

        [Fact]
        public async Task History_ResolvesTheNamesInAskEntries()
        {
            var logs = new List<ActivityLog>
            {
                new()
                {
                    Id = 1, EntityType = "BoardItem", EntityId = 88, UserId = "closer",
                    ActionType = AskHistory.Completed, Timestamp = DateTime.UtcNow,
                    MetadataJson = "{\"AskId\":5,\"Note\":\"Review\",\"RecipientUserId\":\"recipient\",\"CompletedByUserId\":\"closer\"}"
                },
                new()
                {
                    Id = 2, EntityType = "BoardItem", EntityId = 88, UserId = "closer",
                    ActionType = AskHistory.RequirementMoved, Timestamp = DateTime.UtcNow.AddMinutes(-1),
                    MetadataJson = "{\"AskId\":4,\"Note\":\"Review\",\"RecipientUserId\":\"gone\",\"ToRecipientUserId\":\"recipient\"}"
                }
            };

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("caller");
            userService.Setup(u => u.GetUsersByIds(It.IsAny<List<string>>()))
                .ReturnsAsync((List<string> ids) => ids.Where(id => id != "gone").Select(MakeUser).ToList());

            var handler = new GetActivityLogForEntityHandler(
                MockActivityLogRepository.GetMockActivityLogRepository(logs).Object,
                MockAccessServices.GetMockBoardAccessService().Object,
                userService.Object);

            var result = await handler.Handle(new GetActivityLogForEntityQuery { EntityType = "BoardItem", EntityId = 88 }, CancellationToken.None);

            var completed = result.Items.Single(i => i.ActionType == AskHistory.Completed).Changes;
            completed.Single(c => c.FieldName == AskHistory.RecipientUserIdKey).NewDisplayValue.ShouldBe("recipient Tester");
            completed.Single(c => c.FieldName == AskHistory.CompletedByUserIdKey).NewDisplayValue.ShouldBe("closer Tester");
            completed.Single(c => c.FieldName == AskHistory.NoteKey).NewDisplayValue.ShouldBeNull();

            var moved = result.Items.Single(i => i.ActionType == AskHistory.RequirementMoved).Changes;
            moved.Single(c => c.FieldName == AskHistory.RecipientUserIdKey).NewDisplayValue.ShouldBe("Unknown user");
            moved.Single(c => c.FieldName == AskHistory.ToRecipientUserIdKey).NewDisplayValue.ShouldBe("recipient Tester");
        }
    }
}
