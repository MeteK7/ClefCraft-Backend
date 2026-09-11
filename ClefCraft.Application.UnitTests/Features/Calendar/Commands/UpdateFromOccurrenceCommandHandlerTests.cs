using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.UpdateFromOccurrence;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Commands
{
    // "This and following" edit scope. Regression coverage for commit 8b5e132
    // ("bug fix: recurrence this and following"), which fixed a duplicate/dropped
    // split-boundary occurrence and an unguarded NullReferenceException on a missing
    // active segment, and for the idempotency guard that prevents re-splitting at the
    // same occurrence date from stacking duplicate segments.
    public class UpdateFromOccurrenceCommandHandlerTests
    {
        private const string CallerUserId = "user-1";
        private const string SeriesUid = "series-1";

        private static (
            UpdateFromOccurrenceCommandHandler Handler,
            Mock<IRecurrenceSeriesRepository> SeriesRepo,
            Mock<ICalendarEventSegmentRepository> SegmentRepo,
            Mock<ICalendarEventExceptionRepository> ExceptionRepo
        ) MakeHandler(RecurrenceSeries series, CalendarEventSegment? activeSegment)
        {
            var seriesRepo = new Mock<IRecurrenceSeriesRepository>();
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            segmentRepo.Setup(r => r.GetActiveSegmentAsync(series.Id, It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(activeSegment);

            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();

            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateFromOccurrenceCommandHandler(
                seriesRepo.Object, segmentRepo.Object, exceptionRepo.Object,
                accessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            return (handler, seriesRepo, segmentRepo, exceptionRepo);
        }

        [Fact]
        public async Task Handle_SplitsActiveSegment_CapsOldSegmentEffectiveTo_AndCreatesNewOpenEndedSegment()
        {
            var seriesStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var splitDate = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

            var activeSegment = new CalendarEventSegment
            {
                Id = 1, RecurrenceSeriesId = 5, EffectiveFrom = seriesStart, EffectiveTo = null,
                Subject = "Old subject", StartDate = seriesStart, EndDate = seriesStart.AddHours(1),
                IsRecurring = true, RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { activeSegment } };

            var (handler, _, segmentRepo, exceptionRepo) = MakeHandler(series, activeSegment);

            await handler.Handle(new UpdateFromOccurrenceCommand
            {
                SeriesUid = SeriesUid,
                OccurrenceDate = splitDate,
                Subject = "New subject"
            }, CancellationToken.None);

            segmentRepo.Verify(r => r.UpdateAsync(It.Is<CalendarEventSegment>(s => s == activeSegment && s.EffectiveTo == splitDate)), Times.Once);
            segmentRepo.Verify(r => r.CreateAsync(It.Is<CalendarEventSegment>(s =>
                s.EffectiveFrom == splitDate && s.EffectiveTo == null && s.Subject == "New subject")), Times.Once);
        }

        [Fact]
        public async Task Handle_ResplittingAtSameOccurrenceDate_UpdatesExistingSegmentInPlace_DoesNotCreateDuplicate()
        {
            var seriesStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var splitDate = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

            var oldSegment = new CalendarEventSegment
            {
                Id = 1, RecurrenceSeriesId = 5, EffectiveFrom = seriesStart, EffectiveTo = splitDate,
                Subject = "Segment 1", StartDate = seriesStart, EndDate = seriesStart.AddHours(1),
                IsRecurring = true, RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            // A prior "this and following" split already created a segment starting exactly at splitDate.
            var existingNewSegment = new CalendarEventSegment
            {
                Id = 2, RecurrenceSeriesId = 5, EffectiveFrom = splitDate, EffectiveTo = null,
                Subject = "Segment 2 (first edit)", StartDate = splitDate, EndDate = splitDate.AddHours(1),
                IsRecurring = true, RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { oldSegment, existingNewSegment } };

            var (handler, _, segmentRepo, _) = MakeHandler(series, activeSegment: existingNewSegment);

            await handler.Handle(new UpdateFromOccurrenceCommand
            {
                SeriesUid = SeriesUid,
                OccurrenceDate = splitDate,
                Subject = "Segment 2 (re-edited)"
            }, CancellationToken.None);

            // Core idempotency guarantee: re-splitting at the same date updates the
            // existing segment's fields in place rather than stacking a duplicate.
            segmentRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            existingNewSegment.Subject.ShouldBe("Segment 2 (re-edited)");

            // KNOWN DEFECT (found while writing this test, not previously tracked): the
            // unconditional capping step at the top of Handle() runs before the idempotency
            // check and mutates whatever GetActiveSegmentAsync returned — which, on a re-split
            // at an already-existing boundary, is this same segment. That leaves EffectiveTo
            // capped to its own EffectiveFrom (a zero-width window), which silently makes
            // RecurringEventProjectionService project zero future occurrences for it — the
            // "future" tail of the series effectively disappears after a second edit to the
            // same split point. This assertion documents current (buggy) behavior rather than
            // intended behavior; flagged for a fix decision, not fixed here.
            existingNewSegment.EffectiveTo.ShouldBe(splitDate);
        }

        [Fact]
        public async Task Handle_DeletesExceptionsFromOccurrenceDateForward_InclusiveOfTheSplitDateItself()
        {
            var seriesStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var splitDate = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

            var activeSegment = new CalendarEventSegment
            {
                Id = 1, RecurrenceSeriesId = 5, EffectiveFrom = seriesStart, EffectiveTo = null,
                Subject = "Subject", StartDate = seriesStart, EndDate = seriesStart.AddHours(1),
                IsRecurring = true, RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { activeSegment } };

            var (handler, _, _, exceptionRepo) = MakeHandler(series, activeSegment);

            await handler.Handle(new UpdateFromOccurrenceCommand
            {
                SeriesUid = SeriesUid,
                OccurrenceDate = splitDate
            }, CancellationToken.None);

            exceptionRepo.Verify(r => r.DeleteFromDateAsync(SeriesUid, splitDate), Times.Once);
        }

        [Fact]
        public async Task Handle_NoActiveSegmentForOccurrenceDate_ThrowsNotFoundException_BeforeAnyWrite()
        {
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment>() };
            var (handler, _, segmentRepo, exceptionRepo) = MakeHandler(series, activeSegment: null);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new UpdateFromOccurrenceCommand { SeriesUid = SeriesUid, OccurrenceDate = DateTimeOffset.UtcNow }, CancellationToken.None));

            segmentRepo.Verify(r => r.UpdateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            segmentRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            exceptionRepo.Verify(r => r.DeleteFromDateAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SeriesNotFound_ThrowsNotFoundException()
        {
            var seriesRepo = new Mock<IRecurrenceSeriesRepository>();
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync((RecurrenceSeries?)null);

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();
            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateFromOccurrenceCommandHandler(
                seriesRepo.Object, segmentRepo.Object, exceptionRepo.Object,
                accessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new UpdateFromOccurrenceCommand { SeriesUid = SeriesUid, OccurrenceDate = DateTimeOffset.UtcNow }, CancellationToken.None));
        }
    }
}
