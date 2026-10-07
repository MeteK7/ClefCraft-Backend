using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Application.Models.Analytics;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using ClefCraft.Infrastructure.Services.Calendar;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Queries
{
    // Recurring occurrences are projected from segments and single-occurrence edits, which carry
    // only an EventTypeId; the handler resolves those ids to a name and colour. The real
    // projection service runs here so the occurrences are shaped exactly as in production.
    public class GetCalendarEventsQueryHandlerTests
    {
        private const string Owner = "owner-1";
        private static readonly DateTimeOffset Start = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        private static readonly EventType Work = new() { Id = 7, Name = "Work", Color = "#111111", UserId = Owner };
        private static readonly EventType Focus = new() { Id = 8, Name = "Focus", Color = "#222222", UserId = Owner };
        private static readonly EventType Travel = new() { Id = 9, Name = "Travel", Color = "#333333", UserId = Owner };
        private static readonly EventType Personal = new() { Id = 10, Name = "Personal", Color = "#444444", UserId = Owner };

        private const string DailyRule = "{\"Frequency\":\"DAILY\",\"Interval\":1}";

        private sealed class Harness
        {
            public Mock<ICalendarEventRepository> Events { get; } = new();
            public Mock<IRecurrenceSeriesRepository> Series { get; } = new();
            public Mock<ICalendarEventExceptionRepository> Exceptions { get; } = new();
            public Mock<IEventTypeRepository> EventTypes { get; } = new();

            public GetCalendarEventsQueryHandler Handler()
            {
                var analytics = new Mock<IEventAnalyticsService>();
                analytics.Setup(a => a.BuildAsync(It.IsAny<List<CalendarEventDto>>(), It.IsAny<string>())).ReturnsAsync(new List<AIEventDto>());
                var prediction = new Mock<IAttendancePredictionService>();
                prediction.Setup(p => p.PredictAsync(It.IsAny<List<AIEventDto>>())).ReturnsAsync(new Dictionary<int, double?>());
                var reminders = new Mock<ICalendarReminderRepository>();
                reminders.Setup(r => r.GetByEventIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<CalendarReminder>());

                return new GetCalendarEventsQueryHandler(
                    Events.Object,
                    new RecurringEventProjectionService(Exceptions.Object, Series.Object),
                    new Mock<IEventEnrichmentService>().Object,
                    analytics.Object,
                    prediction.Object,
                    new Mock<IUserInteractionService>().Object,
                    reminders.Object,
                    EventTypes.Object,
                    new Mock<IUnitOfWork>().Object);
            }

            public Task<List<CalendarEventDto>> RunAsync() =>
                Handler().Handle(
                    new GetCalendarEventsQuery { UserId = Owner, RangeStart = Start, RangeEnd = Start.AddDays(3) },
                    CancellationToken.None);
        }

        [Fact]
        public async Task RecurringOccurrences_GetTheNameAndColourOfTheirOwnEventType_InOneLookup()
        {
            var harness = new Harness();

            // A series without segments (legacy expansion) whose second occurrence was edited to
            // another type, a series whose segment uses a different type than its root, and a
            // one-off event whose type was loaded with it.
            var legacy = new CalendarEvent
            {
                Id = 1, UserId = Owner, SeriesUid = "legacy", IsRecurring = true, Subject = "Legacy",
                StartDate = Start, EndDate = Start.AddHours(1), RecurrenceRuleJson = DailyRule,
                EventTypeId = Work.Id, EventType = Work
            };
            var segmented = new CalendarEvent
            {
                Id = 2, UserId = Owner, SeriesUid = "segmented", IsRecurring = true, Subject = "Segmented",
                StartDate = Start, EndDate = Start.AddHours(1), RecurrenceRuleJson = DailyRule,
                EventTypeId = Work.Id, EventType = Work
            };
            var oneOff = new CalendarEvent
            {
                Id = 3, UserId = Owner, SeriesUid = "one-off", IsRecurring = false, Subject = "One-off",
                StartDate = Start.AddHours(3), EndDate = Start.AddHours(4),
                EventTypeId = Personal.Id, EventType = Personal
            };
            harness.Events.Setup(r => r.GetByUserIdAsync(Owner, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(new List<CalendarEvent> { legacy, segmented, oneOff });

            harness.Series.Setup(r => r.GetBySeriesUidAsync("legacy")).ReturnsAsync((RecurrenceSeries?)null);
            harness.Series.Setup(r => r.GetBySeriesUidAsync("segmented")).ReturnsAsync(new RecurrenceSeries
            {
                Id = 5, SeriesUid = "segmented",
                Segments = new List<CalendarEventSegment>
                {
                    new()
                    {
                        RecurrenceSeriesId = 5, EffectiveFrom = Start, Subject = "Segmented",
                        StartDate = Start, EndDate = Start.AddHours(1), IsRecurring = true,
                        RecurrenceRuleJson = DailyRule, Importance = ImportanceLevel.Normal, TimeZoneId = "UTC",
                        EventTypeId = Travel.Id
                    }
                }
            });
            harness.Exceptions.Setup(r => r.GetBySeriesUids(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new List<CalendarEventException>
            {
                new() { SeriesUid = "legacy", OccurrenceDate = Start.AddDays(1), EventTypeId = Focus.Id }
            });
            harness.EventTypes.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => new[] { Work, Focus, Travel, Personal }.Where(t => ids.Contains(t.Id)).ToList());

            var result = await harness.RunAsync();

            string TypeOf(string subject, int day)
            {
                var occurrence = result.Single(e => e.Subject == subject && e.StartDate.Date == Start.AddDays(day).Date);
                return $"{occurrence.EventTypeName} {occurrence.EventColor}";
            }

            TypeOf("Legacy", 0).ShouldBe("Work #111111");
            TypeOf("Legacy", 1).ShouldBe("Focus #222222"); // single-occurrence edit changed the type
            TypeOf("Legacy", 2).ShouldBe("Work #111111");
            TypeOf("Segmented", 0).ShouldBe("Travel #333333"); // the segment's type, not the root's
            TypeOf("Segmented", 1).ShouldBe("Travel #333333");
            TypeOf("Segmented", 2).ShouldBe("Travel #333333");
            TypeOf("One-off", 0).ShouldBe("Personal #444444");

            // One batched lookup, only for the ids the projection left unresolved.
            harness.EventTypes.Verify(r => r.GetByIdsAsync(It.Is<List<int>>(ids =>
                ids.OrderBy(i => i).SequenceEqual(new[] { Work.Id, Focus.Id, Travel.Id }))), Times.Once);
        }

        [Fact]
        public async Task WhenEveryEventAlreadyHasItsType_NoLookupIsMade()
        {
            var harness = new Harness();
            var oneOff = new CalendarEvent
            {
                Id = 3, UserId = Owner, SeriesUid = "one-off", IsRecurring = false, Subject = "One-off",
                StartDate = Start, EndDate = Start.AddHours(1), EventTypeId = Personal.Id, EventType = Personal
            };
            harness.Events.Setup(r => r.GetByUserIdAsync(Owner, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .ReturnsAsync(new List<CalendarEvent> { oneOff });
            harness.Exceptions.Setup(r => r.GetBySeriesUids(It.IsAny<IEnumerable<string>>())).ReturnsAsync(new List<CalendarEventException>());

            var result = await harness.RunAsync();

            result.Single().EventTypeName.ShouldBe("Personal");
            result.Single().EventColor.ShouldBe("#444444");
            harness.EventTypes.Verify(r => r.GetByIdsAsync(It.IsAny<List<int>>()), Times.Never);
        }
    }
}
