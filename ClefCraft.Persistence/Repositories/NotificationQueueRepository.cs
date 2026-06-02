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
    public class NotificationQueueRepository
        : GenericRepository<NotificationQueue>,
          INotificationQueueRepository
    {
        public NotificationQueueRepository(
            ClefCraftDatabaseContext context)
            : base(context)
        {
        }

        public async Task<List<NotificationQueue>> GetPendingAsync(DateTimeOffset utcNow)
        {
            return await _context.Set<NotificationQueue>()
                .Where(x =>
                    !x.IsProcessed &&
                    x.ScheduledFor <= utcNow)
                .OrderBy(x => x.ScheduledFor)
                .ToListAsync();
        }

        public async Task DeletePendingByEventIdAsync(int calendarEventId)
        {
            var pending = await _context.NotificationQueues
                .Where(x =>
                    x.CalendarEventId == calendarEventId &&
                    !x.IsProcessed)
                .ToListAsync();

            _context.NotificationQueues.RemoveRange(pending);
        }
    }
}