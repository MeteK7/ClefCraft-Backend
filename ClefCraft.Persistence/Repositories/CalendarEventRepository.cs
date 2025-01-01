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
    public class CalendarEventRepository : GenericRepository<CalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventRepository(ClefCraftDatabaseContext context) : base(context) { }

        public async Task<CalendarEvent> GetByDateAsync(DateTime date)
        {
            return await _context.CalendarEvents
                .Where(e => e.StartDate.Date == date.Date)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CalendarEvent>> GetByUserIdAsync(string userId)
        {
            return await _context.CalendarEvents
                .Where(e => e.CreatedBy == userId)
                .ToListAsync();
        }
    }

}
