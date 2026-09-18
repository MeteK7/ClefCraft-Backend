using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Bypasses the broken shared fixture in ClefCraftDatabaseContextTests (see
    // ActivityLogSaveChangesTests for why). Regression coverage for the Week-view
    // multi-day-event bug: GetByUserIdAsync used to filter non-recurring events by
    // StartDate alone, so an event that started in a previous window but was still
    // running (EndDate inside/after the requested window) was silently dropped
    // before it ever reached the overlap filter in GetCalendarEventsQueryHandler.
    public class CalendarEventRepositoryTests
    {
        private static ClefCraftDatabaseContext CreateContext(string userId = "test-user")
        {
            var options = new DbContextOptionsBuilder<ClefCraftDatabaseContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var userServiceMock = new Mock<IUserService>();
            userServiceMock.Setup(u => u.UserId).Returns(userId);

            return new ClefCraftDatabaseContext(options, userServiceMock.Object);
        }

        [Fact]
        public async Task GetByUserIdAsync_NonRecurringEventStartingBeforeWindowButStillOngoing_IsReturned()
        {
            var context = CreateContext();
            var repository = new CalendarEventRepository(context);

            // Starts in "week 1", ends in "week 2" — the query below only asks for week 2.
            var multiDayEvent = new CalendarEvent
            {
                Subject = "Conference",
                StartDate = new DateTimeOffset(2026, 8, 24, 9, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2026, 9, 2, 17, 0, 0, TimeSpan.Zero),
                AllDayEvent = true,
                UserId = "test-user"
            };
            await context.CalendarEvents.AddAsync(multiDayEvent);
            await context.SaveChangesAsync();

            var windowStart = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero);
            var windowEnd = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);

            var result = await repository.GetByUserIdAsync("test-user", windowStart, windowEnd);

            result.ShouldContain(e => e.Id == multiDayEvent.Id);
        }

        [Fact]
        public async Task GetByUserIdAsync_NonRecurringEventEntirelyOutsideWindow_IsExcluded()
        {
            var context = CreateContext();
            var repository = new CalendarEventRepository(context);

            // Fully before the window — no overlap at all.
            var pastEvent = new CalendarEvent
            {
                Subject = "Old meeting",
                StartDate = new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                UserId = "test-user"
            };

            // Fully after the window — no overlap at all.
            var futureEvent = new CalendarEvent
            {
                Subject = "Far future meeting",
                StartDate = new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero),
                UserId = "test-user"
            };

            await context.CalendarEvents.AddRangeAsync(pastEvent, futureEvent);
            await context.SaveChangesAsync();

            var windowStart = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero);
            var windowEnd = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);

            var result = await repository.GetByUserIdAsync("test-user", windowStart, windowEnd);

            result.ShouldNotContain(e => e.Id == pastEvent.Id);
            result.ShouldNotContain(e => e.Id == futureEvent.Id);
        }

        [Fact]
        public async Task GetByUserIdAsync_EventWithExplicitCollaboratorGrant_IsVisibleToTheCollaborator_NotToOthers()
        {
            // Calendar visibility is granted purely via an explicit CalendarEventCollaborator
            // row (see CalendarAccessService.EnsureCanAccessEventAsync/IsCollaboratorAsync) —
            // board co-membership and LinkedBoardItemId do NOT grant visibility on their own,
            // per GetByUserIdAsync's own doc comment. A "Mark as Worked" entry linked to a
            // shared board item stays private to its owner unless explicitly shared.
            var context = CreateContext();
            var repository = new CalendarEventRepository(context);

            var windowStart = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero);
            var windowEnd = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);

            var ownerEvent = new CalendarEvent
            {
                Subject = "Logged work",
                StartDate = windowStart.AddDays(1),
                EndDate = windowStart.AddDays(1).AddHours(1),
                UserId = "user-owner"
            };
            await context.CalendarEvents.AddAsync(ownerEvent);
            await context.SaveChangesAsync();

            await context.CalendarEventCollaborators.AddAsync(
                new CalendarEventCollaborator { CalendarEventId = ownerEvent.Id, UserId = "user-collaborator" });
            await context.SaveChangesAsync();

            var collaboratorView = await repository.GetByUserIdAsync("user-collaborator", windowStart, windowEnd);
            collaboratorView.ShouldContain(e => e.Id == ownerEvent.Id);

            var strangerView = await repository.GetByUserIdAsync("user-unrelated", windowStart, windowEnd);
            strangerView.ShouldNotContain(e => e.Id == ownerEvent.Id);
        }
    }
}
