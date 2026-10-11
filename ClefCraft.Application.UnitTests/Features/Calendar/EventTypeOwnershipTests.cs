using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.UpdateFromOccurrence;
using ClefCraft.Application.Features.Calendar.Commands.UpdateSingleOccurrence;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using ClefCraft.Infrastructure.Services.Authorization;
using Moq;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ClefCraft.Application.UnitTests.Features.Calendar
{
    // An event type may only be attached to an event by the user who owns it.
    public class EventTypeOwnershipTests
    {
        private const string CallerUserId = "user-1";
        private const int ForeignEventTypeId = 7;

        // ── CalendarAccessService.EnsureEventTypeOwnedByUserAsync ──────────────

        private static CalendarAccessService ServiceWith(EventType? eventType)
        {
            var eventTypeRepo = new Mock<IEventTypeRepository>();
            eventTypeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(eventType);

            return new CalendarAccessService(
                new Mock<ICalendarEventRepository>().Object,
                new Mock<ICalendarEventAttachmentRepository>().Object,
                new Mock<ICalendarEventCollaboratorRepository>().Object,
                eventTypeRepo.Object);
        }

        [Fact]
        public async Task NoEventType_IsAllowed()
        {
            await ServiceWith(null).EnsureEventTypeOwnedByUserAsync(null, CallerUserId);
        }

        [Fact]
        public async Task OwnEventType_IsAllowed()
        {
            await ServiceWith(new EventType { Id = 3, UserId = CallerUserId }).EnsureEventTypeOwnedByUserAsync(3, CallerUserId);
        }

        [Fact]
        public async Task AnotherUsersEventType_ThrowsForbidden()
        {
            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                ServiceWith(new EventType { Id = ForeignEventTypeId, UserId = "someone-else" })
                    .EnsureEventTypeOwnedByUserAsync(ForeignEventTypeId, CallerUserId));
        }

        [Fact]
        public async Task UnknownEventType_ThrowsNotFound()
        {
            await Should.ThrowAsync<NotFoundException>(() =>
                ServiceWith(null).EnsureEventTypeOwnedByUserAsync(99, CallerUserId));
        }

        // ── The four commands that attach an event type ───────────────────────

        private static Mock<ICalendarAccessService> AccessRejectingForeignType()
        {
            var access = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            access.Setup(s => s.EnsureEventTypeOwnedByUserAsync(ForeignEventTypeId, CallerUserId))
                .ThrowsAsync(new ForbiddenAccessException());
            return access;
        }

        private static Mock<IUserService> Caller()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            return userService;
        }

        [Fact]
        public async Task CreateEvent_WithAnotherUsersEventType_IsRejectedBeforeAnyWrite()
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var handler = new CreateCalendarEventCommandHandler(
                eventRepo.Object,
                new Mock<IRecurrenceSeriesRepository>().Object,
                new Mock<ICalendarEventSegmentRepository>().Object,
                new Mock<ICalendarReminderRepository>().Object,
                new Mock<IReminderSchedulerService>().Object,
                MockAccessServices.GetMockBoardAccessService().Object,
                Caller().Object,
                unitOfWork.Object,
                AccessRejectingForeignType().Object);

            var start = DateTimeOffset.UtcNow;
            await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(
                new CreateCalendarEventCommand { Subject = "Gig", StartDate = start, EndDate = start.AddHours(1), EventTypeId = ForeignEventTypeId },
                CancellationToken.None));

            eventRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEvent>()), Times.Never);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateEvent_WithAnotherUsersEventType_IsRejectedBeforeAnyWrite()
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new CalendarEvent { Id = 1, UserId = CallerUserId });
            var unitOfWork = new Mock<IUnitOfWork>();
            var handler = new UpdateCalendarEventCommandHandler(
                eventRepo.Object,
                new Mock<ICalendarReminderRepository>().Object,
                new Mock<IRecurrenceSeriesRepository>().Object,
                new Mock<ICalendarEventSegmentRepository>().Object,
                new Mock<ICalendarEventExceptionRepository>().Object,
                Caller().Object,
                unitOfWork.Object,
                new Mock<IReminderSchedulerService>().Object,
                AccessRejectingForeignType().Object);

            var start = DateTimeOffset.UtcNow;
            await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(
                new UpdateCalendarEventCommand { Id = 1, Subject = "Gig", StartDate = start, EndDate = start.AddHours(1), EventTypeId = ForeignEventTypeId },
                CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSingleOccurrence_WithAnotherUsersEventType_IsRejectedBeforeAnyWrite()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            var handler = new UpdateSingleOccurrenceCommandHandler(
                new Mock<ICalendarEventExceptionRepository>().Object,
                AccessRejectingForeignType().Object,
                Caller().Object,
                unitOfWork.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(
                new UpdateSingleOccurrenceCommand { SeriesUid = "series-1", OccurrenceDate = DateTimeOffset.UtcNow, EventTypeId = ForeignEventTypeId },
                CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateFromOccurrence_WithAnotherUsersEventType_IsRejectedBeforeAnyWrite()
        {
            var unitOfWork = new Mock<IUnitOfWork>();
            var handler = new UpdateFromOccurrenceCommandHandler(
                new Mock<IRecurrenceSeriesRepository>().Object,
                new Mock<ICalendarEventSegmentRepository>().Object,
                new Mock<ICalendarEventExceptionRepository>().Object,
                AccessRejectingForeignType().Object,
                Caller().Object,
                unitOfWork.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(
                new UpdateFromOccurrenceCommand { SeriesUid = "series-1", OccurrenceDate = DateTimeOffset.UtcNow, EventTypeId = ForeignEventTypeId },
                CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
