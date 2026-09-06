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

        Task SendCommentMentionAsync(
            string mentionedUserId,
            string entityType,
            int entityId,
            int commentId,
            string authorFullName,
            string excerpt,
            int? boardId,
            CancellationToken cancellationToken);
    }
}
