using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using ClefCraft.Application.Common.Helpers;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class RecurringEventProjectionService
        : IRecurringEventProjectionService
    {
        private readonly ICalendarEventExceptionRepository _exceptionRepo;
        private readonly IRecurrenceSeriesRepository _seriesRepo;

        public RecurringEventProjectionService(
            ICalendarEventExceptionRepository exceptionRepo,
            IRecurrenceSeriesRepository seriesRepo)
        {
            _exceptionRepo = exceptionRepo;
            _seriesRepo = seriesRepo;
        }

        public async Task<List<CalendarEventInstanceDto>> ProjectAsync(
            List<CalendarEvent> events,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd)
        {
            var result = new List<CalendarEventInstanceDto>();

            var recurringEvents = events
                .Where(x => x.IsRecurring)
                .ToList();

            var nonRecurringEvents = events
                .Where(x => !x.IsRecurring)
                .ToList();

            // NON-RECURRING EVENTS
            foreach (var ev in nonRecurringEvents)
            {
                ev.BaseEventId = ev.Id;

                result.Add(ToDto(
                    ev,
                    ev.StartDate));
            }

            // RECURRING EVENTS
            foreach (var rootEvent in recurringEvents)
            {
                rootEvent.BaseEventId = rootEvent.Id;

                // Try segment architecture first
                var series =
                    await _seriesRepo.GetBySeriesUidAsync(
                        rootEvent.SeriesUid);

                // FALLBACK TO LEGACY
                if (series == null || !series.Segments.Any())
                {
                    var legacy =
                        await ExpandLegacyAsync(
                            rootEvent,
                            rangeStart,
                            rangeEnd);

                    result.AddRange(legacy);

                    continue;
                }

                var projected =
                    await ExpandSegmentSeriesAsync(
                        rootEvent,
                        series,
                        rangeStart,
                        rangeEnd);

                result.AddRange(projected);
            }

            return result
                .Where(x =>
                    x.StartDate < rangeEnd &&
                    x.EndDate > rangeStart)
                .OrderBy(x => x.StartDate)
                .ToList();
        }

        private async Task<List<CalendarEventInstanceDto>>
            ExpandLegacyAsync(
                CalendarEvent rootEvent,
                DateTimeOffset rangeStart,
                DateTimeOffset rangeEnd)
        {
            if (string.IsNullOrWhiteSpace(
                rootEvent.RecurrenceRuleJson))
            {
                return new();
            }

            var exceptions =
                await _exceptionRepo.GetBySeriesUid(
                    rootEvent.SeriesUid);

            var rule =
                JsonSerializer.Deserialize<RecurrenceRule>(
                    rootEvent.RecurrenceRuleJson);

            if (rule == null)
                return new();

            var expanded =
                RecurrenceHelper.ExpandEvent(
                    rootEvent,
                    rule,
                    exceptions,
                    rangeStart,
                    rangeEnd);

            return expanded
                .Select(x => ToDto(x, x.StartDate))
                .ToList();
        }

        private async Task<List<CalendarEventInstanceDto>>
            ExpandSegmentSeriesAsync(
                CalendarEvent rootEvent,
                RecurrenceSeries series,
                DateTimeOffset rangeStart,
                DateTimeOffset rangeEnd)
        {
            var exceptions =
                await _exceptionRepo.GetBySeriesUid(
                    rootEvent.SeriesUid);

            var occurrences =
                new List<CalendarEvent>();

            var orderedSegments =
                series.Segments
                    .OrderBy(x => x.EffectiveFrom)
                    .ToList();

            foreach (var segment in orderedSegments)
            {
                if (string.IsNullOrWhiteSpace(
                    segment.RecurrenceRuleJson))
                {
                    continue;
                }

                var rule =
                    JsonSerializer.Deserialize<RecurrenceRule>(
                        segment.RecurrenceRuleJson);

                if (rule == null)
                    continue;

                var effectiveRangeStart = segment.EffectiveFrom > rangeStart
                    ? segment.EffectiveFrom
                    : rangeStart;

                var effectiveRangeEnd = segment.EffectiveTo.HasValue
                    ? (segment.EffectiveTo.Value < rangeEnd ? segment.EffectiveTo.Value : rangeEnd)
                    : rangeEnd;

                // FIX: Allow matching evaluation boundaries if the segment contains occurrences exactly on the line
                if (effectiveRangeStart > effectiveRangeEnd)
                    continue;

                var virtualEvent =
                    BuildVirtualEvent(
                        rootEvent,
                        segment);

                var expanded =
                    RecurrenceHelper.ExpandEvent(
                        virtualEvent,
                        rule,
                        exceptions,
                        effectiveRangeStart,
                        effectiveRangeEnd);

                occurrences.AddRange(expanded);
            }

            // IMPORTANT:
            // Deduplicate overlapping boundary occurrences
            occurrences =
                occurrences
                    .GroupBy(x =>
                        x.StartDate.UtcDateTime)
                    .Select(x => x.First())
                    .OrderBy(x => x.StartDate)
                    .ToList();

            return occurrences
                .Select(x => ToDto(x, x.StartDate))
                .ToList();
        }

        private CalendarEvent BuildVirtualEvent(
            CalendarEvent rootEvent,
            CalendarEventSegment segment)
        {
            return new CalendarEvent
            {
                Id = rootEvent.Id,
                BaseEventId = rootEvent.Id,
                SeriesUid = rootEvent.SeriesUid,
                Subject = segment.Subject,
                Location = segment.Location,
                Comment = segment.Comment,
                StartDate = segment.StartDate,
                EndDate = segment.EndDate,
                AllDayEvent = rootEvent.AllDayEvent,
                EventTypeId = segment.EventTypeId,
                Importance = segment.Importance,
                IsRecurring = segment.IsRecurring,
                RecurrenceRuleJson =
                    segment.RecurrenceRuleJson,
                LinkedBoardItemId =
                    rootEvent.LinkedBoardItemId
            };
        }

        private CalendarEventInstanceDto ToDto(
            CalendarEvent ev,
            DateTimeOffset occurrenceDate)
        {
            var occurrenceKey =
                $"{ev.SeriesUid}_{occurrenceDate.UtcDateTime:yyyyMMddHHmmss}";

            return new CalendarEventInstanceDto
            {
                Id = ev.Id,
                BaseEventId = ev.BaseEventId,
                SeriesUid = ev.SeriesUid,
                OccurrenceKey = occurrenceKey,
                OccurrenceDate = occurrenceDate,
                Subject = ev.Subject,
                Location = ev.Location,
                Comment = ev.Comment,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate,
                AllDayEvent = ev.AllDayEvent,
                EventTypeId = ev.EventTypeId,
                EventTypeName = ev.EventType?.Name,
                EventColor = ev.EventType?.Color,
                Importance = ev.Importance,
                IsRecurring = ev.IsRecurring,
                RecurrenceRuleJson =
                    ev.RecurrenceRuleJson,
                LinkedBoardItemId =
                    ev.LinkedBoardItemId
            };
        }
    }
}