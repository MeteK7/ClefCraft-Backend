using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ClefCraft.Application.Common.Helpers
{
    public static class RecurrenceHelper
    {
        private static readonly HashSet<string> ValidFrequencies =
            new() { "DAILY", "WEEKLY", "MONTHLY", "YEARLY" };

        /// <summary>
        /// Validates a recurrence rule before it is persisted. Frequency support
        /// is otherwise only discovered at expansion time (NotSupportedException),
        /// and Interval/Count/DaysOfWeek/EndDate were previously unchecked.
        /// </summary>
        public static void ValidateRule(RecurrenceRule rule, DateTimeOffset eventStartDate)
        {
            if (rule == null)
                throw new ValidationException("Recurrence rule is required when IsRecurring is true.");

            if (string.IsNullOrWhiteSpace(rule.Frequency) || !ValidFrequencies.Contains(rule.Frequency))
                throw new ValidationException($"Unsupported recurrence frequency: {rule.Frequency}");

            if (rule.Interval < 1)
                throw new ValidationException("Recurrence interval must be at least 1.");

            if (rule.Count.HasValue && rule.Count.Value < 1)
                throw new ValidationException("Recurrence count must be at least 1.");

            if (rule.EndDate.HasValue && rule.EndDate.Value < eventStartDate)
                throw new ValidationException("Recurrence end date must not be before the event's start date.");

            if (rule.DaysOfWeek != null && rule.DaysOfWeek.Any(d => d < 0 || d > 6))
                throw new ValidationException("Recurrence daysOfWeek values must be between 0 (Sunday) and 6 (Saturday).");
        }

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

        /// <summary>
        /// Generates candidate occurrence dates for a rule, anchored to the
        /// original event start date, terminating once a candidate reaches
        /// rangeEnd (exclusive). Dispatches to a BYDAY-aware generator for
        /// WEEKLY rules that specify DaysOfWeek.
        /// </summary>
        private static IEnumerable<DateTimeOffset> GenerateCandidateDates(
            DateTimeOffset start,
            RecurrenceRule rule,
            DateTimeOffset rangeEnd)
        {
            if (rule.Frequency == "WEEKLY" && rule.DaysOfWeek is { Count: > 0 })
                return GenerateWeeklyByDayCandidates(start, rule, rangeEnd);

            return GenerateSimpleCandidates(start, rule, rangeEnd);
        }

        /// <summary>
        /// DAILY / WEEKLY(no DaysOfWeek) / MONTHLY / YEARLY candidates.
        /// Each candidate is computed directly from the original start date
        /// (start.AddMonths(Interval * occurrenceIndex) etc.) rather than by
        /// repeatedly advancing the previous candidate — AddMonths/AddYears
        /// clamps to the last valid day of a short month, and re-anchoring
        /// to that clamped value on the next iteration would permanently
        /// lose the original day-of-month (e.g. Jan 31 -> Feb 28 -> Mar 28
        /// instead of Mar 31).
        /// </summary>
        private static IEnumerable<DateTimeOffset> GenerateSimpleCandidates(
            DateTimeOffset start,
            RecurrenceRule rule,
            DateTimeOffset rangeEnd)
        {
            var occurrenceIndex = 0;

            while (true)
            {
                var current = rule.Frequency switch
                {
                    "DAILY" => start.AddDays(rule.Interval * occurrenceIndex),
                    "WEEKLY" => start.AddDays(7 * rule.Interval * occurrenceIndex),
                    "MONTHLY" => start.AddMonths(rule.Interval * occurrenceIndex),
                    "YEARLY" => start.AddYears(rule.Interval * occurrenceIndex),
                    _ => throw new NotSupportedException($"Unsupported frequency: {rule.Frequency}")
                };

                if (current >= rangeEnd)
                    yield break;

                yield return current;
                occurrenceIndex++;
            }
        }

        /// <summary>
        /// WEEKLY candidates honoring DaysOfWeek (0 = Sunday .. 6 = Saturday,
        /// matching DateTimeOffset.DayOfWeek and the frontend's day-picker
        /// index convention). Groups of selected weekdays repeat every
        /// Interval weeks, anchored to the week containing the event's
        /// original start date. Candidates before the start date (earlier
        /// weekdays within the very first active week) are skipped.
        /// </summary>
        private static IEnumerable<DateTimeOffset> GenerateWeeklyByDayCandidates(
            DateTimeOffset start,
            RecurrenceRule rule,
            DateTimeOffset rangeEnd)
        {
            var sortedDays = rule.DaysOfWeek!.Distinct().OrderBy(d => d).ToList();
            var weekStart = start.AddDays(-(int)start.DayOfWeek);
            var weekGroup = 0;

            while (true)
            {
                var currentWeekStart = weekStart.AddDays(7L * rule.Interval * weekGroup);

                if (currentWeekStart >= rangeEnd)
                    yield break;

                foreach (var day in sortedDays)
                {
                    var candidate = currentWeekStart.AddDays(day);

                    if (candidate < start || candidate >= rangeEnd)
                        continue;

                    yield return candidate;
                }

                weekGroup++;
            }
        }

        public static List<CalendarEvent> ExpandEvent(
            CalendarEvent sourceEvent,
            RecurrenceRule rule,
            List<CalendarEventException> exceptions,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd) // rangeEnd is EXCLUSIVE
        {
            var result = new List<CalendarEvent>();
            var generated = 0;

            foreach (var current in GenerateCandidateDates(sourceEvent.StartDate, rule, rangeEnd))
            {
                if (rule.Count.HasValue && generated >= rule.Count.Value)
                    break;

                if (rule.EndDate.HasValue && current > rule.EndDate.Value)
                    break;

                if (current >= rangeStart)
                {
                    var duration = sourceEvent.EndDate - sourceEvent.StartDate;

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
                        RecurrenceRuleJson = sourceEvent.RecurrenceRuleJson,
                        LinkedBoardItemId = sourceEvent.LinkedBoardItemId
                    };

                    occurrence = ApplyException(occurrence, sourceEvent, exceptions);

                    if (occurrence != null)
                        result.Add(occurrence);
                }

                generated++;
            }

            return result;
        }
    }
}
