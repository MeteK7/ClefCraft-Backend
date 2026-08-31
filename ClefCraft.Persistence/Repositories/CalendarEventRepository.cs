using ClefCraft.Application.Contracts.Calendar;
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
        /// Returns events that OVERLAP [windowStart, windowEnd] OR are recurring
        /// (recurring events need the window applied after expansion).
        /// Filters by the dedicated UserId column, not the audit CreatedBy column.
        /// </summary>
        public async Task<List<CalendarEvent>> GetByUserIdAsync(
            string userId,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd)
        {
            return await _context.CalendarEvents
                .Include(e => e.EventType)
                .Where(e => e.UserId == userId &&
                            (e.IsRecurring ||
                             (e.StartDate < windowEnd &&
                              e.EndDate > windowStart)))
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
