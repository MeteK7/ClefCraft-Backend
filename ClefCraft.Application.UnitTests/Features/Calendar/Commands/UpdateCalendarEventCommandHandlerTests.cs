using AutoMapper;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Commands
{
    // Covers state-transition matrix rows C, D and I from the recurrence
    // audit: non-recurring -> non-recurring, non-recurring -> recurring
    // (previously silently lost the RecurrenceSeries/CalendarEventSegment),
    // and recurring -> non-recurring (previously left orphaned series data).
    public class UpdateCalendarEventCommandHandlerTests
    {
        private static CalendarEvent MakeEntity(bool isRecurring, string seriesUid = "series-1")
        {
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            return new CalendarEvent
            {
                Id = 42,
                Subject = "Original subject",
                UserId = "user-1",
                SeriesUid = seriesUid,
                StartDate = start,
                EndDate = start.AddHours(1),
                IsRecurring = isRecurring,
                RecurrenceRuleJson = isRecurring
                    ? "{\"Frequency\":\"WEEKLY\",\"Interval\":1}"
                    : null
            };
        }

        private static UpdateCalendarEventCommand MakeRequest(
            bool isRecurring,
            string? recurrenceRuleJson = null)
        {
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            return new UpdateCalendarEventCommand
            {
                Id = 42,
                Subject = "Updated subject",
                StartDate = start,
                EndDate = start.AddHours(1),
                AllDayEvent = false,
                Importance = ImportanceLevel.Normal,
                IsRecurring = isRecurring,
                RecurrenceRuleJson = recurrenceRuleJson
            };
        }

        private static (
            UpdateCalendarEventCommandHandler handler,
            Mock<ICalendarEventRepository> eventRepo,
            Mock<IRecurrenceSeriesRepository> seriesRepo,
            Mock<ICalendarEventSegmentRepository> segmentRepo,
            Mock<ICalendarEventExceptionRepository> exceptionRepo
        ) MakeHandler(CalendarEvent entity, RecurrenceSeries? existingSeries)
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.GetByIdAsync(entity.Id)).ReturnsAsync(entity);

            var reminderRepo = new Mock<ICalendarReminderRepository>();
            reminderRepo.Setup(r => r.GetByEventIdAsync(entity.Id)).ReturnsAsync(new List<CalendarReminder>());

            var seriesRepo = new Mock<IRecurrenceSeriesRepository>();
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(entity.SeriesUid)).ReturnsAsync(existingSeries);

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("user-1");

            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<CalendarEventDto>(It.IsAny<CalendarEvent>())).Returns(new CalendarEventDto());

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var reminderScheduler = new Mock<IReminderSchedulerService>();

            var handler = new UpdateCalendarEventCommandHandler(
                eventRepo.Object,
                reminderRepo.Object,
                seriesRepo.Object,
                segmentRepo.Object,
                exceptionRepo.Object,
                userService.Object,
                mapper.Object,
                unitOfWork.Object,
                reminderScheduler.Object);

            return (handler, eventRepo, seriesRepo, segmentRepo, exceptionRepo);
        }

        [Fact]
        public async Task Handle_NonRecurringToNonRecurring_NeverTouchesSeriesRepositories()
        {
            // Matrix row C
            var entity = MakeEntity(isRecurring: false);
            var (handler, _, seriesRepo, segmentRepo, exceptionRepo) = MakeHandler(entity, existingSeries: null);

            await handler.Handle(MakeRequest(isRecurring: false), CancellationToken.None);

            seriesRepo.Verify(r => r.GetBySeriesUidAsync(It.IsAny<string>()), Times.Never);
            segmentRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            exceptionRepo.Verify(r => r.DeleteAllForSeriesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NonRecurringToRecurring_CreatesSeriesAndInitialSegment()
        {
            // Matrix row D — the root cause of the reported "add recurrence
            // to an existing event doesn't save correctly" bug.
            var entity = MakeEntity(isRecurring: false);
            var (handler, _, seriesRepo, segmentRepo, _) = MakeHandler(entity, existingSeries: null);

            var ruleJson = "{\"Frequency\":\"WEEKLY\",\"Interval\":1,\"DaysOfWeek\":[1,3]}";
            await handler.Handle(MakeRequest(isRecurring: true, ruleJson), CancellationToken.None);

            seriesRepo.Verify(r => r.CreateAsync(It.Is<RecurrenceSeries>(s =>
                s.SeriesUid == entity.SeriesUid && s.UserId == "user-1")), Times.Once);

            segmentRepo.Verify(r => r.CreateAsync(It.Is<CalendarEventSegment>(s =>
                s.RecurrenceRuleJson == ruleJson &&
                s.IsRecurring == true &&
                s.EffectiveTo == null)), Times.Once);

            entity.IsRecurring.ShouldBeTrue();
            entity.RecurrenceRuleJson.ShouldBe(ruleJson);
        }

        [Fact]
        public async Task Handle_RecurringToNonRecurring_DeletesSeriesAndExceptions()
        {
            // Matrix row I
            var entity = MakeEntity(isRecurring: true);
            var existingSeries = new RecurrenceSeries
            {
                Id = 7,
                UserId = "user-1",
                SeriesUid = entity.SeriesUid,
                Segments = new List<CalendarEventSegment>()
            };

            var (handler, _, seriesRepo, _, exceptionRepo) = MakeHandler(entity, existingSeries);

            await handler.Handle(MakeRequest(isRecurring: false), CancellationToken.None);

            seriesRepo.Verify(r => r.DeleteAsync(existingSeries), Times.Once);
            exceptionRepo.Verify(r => r.DeleteAllForSeriesAsync(entity.SeriesUid), Times.Once);

            entity.IsRecurring.ShouldBeFalse();
            entity.RecurrenceRuleJson.ShouldBeNull();
        }

        [Fact]
        public async Task Handle_RecurringToRecurring_ExistingSeriesFound_UpdatesAllSegmentsWithNewRule()
        {
            // Defensive path: this endpoint has no scope, so if it is ever
            // reached for an event that already has a series, the new rule
            // must actually reach the segments the projection reads from —
            // this guards the same class of "rule silently not applied" bug
            // found on the frontend's edit-mode deserialization gap.
            var entity = MakeEntity(isRecurring: true);
            var segment = new CalendarEventSegment
            {
                Id = 1,
                RecurrenceSeriesId = 7,
                RecurrenceRuleJson = "{\"Frequency\":\"WEEKLY\",\"Interval\":1}",
                Subject = "Old subject"
            };
            var existingSeries = new RecurrenceSeries
            {
                Id = 7,
                UserId = "user-1",
                SeriesUid = entity.SeriesUid,
                Segments = new List<CalendarEventSegment> { segment }
            };

            var (handler, _, _, _, _) = MakeHandler(entity, existingSeries);

            var newRuleJson = "{\"Frequency\":\"MONTHLY\",\"Interval\":2}";
            await handler.Handle(MakeRequest(isRecurring: true, newRuleJson), CancellationToken.None);

            segment.RecurrenceRuleJson.ShouldBe(newRuleJson);
            segment.Subject.ShouldBe("Updated subject");
        }

        [Fact]
        public async Task Handle_RecurringRuleWithInvalidInterval_ThrowsBeforeAnyRepositoryWrite()
        {
            var entity = MakeEntity(isRecurring: false);
            var (handler, eventRepo, seriesRepo, _, _) = MakeHandler(entity, existingSeries: null);

            var invalidRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":0}";

            await Should.ThrowAsync<ValidationException>(() =>
                handler.Handle(MakeRequest(isRecurring: true, invalidRuleJson), CancellationToken.None));

            eventRepo.Verify(r => r.UpdateAsync(It.IsAny<CalendarEvent>()), Times.Never);
            seriesRepo.Verify(r => r.CreateAsync(It.IsAny<RecurrenceSeries>()), Times.Never);
        }
    }
}
