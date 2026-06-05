using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface INotificationHubService
    {
        Task SendReminderToUserAsync(string userId, int eventId, string message, CancellationToken cancellationToken);
    }
}
