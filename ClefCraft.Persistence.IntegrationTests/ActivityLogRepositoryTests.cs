using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Regression coverage for the in-memory-pagination-across-merged-sources issue:
    // GetCalendarEventActivityHandler used to fetch every CalendarEvent/CalendarEventSegment/
    // CalendarEventException activity log unpaged, then merge/sort/skip/take in memory,
    // discarding most of what it fetched. GetMergedPagedAsync pushes the merge (via UNION ALL),
    // ordering, and paging down to the database instead, matching how the single-source
    // GetByEntityAsync/CountByEntityAsync pair already behaves. These tests exercise the
    // repository directly (not through a mock) so the actual skip/take/order/count arithmetic
    // is verified, not just handler orchestration.
    public class ActivityLogRepositoryTests
    {
        private static async Task<ClefCraftDatabaseContext> SeedAsync(params ActivityLog[] logs)
        {
            var context = DatabaseContextFactory.CreateContext();
            await context.ActivityLogs.AddRangeAsync(logs);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            return context;
        }

        [Fact]
        public async Task GetMergedPagedAsync_MergesMultipleEntityTypes_OrderedByTimestampDescending()
        {
            var now = DateTime.UtcNow;

            var eventLog = new ActivityLog { EntityType = "CalendarEvent", EntityId = 1, ActionType = "CREATED", UserId = "u1", Timestamp = now.AddMinutes(-3) };
            var segmentLog = new ActivityLog { EntityType = "CalendarEventSegment", EntityId = 10, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-1) };
            var exceptionLog = new ActivityLog { EntityType = "CalendarEventException", EntityId = 20, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-2) };

            var context = await SeedAsync(eventLog, segmentLog, exceptionLog);
            var repository = new ActivityLogRepository(context);

            var criteria = new List<(string EntityType, IEnumerable<int> EntityIds)>
            {
                ("CalendarEvent", new[] { 1 }),
                ("CalendarEventSegment", new[] { 10 }),
                ("CalendarEventException", new[] { 20 }),
            };

            var (items, totalCount) = await repository.GetMergedPagedAsync(criteria, skip: 0, take: 10);

            totalCount.ShouldBe(3);
            items.Select(l => l.EntityType).ShouldBe(new[] { "CalendarEventSegment", "CalendarEventException", "CalendarEvent" });
        }

        [Fact]
        public async Task GetMergedPagedAsync_PageSizeAndPageNumber_ReturnsCorrectSlice_TotalCountReflectsFullMergedSet()
        {
            var now = DateTime.UtcNow;

            // 5 entries total, spread across two entity types, all part of the same merge.
            var logs = new[]
            {
                new ActivityLog { EntityType = "CalendarEvent", EntityId = 1, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-1) },
                new ActivityLog { EntityType = "CalendarEventSegment", EntityId = 10, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-2) },
                new ActivityLog { EntityType = "CalendarEvent", EntityId = 1, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-3) },
                new ActivityLog { EntityType = "CalendarEventSegment", EntityId = 10, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-4) },
                new ActivityLog { EntityType = "CalendarEvent", EntityId = 1, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-5) },
            };

            var context = await SeedAsync(logs);
            var repository = new ActivityLogRepository(context);

            var criteria = new List<(string EntityType, IEnumerable<int> EntityIds)>
            {
                ("CalendarEvent", new[] { 1 }),
                ("CalendarEventSegment", new[] { 10 }),
            };

            // Page 2 of size 2 -> should skip the 2 most recent, return the next 2.
            var (page, totalCount) = await repository.GetMergedPagedAsync(criteria, skip: 2, take: 2);

            totalCount.ShouldBe(5);
            page.Count.ShouldBe(2);
            page.Select(l => l.Timestamp).ShouldBe(new[] { now.AddMinutes(-3), now.AddMinutes(-4) });
        }

        [Fact]
        public async Task GetMergedPagedAsync_LastPagePartial_ReturnsOnlyRemainingItems()
        {
            var now = DateTime.UtcNow;
            var logs = Enumerable.Range(1, 5)
                .Select(i => new ActivityLog { EntityType = "CalendarEvent", EntityId = 1, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-i) })
                .ToArray();

            var context = await SeedAsync(logs);
            var repository = new ActivityLogRepository(context);

            var criteria = new List<(string EntityType, IEnumerable<int> EntityIds)>
            {
                ("CalendarEvent", new[] { 1 }),
            };

            // Page 3 of size 2 over 5 total items -> only 1 item remains.
            var (page, totalCount) = await repository.GetMergedPagedAsync(criteria, skip: 4, take: 2);

            totalCount.ShouldBe(5);
            page.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetMergedPagedAsync_DoesNotIncludeLogsForUnrelatedEntityIdsOrTypes()
        {
            var now = DateTime.UtcNow;

            var relevant = new ActivityLog { EntityType = "CalendarEvent", EntityId = 1, ActionType = "CREATED", UserId = "u1", Timestamp = now };
            var unrelatedSameType = new ActivityLog { EntityType = "CalendarEvent", EntityId = 999, ActionType = "CREATED", UserId = "u1", Timestamp = now };
            var unrelatedOtherType = new ActivityLog { EntityType = "BoardItem", EntityId = 1, ActionType = "CREATED", UserId = "u1", Timestamp = now };

            var context = await SeedAsync(relevant, unrelatedSameType, unrelatedOtherType);
            var repository = new ActivityLogRepository(context);

            var criteria = new List<(string EntityType, IEnumerable<int> EntityIds)>
            {
                ("CalendarEvent", new[] { 1 }),
            };

            var (items, totalCount) = await repository.GetMergedPagedAsync(criteria, skip: 0, take: 10);

            totalCount.ShouldBe(1);
            items.Single().Id.ShouldBe(relevant.Id);
        }

        [Fact]
        public async Task GetMergedPagedAsync_AllCriteriaHaveEmptyIdSets_ReturnsEmptyResultWithZeroCount()
        {
            var context = await SeedAsync(
                new ActivityLog { EntityType = "CalendarEvent", EntityId = 1, ActionType = "CREATED", UserId = "u1", Timestamp = DateTime.UtcNow });
            var repository = new ActivityLogRepository(context);

            // Mirrors the handler's behavior when a recurring event has no segments/exceptions yet.
            var criteria = new List<(string EntityType, IEnumerable<int> EntityIds)>
            {
                ("CalendarEventSegment", Array.Empty<int>()),
                ("CalendarEventException", Array.Empty<int>()),
            };

            var (items, totalCount) = await repository.GetMergedPagedAsync(criteria, skip: 0, take: 10);

            totalCount.ShouldBe(0);
            items.ShouldBeEmpty();
        }
    }
}
