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
    public class EventTypeRepository
        : GenericRepository<EventType>, IEventTypeRepository
    {
        public EventTypeRepository(ClefCraftDatabaseContext context)
            : base(context)
        {
        }

        public async Task<List<EventType>> GetByUserIdAsync(string userId)
        {
            return await _context.EventTypes
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
}
