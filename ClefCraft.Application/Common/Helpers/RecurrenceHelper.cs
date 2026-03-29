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
        public static List<CalendarEventDto> ExpandEvent(
            CalendarEvent ev,
            RecurrenceRule rule,
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

                if (current >= rangeStart)
                {
                    result.Add(new CalendarEventDto
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
                    });

                    occurrences++;
                }

                current = rule.Frequency switch
                {
                    "DAILY" => current.AddDays(rule.Interval),
                    "WEEKLY" => current.AddDays(7 * rule.Interval),
                    "MONTHLY" => current.AddMonths(rule.Interval),
                    "YEARLY" => current.AddYears(rule.Interval),
                    _ => current.AddDays(1)
                };
            }

            return result;
        }
    }
}
