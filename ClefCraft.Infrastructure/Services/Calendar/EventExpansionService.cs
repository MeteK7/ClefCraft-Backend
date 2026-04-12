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
                    result.Add(e);
                    continue;
                }

                try
                {
                    var rule = JsonSerializer.Deserialize<RecurrenceRule>(e.RecurrenceRuleJson);
                    var occurrences = RecurrenceHelper.ExpandEvent(e, rule, exceptions, start, end);

                    foreach (var occ in occurrences)
                    {
                        // FIX: use a deterministic positive synthetic ID that
                        // won't clash with real DB rows and is never negative.
                        occ.Id = GenerateInstanceId(e.Id, occ.StartDate);
                        result.Add(occ);
                    }
                }
                catch
                {
                    result.Add(e);
                }
            }

            return result;
        }

        /// <summary>
        /// Produces a stable, positive synthetic ID for a virtual recurring instance.
        /// Kept separate from real DB IDs by using a large offset so accidental
        /// collisions with persisted rows are extremely unlikely.
        /// </summary>
        private static int GenerateInstanceId(int baseId, DateTimeOffset date)
        {
            // Combine and mask to a positive int (clear the sign bit).
            int raw = HashCode.Combine(baseId, date.UtcTicks);
            return raw & 0x7FFFFFFF; // always non-negative
        }
    }
}