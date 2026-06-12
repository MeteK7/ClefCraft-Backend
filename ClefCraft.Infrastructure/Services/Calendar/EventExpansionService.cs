using ClefCraft.Application.Common.Helpers;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System.Collections.Generic;
using System.Text.Json;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class EventExpansionService : IEventExpansionService
    {
        private readonly ICalendarEventExceptionRepository _exceptionRepo;

        public EventExpansionService(
            ICalendarEventExceptionRepository exceptionRepo)
        {
            _exceptionRepo = exceptionRepo;
        }

        public async Task<List<CalendarEventInstanceDto>> ExpandAsync(
            List<CalendarEvent> events,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd)
        {
            var seriesUids = events
                .Where(e => !string.IsNullOrWhiteSpace(e.SeriesUid))
                .Select(e => e.SeriesUid)
                .Distinct()
                .ToList();

            var exceptions =
                await _exceptionRepo.GetBySeriesUids(seriesUids);

            var result = new List<CalendarEventInstanceDto>();

            foreach (var ev in events)
            {
                // Ensure analytics identity always exists
                ev.BaseEventId = ev.Id;

                if (!ev.IsRecurring ||
                    string.IsNullOrWhiteSpace(ev.RecurrenceRuleJson))
                {
                    result.Add(ToDto(ev, ev.StartDate));
                    continue;
                }

                var rule = JsonSerializer.Deserialize<RecurrenceRule>(
                    ev.RecurrenceRuleJson);

                if (rule == null)
                    continue;

                var occurrences = RecurrenceHelper.ExpandEvent(
                    ev,
                    rule,
                    exceptions,
                    rangeStart,
                    rangeEnd);

                foreach (var occ in occurrences)
                {
                    result.Add(ToDto(occ, occ.StartDate));
                }
            }

            return result;
        }

        private CalendarEventInstanceDto ToDto(
            CalendarEvent ev,
            DateTimeOffset occurrenceDate)
        {
            var occurrenceKey =
                $"{ev.SeriesUid}_{occurrenceDate.UtcDateTime:yyyyMMddHHmmss}";

            return new CalendarEventInstanceDto
            {
                // Physical DB row
                Id = ev.Id,

                // Analytics identity
                BaseEventId = ev.BaseEventId,

                // Logical recurrence identity
                SeriesUid = ev.SeriesUid,

                // Stable UI identity
                OccurrenceKey = occurrenceKey,

                // Exact occurrence identity
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
                RecurrenceRuleJson = ev.RecurrenceRuleJson,
                LinkedBoardItemId = ev.LinkedBoardItemId
            };
        }
    }
}