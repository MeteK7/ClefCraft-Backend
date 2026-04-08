using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Repositories
{
    public class AIDataRepository : IAIDataRepository
    {
        private readonly ClefCraftDatabaseContext _context;

        public AIDataRepository(ClefCraftDatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<ActivityLog>> GetEventLogs(List<int> eventIds)
        {
            return await _context.ActivityLogs
                .Where(l => l.EntityType == "CalendarEvent" && eventIds.Contains(l.EntityId))
                .ToListAsync();
        }

        public async Task<List<UserInteractionSignal>> GetEventSignals(List<int> eventIds)
        {
            return await _context.UserInteractionSignals
                .Where(s => s.EntityType == "CalendarEvent" && eventIds.Contains(s.EntityId))
                .ToListAsync();
        }

        public async Task<List<TaskLifecycle>> GetTaskLifecycles(List<int> boardItemIds)
        {
            return await _context.TaskLifecycles
                .Where(t => boardItemIds.Contains(t.BoardItemId))
                .ToListAsync();
        }
    }
}
