using ClefCraft.Application.Common.Helpers;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
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

        public async Task<List<CalendarEvent>> ExpandAsync(
            List<CalendarEvent> events,
            DateTimeOffset start,
            DateTimeOffset end)
        {
            var eventIds = events.Select(e => e.Id).ToList();
            var exceptions = await _exceptionRepo.GetByEventIdsAsync(eventIds);

            var result = new List<CalendarEvent>();

            foreach (var e in events)
            {
                if (!e.IsRecurring || string.IsNullOrEmpty(e.RecurrenceRuleJson))
                {
                    // Non-recurring: BaseEventId == its own Id
                    e.BaseEventId = e.Id;
                    result.Add(e);
                    continue;
                }

                try
                {
                    var rule = JsonSerializer.Deserialize<RecurrenceRule>(e.RecurrenceRuleJson);
                    var occurrences = RecurrenceHelper.ExpandEvent(e, rule, exceptions, start, end);

                    foreach (var occ in occurrences)
                    {
                        occ.BaseEventId = e.Id; // preserve original before overwriting
                        occ.Id = GenerateInstanceId(e.Id, occ.StartDate);
                        result.Add(occ);
                    }
                }
                catch
                {
                    e.BaseEventId = e.Id;
                    result.Add(e);
                }
            }

            return result;
        }

        private static int GenerateInstanceId(int baseId, DateTimeOffset date)
        {
            int raw = HashCode.Combine(baseId, date.UtcTicks);
            return raw & 0x7FFFFFFF;
        }
    }
}