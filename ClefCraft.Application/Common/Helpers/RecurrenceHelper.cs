using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Application.Common.Helpers
{
    public static class RecurrenceHelper
    {
        private static CalendarEvent? ApplyException(
            CalendarEvent occurrence,
            CalendarEvent sourceEvent,
            List<CalendarEventException> exceptions)
        {
            var occurrenceDate =
                DateOnly.FromDateTime(
                    occurrence.StartDate.UtcDateTime);

            var exception = exceptions.FirstOrDefault(x =>
                x.SeriesUid == sourceEvent.SeriesUid &&
                DateOnly.FromDateTime(
                    x.OccurrenceDate.UtcDateTime) == occurrenceDate);

            if (exception == null)
                return occurrence;

            if (exception.IsCancelled)
                return null;

            occurrence.Subject =
                exception.Subject ?? occurrence.Subject;

            occurrence.Comment =
                exception.Comment ?? occurrence.Comment;

            occurrence.StartDate =
                exception.StartDate ?? occurrence.StartDate;

            occurrence.EndDate =
                exception.EndDate ?? occurrence.EndDate;

            occurrence.Location =
                exception.Location ?? occurrence.Location;

            occurrence.EventTypeId =
                exception.EventTypeId ?? occurrence.EventTypeId;

            return occurrence;
        }

        public static List<CalendarEvent> ExpandEvent(
            CalendarEvent sourceEvent,
            RecurrenceRule rule,
            List<CalendarEventException> exceptions,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd)
        {
            var result = new List<CalendarEvent>();

            var current = sourceEvent.StartDate;

            var generated = 0;

            while (current <= rangeEnd)
            {
                if (rule.Count.HasValue &&
                    generated >= rule.Count.Value)
                {
                    break;
                }

                if (rule.EndDate.HasValue &&
                    current > rule.EndDate.Value)
                {
                    break;
                }

                if (current >= rangeStart)
                {
                    var duration =
                        sourceEvent.EndDate - sourceEvent.StartDate;

                    var occurrence = new CalendarEvent
                    {
                        Id = sourceEvent.Id,
                        BaseEventId = sourceEvent.Id,
                        SeriesUid = sourceEvent.SeriesUid,
                        Subject = sourceEvent.Subject,
                        Location = sourceEvent.Location,
                        Comment = sourceEvent.Comment,
                        StartDate = current,
                        EndDate = current + duration,
                        AllDayEvent = sourceEvent.AllDayEvent,
                        EventTypeId = sourceEvent.EventTypeId,
                        Importance = sourceEvent.Importance,
                        IsRecurring = true,
                        RecurrenceRuleJson =
                            sourceEvent.RecurrenceRuleJson,
                        LinkedBoardItemId =
                            sourceEvent.LinkedBoardItemId
                    };

                    occurrence = ApplyException(
                        occurrence,
                        sourceEvent,
                        exceptions);

                    if (occurrence != null)
                    {
                        result.Add(occurrence);
                    }
                }

                generated++;

                current = rule.Frequency switch
                {
                    "DAILY" =>
                        current.AddDays(rule.Interval),
                    "WEEKLY" =>
                        current.AddDays(7 * rule.Interval),
                    "MONTHLY" =>
                        current.AddMonths(rule.Interval),
                    "YEARLY" =>
                        current.AddYears(rule.Interval),

                    _ => throw new NotSupportedException(
                        $"Unsupported frequency: {rule.Frequency}")
                };
            }

            return result;
        }
    }
}