using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Application.Common.Helpers
{
    public static class RecurrenceHelper
    {
        private static CalendarEvent ApplyException(
            CalendarEvent occurrence,
            CalendarEvent ev,
            List<CalendarEventException> exceptions)
        {
            var occurrenceDate = DateOnly.FromDateTime(occurrence.StartDate.UtcDateTime);

            var exception = exceptions.FirstOrDefault(x =>
                x.SeriesUid == ev.BaseEventId.ToString() &&
                DateOnly.FromDateTime(x.OccurrenceDate.UtcDateTime) == occurrenceDate);

            if (exception != null)
            {
                if (exception.IsCancelled)
                    return null;

                occurrence.Subject = exception.Subject ?? occurrence.Subject;
                occurrence.Comment = exception.Comment ?? occurrence.Comment;
                occurrence.StartDate = exception.StartDate ?? occurrence.StartDate;
                occurrence.EndDate = exception.EndDate ?? occurrence.EndDate;
                occurrence.Location = exception.Location ?? occurrence.Location;
            }

            return occurrence;
        }

        public static List<CalendarEvent> ExpandEvent(
            CalendarEvent ev,
            RecurrenceRule rule,
            List<CalendarEventException> exceptions,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd)
        {
            var result = new List<CalendarEvent>();

            var current = ev.StartDate;

            while (current <= rangeEnd)
            {
                if (current >= rangeStart)
                {
                    var occurrence = new CalendarEvent
                    {
                        Id = ev.Id,
                        BaseEventId = ev.BaseEventId,

                        Subject = ev.Subject,
                        Location = ev.Location,
                        Comment = ev.Comment,

                        StartDate = current,
                        EndDate = current + (ev.EndDate - ev.StartDate),

                        AllDayEvent = ev.AllDayEvent,
                        EventTypeId = ev.EventTypeId,
                        Importance = ev.Importance
                    };

                    occurrence = ApplyException(occurrence, ev, exceptions);

                    if (occurrence != null)
                        result.Add(occurrence);
                }

                current = rule.Frequency switch
                {
                    "DAILY" => current.AddDays(rule.Interval),
                    "WEEKLY" => current.AddDays(7 * rule.Interval),
                    "MONTHLY" => current.AddMonths(rule.Interval),
                    "YEARLY" => current.AddYears(rule.Interval),
                    _ => throw new NotSupportedException()
                };
            }

            return result;
        }
    }
}