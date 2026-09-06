using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using ClefCraft.Infrastructure.Services.Authorization;
using Moq;
using Shouldly;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Comments
{
    // EnsureCanCommentOnEventAsync is new, real branching logic (unlike the mocked-through
    // interface used everywhere else), so it's tested directly against the concrete service
    // rather than only through a handler-level mock.
    public class CalendarAccessServiceCommentTests
    {
        private const string OwnerUserId = "owner-1";

        private static CalendarAccessService BuildService(
            CalendarEvent calendarEvent, bool sharesABoardWithOwner)
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.GetByIdReadOnlyAsync(calendarEvent.Id)).ReturnsAsync(calendarEvent);

            var attachmentRepo = new Mock<ICalendarEventAttachmentRepository>();

            var boardMemberRepo = new Mock<IBoardMemberRepository>();
            boardMemberRepo.Setup(r => r.ShareAnyBoardAsync(It.IsAny<string>(), OwnerUserId))
                .ReturnsAsync(sharesABoardWithOwner);

            return new CalendarAccessService(eventRepo.Object, attachmentRepo.Object, boardMemberRepo.Object);
        }

        [Fact]
        public async Task EnsureCanCommentOnEventAsync_Owner_Allowed()
        {
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId };
            var service = BuildService(calendarEvent, sharesABoardWithOwner: false);

            await Should.NotThrowAsync(() => service.EnsureCanCommentOnEventAsync(1, OwnerUserId));
        }

        [Fact]
        public async Task EnsureCanCommentOnEventAsync_TeammateSharingABoard_Allowed_RegardlessOfLinkedBoardItemId()
        {
            // Standalone event — no LinkedBoardItemId at all — proving the check never
            // depends on it: only board co-membership with the owner matters.
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId, LinkedBoardItemId = null };
            var service = BuildService(calendarEvent, sharesABoardWithOwner: true);

            await Should.NotThrowAsync(() => service.EnsureCanCommentOnEventAsync(1, "teammate-1"));
        }

        [Fact]
        public async Task EnsureCanCommentOnEventAsync_UnrelatedUser_ThrowsForbiddenAccessException()
        {
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId };
            var service = BuildService(calendarEvent, sharesABoardWithOwner: false);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                service.EnsureCanCommentOnEventAsync(1, "stranger"));
        }

        [Fact]
        public async Task EnsureCanCommentOnEventAsync_EventNotFound_ThrowsNotFoundException()
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<int>())).ReturnsAsync((CalendarEvent?)null);

            var service = new CalendarAccessService(
                eventRepo.Object, new Mock<ICalendarEventAttachmentRepository>().Object, new Mock<IBoardMemberRepository>().Object);

            await Should.ThrowAsync<NotFoundException>(() => service.EnsureCanCommentOnEventAsync(999, "anyone"));
        }
    }
}
