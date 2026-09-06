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

        public async Task SendCommentMentionAsync(
            string mentionedUserId,
            string entityType,
            int entityId,
            int commentId,
            string authorFullName,
            string excerpt,
            int? boardId,
            CancellationToken cancellationToken)
        {
            await _hubContext.Clients
                .User(mentionedUserId)
                .SendAsync("ReceiveMention", new { entityType, entityId, commentId, authorFullName, excerpt, boardId }, cancellationToken);
        }
    }
}