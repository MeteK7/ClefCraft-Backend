using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface INotificationQueueRepository
    {
        Task CreateAsync(
            NotificationQueue notification);

        Task<List<NotificationQueue>> GetPendingAsync(
            DateTimeOffset utcNow);

        Task UpdateAsync(
            NotificationQueue notification);
    }
}
