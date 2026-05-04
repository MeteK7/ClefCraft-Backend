using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Application.Common.Helpers
{
    public static class RecurrenceHelper
    {
        private static DateTimeOffset GetNextWeekday(DateTimeOffset start, int targetDay)
        {
            int currentDay = (int)start.DayOfWeek;
            int diff = (targetDay - currentDay + 7) % 7;
            return start.AddDays(diff == 0 ? 7 : diff); // always move forward
        }

        private static CalendarEvent ApplyException(CalendarEvent occurrence, CalendarEvent ev, List<CalendarEventException> exceptions)
        {
            var occurrenceDate = DateOnly.FromDateTime(occurrence.StartDate.UtcDateTime);
            var exception = exceptions.FirstOrDefault(x =>
                x.CalendarEventId == ev.Id &&
                DateOnly.FromDateTime(x.OccurrenceDate.UtcDateTime) == occurrenceDate);

            if (exception != null)
            {
                if (exception.IsCancelled)
                    return null;

                occurrence.Subject = exception.Subject ?? occurrence.Subject;
                occurrence.Comment = exception.Comment ?? occurrence.Comment;
                occurrence.StartDate = exception.StartDate ?? occurrence.StartDate;
                occurrence.EndDate = exception.EndDate ?? occurrence.EndDate;
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
            int occurrences = 0;

            while (current <= rangeEnd)
            {
                if (rule.Count.HasValue && occurrences >= rule.Count)
                    break;

                if (rule.EndDate.HasValue && current > rule.EndDate)
                    break;

                if (rule.Frequency == "WEEKLY" && rule.DaysOfWeek?.Any() == true)
                {
                    foreach (var day in rule.DaysOfWeek)
                    {
                        var next = GetNextWeekday(current, day);

                        if (next >= rangeStart && next <= rangeEnd)
                        {
                            var occurrence = new CalendarEvent
                            {
                                Id = ev.Id,
                                Subject = ev.Subject,
                                StartDate = next,
                                EndDate = next + (ev.EndDate - ev.StartDate),
                                AllDayEvent = ev.AllDayEvent,
                                EventTypeId = ev.EventTypeId,
                                Importance = ev.Importance,
                                Comment = ev.Comment,
                                LinkedBoardItemId = ev.LinkedBoardItemId,
                                IsRecurring = ev.IsRecurring,      
                                RecurrenceRuleJson = ev.RecurrenceRuleJson
                            };

                            occurrence = ApplyException(occurrence, ev, exceptions);
                            if (occurrence != null)
                            {
                                result.Add(occurrence);
                                occurrences++;
                            }
                        }
                    }

                    current = current.AddDays(7 * rule.Interval);
                    continue;
                }

                if (current >= rangeStart)
                {
                    var occurrence = new CalendarEvent
                    {
                        Id = ev.Id,
                        Subject = ev.Subject,
                        StartDate = current,
                        EndDate = current + (ev.EndDate - ev.StartDate),
                        AllDayEvent = ev.AllDayEvent,
                        EventTypeId = ev.EventTypeId,
                        Importance = ev.Importance,
                        Comment = ev.Comment,
                        LinkedBoardItemId = ev.LinkedBoardItemId,
                        IsRecurring = ev.IsRecurring,
                        RecurrenceRuleJson = ev.RecurrenceRuleJson
                    };

                    occurrence = ApplyException(occurrence, ev, exceptions);
                    if (occurrence != null)
                    {
                        result.Add(occurrence);
                        occurrences++;
                    }
                }

                current = rule.Frequency switch
                {
                    "DAILY" => current.AddDays(rule.Interval),
                    "MONTHLY" => current.AddMonths(rule.Interval),
                    "YEARLY" => current.AddYears(rule.Interval),
                    _ => throw new NotSupportedException($"Unsupported frequency: {rule.Frequency}")
                };
            }

            return result;
        }
    }
}
