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
    public class CalendarReminderRepository
        : GenericRepository<CalendarReminder>,
          ICalendarReminderRepository
    {
        public CalendarReminderRepository(
            ClefCraftDatabaseContext context)
            : base(context)
        {
        }

        public async Task<List<CalendarReminder>>
            GetByEventIdAsync(int eventId)
        {
            return await _context.Set<CalendarReminder>()
                .Where(x => x.CalendarEventId == eventId)
                .ToListAsync();
        }

        public async Task<List<CalendarReminder>> GetByEventIdsAsync(List<int> eventIds)
        {
            return await _context.CalendarReminders
                .Where(r => eventIds.Contains(r.CalendarEventId))
                .ToListAsync();
        }
    }
}