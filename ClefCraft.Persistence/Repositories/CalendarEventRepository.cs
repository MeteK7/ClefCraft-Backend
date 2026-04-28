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

        /// <summary>
        /// Returns events that START within [windowStart, windowEnd] OR are recurring
        /// (recurring events need the window applied after expansion).
        /// Filters by the dedicated UserId column, not the audit CreatedBy column.
        /// </summary>
        public async Task<List<CalendarEvent>> GetByUserIdAsync(
            string userId,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd)
        {
            return await _context.CalendarEvents
                .Where(e => e.UserId == userId &&                      // ← use UserId, not CreatedBy
                            (e.IsRecurring ||                          // recurring: expand in memory
                             (e.StartDate >= windowStart &&            // one-off: filter in DB
                              e.StartDate <= windowEnd)))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<CalendarEvent>> GetWorkHistoryByItemIdAsync(int itemId)
        {
            return await _context.CalendarEvents
                .Where(e => e.LinkedBoardItemId != null && e.LinkedBoardItemId == itemId)
                .OrderByDescending(e => e.StartDate)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
