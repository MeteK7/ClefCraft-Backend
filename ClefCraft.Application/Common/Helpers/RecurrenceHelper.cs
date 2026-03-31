using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Common.Helpers
{
    public static class RecurrenceHelper
    {
        private static DateTimeOffset GetNextWeekday(DateTimeOffset start, int targetDay)
        {
            int currentDay = (int)start.DayOfWeek;
            int diff = (targetDay - currentDay + 7) % 7;

            return start.AddDays(diff);
        }

        public static List<CalendarEventDto> ExpandEvent(
            CalendarEvent ev,
            RecurrenceRule rule,
            List<CalendarEventException> exceptions,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd)
        {
            var result = new List<CalendarEventDto>();

            var current = ev.StartDate;

            int occurrences = 0;

            while (current <= rangeEnd)
            {
                if (rule.Count.HasValue && occurrences >= rule.Count)
                    break;

                if (rule.EndDate.HasValue && current > rule.EndDate)
                    break;

                // ✅ SPECIAL HANDLING FOR WEEKLY
                if (rule.Frequency == "WEEKLY" && rule.DaysOfWeek?.Any() == true)
                {
                    foreach (var day in rule.DaysOfWeek)
                    {
                        var next = GetNextWeekday(current, day);

                        if (next >= rangeStart && next <= rangeEnd)
                        {
                            var dto = new CalendarEventDto
                            {
                                Id = ev.Id,
                                Subject = ev.Subject,
                                StartDate = next,
                                EndDate = next + (ev.EndDate - ev.StartDate),
                                AllDayEvent = ev.AllDayEvent,
                                EventTypeId = ev.EventTypeId,
                                Importance = ev.Importance,
                                Comment = ev.Comment,
                                LinkedBoardItemId = ev.LinkedBoardItemId
                            };

                            // 👉 APPLY EXCEPTION HERE
                            var occurrenceDate = next;

                            var exception = exceptions.FirstOrDefault(x =>
                                x.CalendarEventId == ev.Id &&
                                x.OccurrenceDate.Date == occurrenceDate.Date);

                            if (exception != null)
                            {
                                if (exception.IsCancelled)
                                    continue;

                                dto.Subject = exception.Subject ?? dto.Subject;
                                dto.Comment = exception.Comment ?? dto.Comment;
                                dto.StartDate = exception.StartDate ?? dto.StartDate;
                                dto.EndDate = exception.EndDate ?? dto.EndDate;
                            }

                            result.Add(dto);

                            occurrences++;
                        }
                    }

                    current = current.AddDays(7 * rule.Interval);
                    continue;
                }

                // ✅ DEFAULT FLOW (daily/monthly/yearly)
                if (current >= rangeStart)
                {
                    var dto = new CalendarEventDto
                    {
                        Id = ev.Id,
                        Subject = ev.Subject,
                        StartDate = current,
                        EndDate = current + (ev.EndDate - ev.StartDate),
                        AllDayEvent = ev.AllDayEvent,
                        EventTypeId = ev.EventTypeId,
                        Importance = ev.Importance,
                        Comment = ev.Comment,
                        LinkedBoardItemId = ev.LinkedBoardItemId
                    };

                    // 👉 APPLY EXCEPTION HERE
                    var occurrenceDate = current;

                    var exception = exceptions.FirstOrDefault(x =>
                        x.CalendarEventId == ev.Id &&
                        x.OccurrenceDate.Date == occurrenceDate.Date);

                    if (exception != null)
                    {
                        if (exception.IsCancelled)
                            continue;

                        dto.Subject = exception.Subject ?? dto.Subject;
                        dto.Comment = exception.Comment ?? dto.Comment;
                        dto.StartDate = exception.StartDate ?? dto.StartDate;
                        dto.EndDate = exception.EndDate ?? dto.EndDate;
                    }

                    result.Add(dto);

                    occurrences++;
                }

                current = rule.Frequency switch
                {
                    "DAILY" => current.AddDays(rule.Interval),
                    "MONTHLY" => current.AddMonths(rule.Interval),
                    "YEARLY" => current.AddYears(rule.Interval),
                    _ => current.AddDays(1)
                };
            }

            return result;
        }
    }
}
