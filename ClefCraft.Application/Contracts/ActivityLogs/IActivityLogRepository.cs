using ClefCraft.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.ActivityLogs
{
    // Deliberately not IGenericRepository<ActivityLog>: ActivityLog rows are written only via
    // ClefCraftDatabaseContext.SaveChangesAsync's automatic audit trail (or IActivityLogger for
    // semantic entries) — never through a repository — so this interface is read-only by design.
    public interface IActivityLogRepository
    {
        Task<List<ActivityLog>> GetByEntityAsync(string entityType, int entityId, int skip, int take);
        Task<int> CountByEntityAsync(string entityType, int entityId);

        // Merges multiple (EntityType, EntityIds) criteria (e.g. Calendar History's
        // CalendarEvent/CalendarEventSegment/CalendarEventException sources) into a single
        // timestamp-ordered, paged result at the database level via UNION ALL, instead of
        // fetching every source unpaged and paging in memory.
        Task<(List<ActivityLog> Items, int TotalCount)> GetMergedPagedAsync(
            IReadOnlyList<(string EntityType, IEnumerable<int> EntityIds)> criteria,
            int skip,
            int take);
    }
}
