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
        /// Also includes events the caller doesn't own but that are linked to a
        /// BoardItem on a board they're a member of (e.g. a teammate's "Mark as
        /// Worked" entry) — visible here, but editing/deleting one still requires
        /// actual ownership via ICalendarAccessService.
        /// </summary>
        public async Task<List<CalendarEvent>> GetByUserIdAsync(
            string userId,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd)
        {
            var memberBoardIds = _context.BoardMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.BoardId);

            return await _context.CalendarEvents
                .Include(e => e.EventType)
                .Where(e =>
                    (e.UserId == userId ||
                     (e.LinkedBoardItemId != null &&
                      _context.BoardItems.Any(bi =>
                          bi.Id == e.LinkedBoardItemId && memberBoardIds.Contains(bi.BoardId)))) &&
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

        public async Task<CalendarEvent?> GetBySeriesUidAsync(string seriesUid)
        {
            return await _context.CalendarEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.SeriesUid == seriesUid);
        }
    }
}
