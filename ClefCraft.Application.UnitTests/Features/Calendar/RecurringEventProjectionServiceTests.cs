using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using ClefCraft.Infrastructure.Services.Calendar;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar
{
    // RecurringEventProjectionService is the live recurrence-expansion path behind
    // GetCalendarEventsQueryHandler (EventExpansionService is dead code — unregistered,
    // uncalled). It had zero test coverage prior to this file despite being the most
    // historically bug-prone area of the backend (duplicate/dropped "this and following"
    // boundary occurrences, segment vs. legacy fallback selection, etc.). These tests
    // characterize its current, intended behavior.
    public class RecurringEventProjectionServiceTests
    {
        private const string SeriesUid = "series-1";

        private static (RecurringEventProjectionService Service, Mock<ICalendarEventExceptionRepository> ExceptionRepo, Mock<IRecurrenceSeriesRepository> SeriesRepo) MakeService()
        {
            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();
            var seriesRepo = new Mock<IRecurrenceSeriesRepository>();
            var service = new RecurringEventProjectionService(exceptionRepo.Object, seriesRepo.Object);
            return (service, exceptionRepo, seriesRepo);
        }

        private static CalendarEventSegment MakeSegment(
            DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo,
            DateTimeOffset startDate, DateTimeOffset endDate,
            string ruleJson, string subject = "Segment subject") =>
            new CalendarEventSegment
            {
                RecurrenceSeriesId = 5,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = effectiveTo,
                Subject = subject,
                StartDate = startDate,
                EndDate = endDate,
                IsRecurring = true,
                RecurrenceRuleJson = ruleJson,
                Importance = ImportanceLevel.Normal
            };

        [Fact]
        public async Task ProjectAsync_NonRecurringEvent_PassesThroughUnexpanded_SetsBaseEventIdToOwnId()
        {
            var (service, _, seriesRepo) = MakeService();
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            var ev = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = false,
                Subject = "One-off", StartDate = start, EndDate = start.AddHours(1)
            };

            var result = await service.ProjectAsync(
                new List<CalendarEvent> { ev }, start.AddDays(-1), start.AddDays(1));

            result.Count.ShouldBe(1);
            result[0].BaseEventId.ShouldBe(1);
            result[0].OccurrenceDate.ShouldBe(start);
            seriesRepo.Verify(r => r.GetBySeriesUidAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ProjectAsync_RecurringEventWithNoSeriesRow_FallsBackToLegacyExpansion()
        {
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync((RecurrenceSeries?)null);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>());

            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true,
                Subject = "Daily standup", StartDate = start, EndDate = start.AddHours(1),
                RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, start, start.AddDays(3));

            result.Count.ShouldBe(3);
            result.ShouldAllBe(x => x.Subject == "Daily standup");
        }

        [Fact]
        public async Task ProjectAsync_RecurringEventWithSeriesButNoSegments_FallsBackToLegacyExpansion()
        {
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment>() };
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>());

            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true,
                Subject = "Daily standup", StartDate = start, EndDate = start.AddHours(1),
                RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            };

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, start, start.AddDays(3));

            result.Count.ShouldBe(3);
            result.ShouldAllBe(x => x.Subject == "Daily standup");
        }

        [Fact]
        public async Task ProjectAsync_SeriesWithSegments_UsesSegmentExpansion_NotLegacyRule()
        {
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            // Root event's own RecurrenceRuleJson (WEEKLY) is stale — the segment (DAILY)
            // must win. If the legacy rule were used instead, only 1 occurrence would
            // appear in the 3-day window instead of 3, with the stale subject.
            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true,
                Subject = "Stale root subject", StartDate = start, EndDate = start.AddHours(1),
                RecurrenceRuleJson = "{\"Frequency\":\"WEEKLY\",\"Interval\":1}"
            };

            var segment = MakeSegment(
                effectiveFrom: start, effectiveTo: null,
                startDate: start, endDate: start.AddHours(1),
                ruleJson: "{\"Frequency\":\"DAILY\",\"Interval\":1}",
                subject: "Fresh segment subject");

            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { segment } };
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>());

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, start, start.AddDays(3));

            result.Count.ShouldBe(3);
            result.ShouldAllBe(x => x.Subject == "Fresh segment subject");
        }

        [Fact]
        public async Task ProjectAsync_ThisAndFollowingSplit_ProducesBoundaryOccurrenceExactlyOnce()
        {
            // Regression for commit 8b5e132 ("bug fix: recurrence this and following"),
            // which could duplicate or drop the split-boundary occurrence.
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var seriesStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var splitDate = new DateTimeOffset(2026, 1, 4, 9, 0, 0, TimeSpan.Zero);
            var rangeEnd = seriesStart.AddDays(10);

            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true,
                StartDate = seriesStart, EndDate = seriesStart.AddHours(1)
            };

            var segmentA = MakeSegment(
                effectiveFrom: seriesStart, effectiveTo: splitDate,
                startDate: seriesStart, endDate: seriesStart.AddHours(1),
                ruleJson: "{\"Frequency\":\"DAILY\",\"Interval\":1}",
                subject: "Old subject");

            var segmentB = MakeSegment(
                effectiveFrom: splitDate, effectiveTo: null,
                startDate: splitDate, endDate: splitDate.AddHours(1),
                ruleJson: "{\"Frequency\":\"DAILY\",\"Interval\":1}",
                subject: "New subject");

            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { segmentA, segmentB } };
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>());

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, seriesStart, rangeEnd);

            result.Count(x => x.StartDate == splitDate).ShouldBe(1);
            result.Single(x => x.StartDate == splitDate).Subject.ShouldBe("New subject");
        }

        [Fact]
        public async Task ProjectAsync_SegmentEffectiveFromLaterThanQueryRangeStart_ExcludesOccurrencesBeforeSegmentStart()
        {
            // Protects the effectiveRangeStart clamp (segment.EffectiveFrom vs. rangeStart)
            // in ExpandSegmentSeriesAsync — a segment must never produce occurrences before
            // its own EffectiveFrom even when the caller's query window starts earlier.
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var queryStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var segmentStart = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);
            var queryEnd = new DateTimeOffset(2026, 1, 20, 9, 0, 0, TimeSpan.Zero);

            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true,
                StartDate = segmentStart, EndDate = segmentStart.AddHours(1)
            };

            var segment = MakeSegment(
                effectiveFrom: segmentStart, effectiveTo: null,
                startDate: segmentStart, endDate: segmentStart.AddHours(1),
                ruleJson: "{\"Frequency\":\"DAILY\",\"Interval\":1}");

            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { segment } };
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>());

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, queryStart, queryEnd);

            result.ShouldNotBeEmpty();
            result.ShouldAllBe(x => x.StartDate >= segmentStart);
            result.Min(x => x.StartDate).ShouldBe(segmentStart);
        }

        [Fact]
        public async Task ProjectAsync_OverlappingSegments_DedupKeepsEarlierProcessedSegmentsVersion()
        {
            // Documents current dedup-collision behavior (ExpandSegmentSeriesAsync groups
            // by exact StartDate and takes .First()): segments are processed in EffectiveFrom
            // ascending order, so on an exact-timestamp collision the earlier segment's
            // version wins, not the later (chronologically newer) one. This is a defensive
            // characterization — the command handlers today always cap the prior segment's
            // EffectiveTo to the new segment's EffectiveFrom (non-overlapping), so this
            // scenario shouldn't arise via normal edits, but the service's own dedup rule
            // should stay predictable if that ever changes.
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var rangeStart = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var rangeEnd = rangeStart.AddDays(10);

            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true,
                StartDate = rangeStart, EndDate = rangeStart.AddHours(1)
            };

            var segmentA = MakeSegment(
                effectiveFrom: rangeStart, effectiveTo: rangeStart.AddDays(9),
                startDate: rangeStart, endDate: rangeStart.AddHours(1),
                ruleJson: "{\"Frequency\":\"DAILY\",\"Interval\":1}",
                subject: "Old");

            var overlapStart = rangeStart.AddDays(4);
            var segmentB = MakeSegment(
                effectiveFrom: overlapStart, effectiveTo: null,
                startDate: overlapStart, endDate: overlapStart.AddHours(1),
                ruleJson: "{\"Frequency\":\"DAILY\",\"Interval\":1}",
                subject: "New");

            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { segmentA, segmentB } };
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>());

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, rangeStart, rangeEnd);

            result.Count(x => x.StartDate == overlapStart).ShouldBe(1);
            result.Single(x => x.StartDate == overlapStart).Subject.ShouldBe("Old");
        }

        [Fact]
        public async Task ProjectAsync_AllDayRecurringEvent_CarriesAllDayEventThroughEverySegmentAndOccurrence()
        {
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var seriesStart = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var splitDate = new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero);
            var rangeEnd = seriesStart.AddDays(10);

            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true, AllDayEvent = true,
                StartDate = seriesStart, EndDate = seriesStart.AddDays(1)
            };

            var segmentA = MakeSegment(seriesStart, splitDate, seriesStart, seriesStart.AddDays(1), "{\"Frequency\":\"DAILY\",\"Interval\":1}");
            var segmentB = MakeSegment(splitDate, null, splitDate, splitDate.AddDays(1), "{\"Frequency\":\"DAILY\",\"Interval\":1}");

            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { segmentA, segmentB } };
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>());

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, seriesStart, rangeEnd);

            result.ShouldNotBeEmpty();
            result.ShouldAllBe(x => x.AllDayEvent);
        }

        [Fact]
        public async Task ProjectAsync_OccurrenceKey_IsSeriesUidPlusUtcSecondPrecisionTimestamp()
        {
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var start = new DateTimeOffset(2026, 1, 5, 9, 30, 0, TimeSpan.Zero);

            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true,
                StartDate = start, EndDate = start.AddHours(1)
            };

            var segment = MakeSegment(start, null, start, start.AddHours(1), "{\"Frequency\":\"DAILY\",\"Interval\":1,\"Count\":1}");
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { segment } };
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>());

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, start, start.AddDays(1));

            result.Count.ShouldBe(1);
            var expectedKey = $"{SeriesUid}_{start.UtcDateTime:yyyyMMddHHmmss}";
            result[0].OccurrenceKey.ShouldBe(expectedKey);
        }

        [Fact]
        public async Task ProjectAsync_CancelledException_RemovesThatOccurrenceFromProjectedResults()
        {
            var (service, exceptionRepo, seriesRepo) = MakeService();
            var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var cancelledDate = start.AddDays(2);

            var rootEvent = new CalendarEvent
            {
                Id = 1, SeriesUid = SeriesUid, IsRecurring = true,
                StartDate = start, EndDate = start.AddHours(1)
            };

            var segment = MakeSegment(start, null, start, start.AddHours(1), "{\"Frequency\":\"DAILY\",\"Interval\":1}");
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, Segments = new List<CalendarEventSegment> { segment } };
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);
            exceptionRepo.Setup(r => r.GetBySeriesUid(SeriesUid)).ReturnsAsync(new List<CalendarEventException>
            {
                new CalendarEventException { SeriesUid = SeriesUid, OccurrenceDate = cancelledDate, IsCancelled = true }
            });

            var result = await service.ProjectAsync(new List<CalendarEvent> { rootEvent }, start, start.AddDays(5));

            result.Count.ShouldBe(4); // 5 daily occurrences minus the cancelled one
            result.ShouldNotContain(x => x.StartDate == cancelledDate);
        }
    }
}
