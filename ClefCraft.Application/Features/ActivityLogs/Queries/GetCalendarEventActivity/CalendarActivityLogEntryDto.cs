using ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity;
using System;
using System.Collections.Generic;

namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetCalendarEventActivity
{
    public class CalendarActivityLogEntryDto
    {
        public int Id { get; set; }

        // "Event" | "Segment" | "Exception" — which part of the recurrence model this entry
        // describes. Non-recurring events only ever produce "Event" entries.
        public string Scope { get; set; } = default!;

        public string ActionType { get; set; } = default!;
        public DateTime Timestamp { get; set; }
        public string ActorUserId { get; set; } = default!;
        public string ActorFullName { get; set; } = default!;
        public List<ActivityFieldChangeDto> Changes { get; set; } = new();

        // Populated only when Scope == "Segment": the date range this recurring-series default
        // change applied to.
        public DateTimeOffset? EffectiveFrom { get; set; }
        public DateTimeOffset? EffectiveTo { get; set; }

        // Populated only when Scope == "Exception": the single occurrence date this override/
        // cancellation applies to.
        public DateTimeOffset? OccurrenceDate { get; set; }
    }
}
