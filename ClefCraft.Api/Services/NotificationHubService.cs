using ClefCraft.Api.Hubs;
using ClefCraft.Application.Contracts.Calendar;
using Microsoft.AspNetCore.SignalR;

namespace ClefCraft.Api.Services
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationHubService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendReminderToUserAsync(string userId, int eventId, string message, CancellationToken cancellationToken)
        {
            await _hubContext.Clients
                .User(userId)
                .SendAsync("ReceiveReminder", new { eventId, message }, cancellationToken);
        }
    }
}