using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClefCraft.Api.Hubs
{
    // Notifications are addressed with Clients.User(uid), so an anonymous connection can never
    // receive anything. Rejecting it makes a client that connects before login fail loudly
    // instead of silently missing every mention/reminder.
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Notification hub connected: user {UserId}", Context.UserIdentifier);

            await base.OnConnectedAsync();
        }
    }
}
