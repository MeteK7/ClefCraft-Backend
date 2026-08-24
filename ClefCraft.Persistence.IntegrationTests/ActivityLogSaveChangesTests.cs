using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Bypasses the broken shared fixture in ClefCraftDatabaseContextTests (its constructor never
    // supplies ClefCraftDatabaseContext's required IUserService, so its tests cannot run). This
    // class builds its own context per test with a mocked IUserService instead, specifically to
    // cover the automatic ActivityLog audit trail SaveChangesAsync produces, since History reads
    // directly from that table and depends on it being correct.
    public class ActivityLogSaveChangesTests
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
        public async Task Save_SingleCreatedEntity_GetsCorrectEntityId()
        {
            var context = CreateContext();

            var board = new Board { Title = "Board A" };
            await context.Boards.AddAsync(board);
            await context.SaveChangesAsync();

            var log = context.ActivityLogs.Single(l => l.EntityType == "Board" && l.ActionType == "CREATED");
            log.EntityId.ShouldBe(board.Id);
        }

        // Regression test for the EntityId backfill fix: previously, creating two entities of the
        // same CLR type in one SaveChangesAsync call caused both CREATED log rows to be assigned
        // the same EntityId (matched by type name via FirstOrDefault instead of by the specific
        // EntityEntry). CreateCalendarEventCommandHandler triggers exactly this today when an
        // event is created with 2+ reminders.
        [Fact]
        public async Task Save_MultipleSameTypeEntitiesCreatedInOneBatch_EachGetsOwnCorrectEntityId()
        {
            var context = CreateContext();

            var calendarEvent = new CalendarEvent
            {
                Subject = "Event 1",
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddHours(1),
                UserId = "test-user"
            };
            await context.CalendarEvents.AddAsync(calendarEvent);
            await context.SaveChangesAsync();

            var reminderA = new CalendarReminder { CalendarEventId = calendarEvent.Id, MinutesBeforeStart = 15, IsEnabled = true };
            var reminderB = new CalendarReminder { CalendarEventId = calendarEvent.Id, MinutesBeforeStart = 60, IsEnabled = true };
            await context.CalendarReminders.AddRangeAsync(reminderA, reminderB);
            await context.SaveChangesAsync();

            var reminderLogs = context.ActivityLogs
                .Where(l => l.EntityType == "CalendarReminder" && l.ActionType == "CREATED")
                .ToList();

            reminderLogs.Count.ShouldBe(2);
            reminderLogs.Select(l => l.EntityId).Distinct().Count().ShouldBe(2);
            reminderLogs.ShouldContain(l => l.EntityId == reminderA.Id);
            reminderLogs.ShouldContain(l => l.EntityId == reminderB.Id);
        }

        [Fact]
        public async Task Save_UpdatedEntity_DiffExcludesAuditFieldsAndCapturesRealChanges()
        {
            var context = CreateContext();

            var board = new Board { Title = "Original Title" };
            await context.Boards.AddAsync(board);
            await context.SaveChangesAsync();

            board.Title = "Updated Title";
            await context.SaveChangesAsync();

            var log = context.ActivityLogs.Single(l => l.EntityType == "Board" && l.ActionType == "UPDATED");
            log.MetadataJson.ShouldNotBeNull();
            log.MetadataJson.ShouldContain("Title");
            log.MetadataJson.ShouldNotContain("DateModified");
            log.MetadataJson.ShouldNotContain("ModifiedBy");
        }

        [Fact]
        public async Task Save_ModifiedEntityWithNoRealChanges_ProducesNoLogRow()
        {
            var context = CreateContext();

            var board = new Board { Title = "Same Title" };
            await context.Boards.AddAsync(board);
            await context.SaveChangesAsync();

            var countAfterCreate = context.ActivityLogs.Count();

            // Force a Modified state without actually changing any tracked property value.
            context.Entry(board).State = EntityState.Modified;
            await context.SaveChangesAsync();

            context.ActivityLogs.Count().ShouldBe(countAfterCreate);
        }

        // Calendar History depends on a CalendarEvent edit producing exactly one UPDATED row, not
        // several. UpdateCalendarEventCommandHandler used to also manually log EVENT_RESCHEDULED/
        // IMPORTANCE_CHANGED via IActivityLogger on top of this generic diff for the same edit —
        // that duplication was removed; this confirms the generic diff alone still captures every
        // changed field (reschedule + importance together) in one row.
        [Fact]
        public async Task Save_CalendarEventRescheduledAndImportanceChanged_ProducesSingleUpdatedRowWithBothFields()
        {
            var context = CreateContext();

            var start = DateTimeOffset.UtcNow;
            var calendarEvent = new CalendarEvent
            {
                Subject = "Planning",
                StartDate = start,
                EndDate = start.AddHours(1),
                Importance = ImportanceLevel.Normal,
                UserId = "test-user"
            };
            await context.CalendarEvents.AddAsync(calendarEvent);
            await context.SaveChangesAsync();

            calendarEvent.StartDate = start.AddDays(1);
            calendarEvent.EndDate = start.AddDays(1).AddHours(1);
            calendarEvent.Importance = ImportanceLevel.High;
            await context.SaveChangesAsync();

            var updatedLogs = context.ActivityLogs
                .Where(l => l.EntityType == "CalendarEvent" && l.EntityId == calendarEvent.Id && l.ActionType == "UPDATED")
                .ToList();

            updatedLogs.Count.ShouldBe(1);
            updatedLogs[0].MetadataJson.ShouldContain("StartDate");
            updatedLogs[0].MetadataJson.ShouldContain("EndDate");
            updatedLogs[0].MetadataJson.ShouldContain("Importance");
        }
    }
}
