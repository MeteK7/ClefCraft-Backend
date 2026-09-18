using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.DeleteFromOccurrence;
using ClefCraft.Application.Features.Calendar.Commands.DeleteSeries;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using MediatR;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Commands
{
    // "This and following" delete. The one genuine correctness risk here is the
    // zero-segment case: RecurringEventProjectionService.ProjectAsync falls back to legacy
    // expansion off the root event's own (stale) RecurrenceRuleJson whenever
    // series == null || !series.Segments.Any() — leaving a RecurrenceSeries with zero
    // segments after a delete would silently resurrect every future occurrence via that
    // fallback instead of deleting them. That case must degrade to a full series+event delete.
    public class DeleteFromOccurrenceCommandHandlerTests
    {
        private const string CallerUserId = "user-1";
        private const string SeriesUid = "series-1";

        private static (
            DeleteFromOccurrenceCommandHandler Handler,
            Mock<IRecurrenceSeriesRepository> SeriesRepo,
            Mock<ICalendarEventSegmentRepository> SegmentRepo,
            Mock<ICalendarEventExceptionRepository> ExceptionRepo,
            Mock<IMediator> Mediator
        ) MakeHandler(RecurrenceSeries series, CalendarEventSegment? activeSegment, bool authorized = true)
        {
            var seriesRepo = new Mock<IRecurrenceSeriesRepository>();
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            segmentRepo.Setup(r => r.GetActiveSegmentAsync(series.Id, It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(activeSegment);

            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();
            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            var mediator = new Mock<IMediator>();

            var handler = new DeleteFromOccurrenceCommandHandler(
                seriesRepo.Object, segmentRepo.Object, exceptionRepo.Object,
                accessService.Object, userService.Object, unitOfWork.Object, mediator.Object);

            return (handler, seriesRepo, segmentRepo, exceptionRepo, mediator);
        }

        [Fact]
        public async Task Handle_OccurrenceDateMidSegment_CapsEffectiveTo_CreatesNoContinuationSegment()
        {
            var seriesStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var occurrenceDate = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

            var activeSegment = new CalendarEventSegment
            {
                Id = 1, RecurrenceSeriesId = 5, EffectiveFrom = seriesStart, EffectiveTo = null,
                StartDate = seriesStart, EndDate = seriesStart.AddHours(1),
                IsRecurring = true, RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { activeSegment } };

            var (handler, _, segmentRepo, exceptionRepo, mediator) = MakeHandler(series, activeSegment);

            await handler.Handle(new DeleteFromOccurrenceCommand { SeriesUid = SeriesUid, OccurrenceDate = occurrenceDate }, CancellationToken.None);

            segmentRepo.Verify(r => r.UpdateAsync(It.Is<CalendarEventSegment>(s => s == activeSegment && s.EffectiveTo == occurrenceDate)), Times.Once);
            segmentRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            segmentRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            exceptionRepo.Verify(r => r.DeleteFromDateAsync(SeriesUid, occurrenceDate), Times.Once);
            mediator.Verify(m => m.Send(It.IsAny<DeleteSeriesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_OccurrenceDateAtSegmentBoundary_OtherSegmentsRemain_DeletesJustThatSegment()
        {
            var seriesStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var splitDate = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);

            var earlierSegment = new CalendarEventSegment
            {
                Id = 1, RecurrenceSeriesId = 5, EffectiveFrom = seriesStart, EffectiveTo = splitDate,
                StartDate = seriesStart, EndDate = seriesStart.AddHours(1), IsRecurring = true,
                RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            var activeSegment = new CalendarEventSegment
            {
                Id = 2, RecurrenceSeriesId = 5, EffectiveFrom = splitDate, EffectiveTo = null,
                StartDate = splitDate, EndDate = splitDate.AddHours(1), IsRecurring = true,
                RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { earlierSegment, activeSegment } };

            var (handler, _, segmentRepo, exceptionRepo, mediator) = MakeHandler(series, activeSegment);

            await handler.Handle(new DeleteFromOccurrenceCommand { SeriesUid = SeriesUid, OccurrenceDate = splitDate }, CancellationToken.None);

            segmentRepo.Verify(r => r.DeleteAsync(activeSegment), Times.Once);
            segmentRepo.Verify(r => r.UpdateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            exceptionRepo.Verify(r => r.DeleteFromDateAsync(SeriesUid, splitDate), Times.Once);
            mediator.Verify(m => m.Send(It.IsAny<DeleteSeriesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_OccurrenceDateAtOnlySegmentsStart_DegradesToFullSeriesAndEventDelete()
        {
            var seriesStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

            var onlySegment = new CalendarEventSegment
            {
                Id = 1, RecurrenceSeriesId = 5, EffectiveFrom = seriesStart, EffectiveTo = null,
                StartDate = seriesStart, EndDate = seriesStart.AddHours(1), IsRecurring = true,
                RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { onlySegment } };

            var (handler, _, segmentRepo, exceptionRepo, mediator) = MakeHandler(series, onlySegment);

            await handler.Handle(new DeleteFromOccurrenceCommand { SeriesUid = SeriesUid, OccurrenceDate = seriesStart }, CancellationToken.None);

            // Delegates to the full series+event delete rather than leaving a zero-segment series.
            mediator.Verify(m => m.Send(
                It.Is<DeleteSeriesCommand>(c => c.SeriesUid == SeriesUid),
                It.IsAny<CancellationToken>()), Times.Once);

            segmentRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            segmentRepo.Verify(r => r.UpdateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            exceptionRepo.Verify(r => r.DeleteFromDateAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NoActiveSegmentForOccurrenceDate_ThrowsNotFoundException()
        {
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment>() };
            var (handler, _, segmentRepo, exceptionRepo, mediator) = MakeHandler(series, activeSegment: null);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new DeleteFromOccurrenceCommand { SeriesUid = SeriesUid, OccurrenceDate = DateTimeOffset.UtcNow }, CancellationToken.None));

            segmentRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            segmentRepo.Verify(r => r.UpdateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            mediator.Verify(m => m.Send(It.IsAny<DeleteSeriesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
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
            var unitOfWork = new Mock<IUnitOfWork>();
            var mediator = new Mock<IMediator>();

            var handler = new DeleteFromOccurrenceCommandHandler(
                seriesRepo.Object, segmentRepo.Object, exceptionRepo.Object,
                accessService.Object, userService.Object, unitOfWork.Object, mediator.Object);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new DeleteFromOccurrenceCommand { SeriesUid = SeriesUid, OccurrenceDate = DateTimeOffset.UtcNow }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_SeriesOwnedBySomeoneElse_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var seriesStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var activeSegment = new CalendarEventSegment
            {
                Id = 1, RecurrenceSeriesId = 5, EffectiveFrom = seriesStart, EffectiveTo = null,
                StartDate = seriesStart, EndDate = seriesStart.AddHours(1), IsRecurring = true,
                RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { activeSegment } };

            var (handler, _, segmentRepo, _, mediator) = MakeHandler(series, activeSegment, authorized: false);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new DeleteFromOccurrenceCommand { SeriesUid = SeriesUid, OccurrenceDate = seriesStart }, CancellationToken.None));

            segmentRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            segmentRepo.Verify(r => r.UpdateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
            mediator.Verify(m => m.Send(It.IsAny<DeleteSeriesCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
