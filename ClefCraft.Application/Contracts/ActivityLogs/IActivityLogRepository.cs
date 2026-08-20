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
    }
}
