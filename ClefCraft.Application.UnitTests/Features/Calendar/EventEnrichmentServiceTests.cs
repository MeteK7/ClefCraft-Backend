using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using ClefCraft.Infrastructure.Services.Calendar;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar
{
    // Regression coverage for the P0 follow-up: LinkedBoardItemId can point at a board the
    // caller isn't a member of (a pre-existing row, or a gap in an upstream check), so
    // enrichment must never attach a title for an item outside the caller's membership.
    public class EventEnrichmentServiceTests
    {
        private static Domain.BoardItem MakeItem(int id, int boardId, string title) =>
            new Domain.BoardItem { Id = id, BoardId = boardId, Title = title };

        [Fact]
        public async Task EnrichAsync_LinkedItemOnMemberBoard_AttachesTitle()
        {
            var boardRepo = new Mock<IBoardItemRepository>();
            boardRepo.Setup(r => r.GetByIdsAsync(It.Is<List<int>>(ids => ids.Contains(5))))
                .ReturnsAsync(new List<Domain.BoardItem> { MakeItem(5, boardId: 10, title: "Practice scales") });

            var boardMemberRepo = new Mock<IBoardMemberRepository>();
            boardMemberRepo.Setup(r => r.GetMemberBoardIdsAsync("user-1")).ReturnsAsync(new List<int> { 10 });

            var service = new EventEnrichmentService(boardRepo.Object, boardMemberRepo.Object);
            var events = new List<CalendarEventDto> { new CalendarEventDto { LinkedBoardItemId = 5 } };

            await service.EnrichAsync(events, "user-1");

            events[0].LinkedBoardItemTitle.ShouldBe("Practice scales");
        }

        [Fact]
        public async Task EnrichAsync_LinkedItemOnBoardCallerIsNotAMemberOf_LeavesTitleNull()
        {
            var boardRepo = new Mock<IBoardItemRepository>();
            boardRepo.Setup(r => r.GetByIdsAsync(It.Is<List<int>>(ids => ids.Contains(5))))
                .ReturnsAsync(new List<Domain.BoardItem> { MakeItem(5, boardId: 999, title: "Someone else's item") });

            var boardMemberRepo = new Mock<IBoardMemberRepository>();
            boardMemberRepo.Setup(r => r.GetMemberBoardIdsAsync("user-1")).ReturnsAsync(new List<int> { 10 });

            var service = new EventEnrichmentService(boardRepo.Object, boardMemberRepo.Object);
            var events = new List<CalendarEventDto> { new CalendarEventDto { LinkedBoardItemId = 5 } };

            await service.EnrichAsync(events, "user-1");

            events[0].LinkedBoardItemTitle.ShouldBeNull();
        }

        [Fact]
        public async Task EnrichAsync_NoLinkedItems_NeverQueriesRepositories()
        {
            var boardRepo = new Mock<IBoardItemRepository>();
            var boardMemberRepo = new Mock<IBoardMemberRepository>();

            var service = new EventEnrichmentService(boardRepo.Object, boardMemberRepo.Object);
            var events = new List<CalendarEventDto> { new CalendarEventDto { LinkedBoardItemId = null } };

            await service.EnrichAsync(events, "user-1");

            boardRepo.Verify(r => r.GetByIdsAsync(It.IsAny<List<int>>()), Times.Never);
            boardMemberRepo.Verify(r => r.GetMemberBoardIdsAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
