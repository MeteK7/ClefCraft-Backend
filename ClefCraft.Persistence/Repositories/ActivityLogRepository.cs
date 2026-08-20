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
    }
}
