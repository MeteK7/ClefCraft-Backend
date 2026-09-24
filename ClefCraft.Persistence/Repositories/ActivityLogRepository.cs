using ClefCraft.Application.Contracts.ActivityLogs;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Repositories
{
    public class ActivityLogRepository : IActivityLogRepository
    {
        private readonly ClefCraftDatabaseContext _context;

        public ActivityLogRepository(ClefCraftDatabaseContext context)
        {
            _context = context;
        }

        // Filter shape matches AIDataRepository.GetEventLogs, which already queries ActivityLogs
        // the same way — hits the (EntityType, EntityId) composite index defined in
        // ActivityLogConfiguration.
        public async Task<List<ActivityLog>> GetByEntityAsync(string entityType, int entityId, int skip, int take)
        {
            return await _context.ActivityLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.Timestamp)
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountByEntityAsync(string entityType, int entityId)
        {
            return await _context.ActivityLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .CountAsync();
        }

        public async Task<(List<ActivityLog> Items, int TotalCount)> GetMergedPagedAsync(
            IReadOnlyList<(string EntityType, IEnumerable<int> EntityIds)> criteria,
            int skip,
            int take)
        {
            IQueryable<ActivityLog>? merged = null;

            foreach (var (entityType, entityIds) in criteria)
            {
                var ids = entityIds as ICollection<int> ?? entityIds.ToList();
                if (ids.Count == 0) continue;

                var source = _context.ActivityLogs
                    .Where(l => l.EntityType == entityType && ids.Contains(l.EntityId));

                // Concat (not Union) since each criterion targets a distinct EntityType, so the
                // sets can never overlap — this translates to a plain UNION ALL rather than a
                // UNION with a redundant de-dup pass.
                merged = merged == null ? source : merged.Concat(source);
            }

            if (merged == null)
                return (new List<ActivityLog>(), 0);

            var totalCount = await merged.CountAsync();

            var items = await merged
                .OrderByDescending(l => l.Timestamp)
                .AsNoTracking()
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
