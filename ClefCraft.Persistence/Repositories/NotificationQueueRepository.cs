using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Repositories
{
    public class NotificationQueueRepository
        : GenericRepository<NotificationQueue>,
          INotificationQueueRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationQueueRepository(
            ClefCraftDatabaseContext context, IUnitOfWork unitOfWork)
            : base(context)
        {
            _unitOfWork = unitOfWork;
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
            await _unitOfWork.SaveChangesAsync();

        }
    }
}