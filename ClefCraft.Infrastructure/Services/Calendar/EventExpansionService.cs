using ClefCraft.Application.Common.Helpers;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System.Collections.Generic;
using System.Text.Json;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class EventExpansionService : IEventExpansionService
    {
        private readonly ICalendarEventExceptionRepository _exceptionRepo;

        public EventExpansionService(ICalendarEventExceptionRepository exceptionRepo)
        {
            _exceptionRepo = exceptionRepo;
        }

        public async Task<List<CalendarEventInstanceDto>> ExpandAsync(
            List<CalendarEvent> events,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd)
        {
            var seriesUids = events
                .Select(e => e.BaseEventId.ToString())
                .Distinct()
                .ToList();

            var exceptions = await _exceptionRepo.GetBySeriesUids(seriesUids);

            var result = new List<CalendarEventInstanceDto>();

            foreach (var ev in events)
            {
                if (!ev.IsRecurring || string.IsNullOrEmpty(ev.RecurrenceRuleJson))
                {
                    result.Add(ToDto(ev, ev.StartDate));
                    continue;
                }

                var rule = JsonSerializer.Deserialize<RecurrenceRule>(ev.RecurrenceRuleJson);

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

        private CalendarEventInstanceDto ToDto(CalendarEvent ev, DateTimeOffset date)
        {
            return new CalendarEventInstanceDto
            {
                Id = ev.Id,
                BaseEventId = ev.BaseEventId,

                Subject = ev.Subject,
                Location = ev.Location,
                Comment = ev.Comment,

                StartDate = date,
                EndDate = date + (ev.EndDate - ev.StartDate),

                AllDayEvent = ev.AllDayEvent,
                EventTypeId = ev.EventTypeId,
                Importance = ev.Importance
            };
        }
    }
}