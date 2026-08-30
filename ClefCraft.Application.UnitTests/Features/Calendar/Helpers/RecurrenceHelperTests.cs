using ClefCraft.Application.Common.Helpers;
using ClefCraft.Domain;
using Shouldly;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Helpers
{
    public class RecurrenceHelperTests
    {
        private static CalendarEvent MakeSourceEvent(DateTimeOffset start, DateTimeOffset end, string seriesUid = "series-1")
        {
            return new CalendarEvent
            {
                Id = 1,
                Subject = "Test event",
                UserId = "user-1",
                SeriesUid = seriesUid,
                StartDate = start,
                EndDate = end,
                IsRecurring = true
            };
        }

        // ------------------------------------------------------------------
        // WEEKLY + DaysOfWeek
        // ------------------------------------------------------------------

        [Fact]
        public void ExpandEvent_WeeklyWithMultipleDaysOfWeek_OnlyGeneratesSelectedWeekdays()
        {
            // 2026-01-05 is a Monday.
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
            var sourceEvent = MakeSourceEvent(start, start.AddHours(1));

            var rule = new RecurrenceRule
            {
                Frequency = "WEEKLY",
                Interval = 1,
                DaysOfWeek = new List<int> { 1, 3, 5 } // Mon, Wed, Fri
            };

            var rangeEnd = start.AddDays(15); // exclusive — through 2026-01-20

            var occurrences = RecurrenceHelper.ExpandEvent(
                sourceEvent, rule, new List<CalendarEventException>(), start, rangeEnd);

            occurrences.Select(o => o.StartDate.Date).ShouldBe(new[]
            {
                new DateTime(2026, 1, 5),
                new DateTime(2026, 1, 7),
                new DateTime(2026, 1, 9),
                new DateTime(2026, 1, 12),
                new DateTime(2026, 1, 14),
                new DateTime(2026, 1, 16),
                new DateTime(2026, 1, 19),
            });

            occurrences.ShouldAllBe(o =>
                o.StartDate.DayOfWeek == DayOfWeek.Monday ||
                o.StartDate.DayOfWeek == DayOfWeek.Wednesday ||
                o.StartDate.DayOfWeek == DayOfWeek.Friday);
        }

        [Fact]
        public void ExpandEvent_WeeklyWithoutDaysOfWeek_KeepsSingleWeekdayEveryIntervalWeeksBehavior()
        {
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
            var sourceEvent = MakeSourceEvent(start, start.AddHours(1));

            var rule = new RecurrenceRule
            {
                Frequency = "WEEKLY",
                Interval = 2,
                DaysOfWeek = null
            };

            var rangeEnd = start.AddDays(56); // exclusive

            var occurrences = RecurrenceHelper.ExpandEvent(
                sourceEvent, rule, new List<CalendarEventException>(), start, rangeEnd);

            occurrences.Count.ShouldBe(4);
            for (var i = 0; i < occurrences.Count; i++)
            {
                occurrences[i].StartDate.ShouldBe(start.AddDays(14 * i));
            }
        }

        // ------------------------------------------------------------------
        // MONTHLY / YEARLY day-of-month drift
        // ------------------------------------------------------------------

        [Fact]
        public void ExpandEvent_MonthlyStartingOn31st_DoesNotDriftAfterAShortMonth()
        {
            var start = new DateTimeOffset(2026, 1, 31, 9, 0, 0, TimeSpan.Zero);
            var sourceEvent = MakeSourceEvent(start, start.AddHours(1));

            var rule = new RecurrenceRule
            {
                Frequency = "MONTHLY",
                Interval = 1
            };

            var rangeEnd = start.AddMonths(6); // exclusive — through 2026-07-31

            var occurrences = RecurrenceHelper.ExpandEvent(
                sourceEvent, rule, new List<CalendarEventException>(), start, rangeEnd);

            occurrences.Count.ShouldBe(6);
            occurrences[0].StartDate.Date.ShouldBe(new DateTime(2026, 1, 31)); // Jan
            occurrences[1].StartDate.Date.ShouldBe(new DateTime(2026, 2, 28)); // Feb (clamped, not leap)
            // The critical regression check: March must anchor back to the
            // original day 31, not drift forward from February's clamped 28.
            occurrences[2].StartDate.Date.ShouldBe(new DateTime(2026, 3, 31));
            occurrences[3].StartDate.Date.ShouldBe(new DateTime(2026, 4, 30)); // April (clamped)
            occurrences[4].StartDate.Date.ShouldBe(new DateTime(2026, 5, 31));
            occurrences[5].StartDate.Date.ShouldBe(new DateTime(2026, 6, 30)); // June (clamped)
        }

        [Fact]
        public void ExpandEvent_YearlyStartingOnFeb29_DoesNotDriftAcrossNonLeapYears()
        {
            var start = new DateTimeOffset(2024, 2, 29, 9, 0, 0, TimeSpan.Zero); // 2024 is a leap year
            var sourceEvent = MakeSourceEvent(start, start.AddHours(1));

            var rule = new RecurrenceRule
            {
                Frequency = "YEARLY",
                Interval = 1
            };

            var rangeEnd = start.AddYears(5); // exclusive — through 2029-02-28

            var occurrences = RecurrenceHelper.ExpandEvent(
                sourceEvent, rule, new List<CalendarEventException>(), start, rangeEnd);

            occurrences.Count.ShouldBe(5); // 2024-2028 (2029-02-28 == rangeEnd is excluded)
            occurrences[0].StartDate.Date.ShouldBe(new DateTime(2024, 2, 29));
            occurrences[1].StartDate.Date.ShouldBe(new DateTime(2025, 2, 28)); // clamped
            // Anchor check: the following years must still resolve from the
            // original Feb 29, not drift from 2025's clamped Feb 28.
            occurrences[2].StartDate.Date.ShouldBe(new DateTime(2026, 2, 28));
            occurrences[3].StartDate.Date.ShouldBe(new DateTime(2027, 2, 28));
            occurrences[4].StartDate.Date.ShouldBe(new DateTime(2028, 2, 29)); // 2028 is a leap year again
        }

        // ------------------------------------------------------------------
        // Count / EndDate boundaries
        // ------------------------------------------------------------------

        [Fact]
        public void ExpandEvent_WithCount_StopsExactlyAtTheLimit()
        {
            var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var sourceEvent = MakeSourceEvent(start, start.AddHours(1));

            var rule = new RecurrenceRule { Frequency = "DAILY", Interval = 1, Count = 3 };

            var occurrences = RecurrenceHelper.ExpandEvent(
                sourceEvent, rule, new List<CalendarEventException>(), start, start.AddDays(100));

            occurrences.Count.ShouldBe(3);
            occurrences.Last().StartDate.ShouldBe(start.AddDays(2));
        }

        [Fact]
        public void ExpandEvent_WithEndDate_IncludesTheEndDateItselfButNothingAfter()
        {
            var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var sourceEvent = MakeSourceEvent(start, start.AddHours(1));

            var rule = new RecurrenceRule { Frequency = "DAILY", Interval = 1, EndDate = start.AddDays(2) };

            var occurrences = RecurrenceHelper.ExpandEvent(
                sourceEvent, rule, new List<CalendarEventException>(), start, start.AddDays(100));

            occurrences.Count.ShouldBe(3);
            occurrences.Last().StartDate.ShouldBe(start.AddDays(2));
        }

        // ------------------------------------------------------------------
        // Exceptions still apply correctly on top of the new expansion loop
        // ------------------------------------------------------------------

        [Fact]
        public void ExpandEvent_CancelledException_RemovesThatOccurrenceOnly()
        {
            var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var sourceEvent = MakeSourceEvent(start, start.AddHours(1));

            var rule = new RecurrenceRule { Frequency = "DAILY", Interval = 1, Count = 3 };

            var exceptions = new List<CalendarEventException>
            {
                new CalendarEventException
                {
                    SeriesUid = sourceEvent.SeriesUid,
                    OccurrenceDate = start.AddDays(1),
                    IsCancelled = true
                }
            };

            var occurrences = RecurrenceHelper.ExpandEvent(
                sourceEvent, rule, exceptions, start, start.AddDays(100));

            occurrences.Count.ShouldBe(2);
            occurrences.ShouldNotContain(o => o.StartDate.Date == start.AddDays(1).Date);
        }

        [Fact]
        public void ExpandEvent_ModifiedException_OverridesSubjectForThatOccurrenceOnly()
        {
            var start = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
            var sourceEvent = MakeSourceEvent(start, start.AddHours(1));

            var rule = new RecurrenceRule { Frequency = "DAILY", Interval = 1, Count = 3 };

            var exceptions = new List<CalendarEventException>
            {
                new CalendarEventException
                {
                    SeriesUid = sourceEvent.SeriesUid,
                    OccurrenceDate = start.AddDays(1),
                    Subject = "Rescheduled meeting"
                }
            };

            var occurrences = RecurrenceHelper.ExpandEvent(
                sourceEvent, rule, exceptions, start, start.AddDays(100));

            occurrences.Single(o => o.StartDate.Date == start.AddDays(1).Date).Subject.ShouldBe("Rescheduled meeting");
            occurrences.Single(o => o.StartDate.Date == start.Date).Subject.ShouldBe("Test event");
        }

        // ------------------------------------------------------------------
        // ValidateRule
        // ------------------------------------------------------------------

        [Fact]
        public void ValidateRule_ValidRule_DoesNotThrow()
        {
            var rule = new RecurrenceRule { Frequency = "WEEKLY", Interval = 1, Count = 5 };
            Should.NotThrow(() => RecurrenceHelper.ValidateRule(rule, DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData("BIWEEKLY")]
        [InlineData("")]
        public void ValidateRule_UnsupportedFrequency_Throws(string frequency)
        {
            var rule = new RecurrenceRule { Frequency = frequency, Interval = 1 };
            Should.Throw<ValidationException>(() => RecurrenceHelper.ValidateRule(rule, DateTimeOffset.UtcNow));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidateRule_NonPositiveInterval_Throws(int interval)
        {
            var rule = new RecurrenceRule { Frequency = "DAILY", Interval = interval };
            Should.Throw<ValidationException>(() => RecurrenceHelper.ValidateRule(rule, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void ValidateRule_NonPositiveCount_Throws()
        {
            var rule = new RecurrenceRule { Frequency = "DAILY", Interval = 1, Count = 0 };
            Should.Throw<ValidationException>(() => RecurrenceHelper.ValidateRule(rule, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void ValidateRule_EndDateBeforeEventStart_Throws()
        {
            var start = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
            var rule = new RecurrenceRule { Frequency = "DAILY", Interval = 1, EndDate = start.AddDays(-1) };
            Should.Throw<ValidationException>(() => RecurrenceHelper.ValidateRule(rule, start));
        }

        [Fact]
        public void ValidateRule_DaysOfWeekOutOfRange_Throws()
        {
            var rule = new RecurrenceRule { Frequency = "WEEKLY", Interval = 1, DaysOfWeek = new List<int> { 0, 7 } };
            Should.Throw<ValidationException>(() => RecurrenceHelper.ValidateRule(rule, DateTimeOffset.UtcNow));
        }
    }
}
