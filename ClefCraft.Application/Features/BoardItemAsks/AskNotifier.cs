using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Models.Notifications;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    public enum AskEvent
    {
        Requested,
        Changed,
        Nudged,
        Accepted,
        Declined,
        Completed,
        Reopened,
        Cancelled,

        /// <summary>The requester or recipient replied; only the other one is told.</summary>
        Replied
    }

    /// <summary>One notification to send about an ask, once the change is saved.</summary>
    /// <param name="Audience">
    /// Who to tell. When null: the recipient for a nudge, otherwise the recipient and the requester.
    /// The actor and anyone who isn't a current member of the board are always left out.
    /// </param>
    /// <param name="Reply">The reply this event carries (Replied, or Completed with an answer), once saved.</param>
    public sealed record AskNotice(
        BoardItemAsk Ask,
        int BoardId,
        string ItemTitle,
        AskEvent Event,
        string? Reason = null,
        IReadOnlyCollection<string>? Audience = null,
        BoardItemAskReply? Reply = null);

    // Sends ReceiveAsk notifications. Called only after the save that made the change succeeded,
    // so nobody is told about a change that didn't happen.
    public static class AskNotifier
    {
        public static async Task SendAsync(
            IReadOnlyCollection<AskNotice> notices,
            string actorUserId,
            INotificationHubService notificationHubService,
            IBoardMemberRepository boardMemberRepository,
            IUserService userService,
            CancellationToken cancellationToken)
        {
            if (notices.Count == 0)
                return;

            var actor = await userService.GetUser(actorUserId);
            var actorFullName = actor != null ? $"{actor.Firstname} {actor.Lastname}" : "Unknown";

            // Membership doesn't change between notices of one request; look each user up once.
            var membership = new Dictionary<(int BoardId, string UserId), bool>();

            foreach (var notice in notices)
            {
                var audience = notice.Audience ?? DefaultAudience(notice);

                foreach (var userId in audience.Where(id => !string.IsNullOrEmpty(id) && id != actorUserId).Distinct())
                {
                    var key = (notice.BoardId, userId);
                    if (!membership.TryGetValue(key, out var isMember))
                    {
                        isMember = await boardMemberRepository.IsMemberAsync(notice.BoardId, userId);
                        membership[key] = isMember;
                    }

                    if (!isMember)
                        continue;

                    await notificationHubService.SendAskEventAsync(userId, ToPayload(notice, actorFullName), cancellationToken);
                }
            }
        }

        private static IReadOnlyCollection<string> DefaultAudience(AskNotice notice) =>
            notice.Event == AskEvent.Nudged
                ? new[] { notice.Ask.RecipientUserId }
                : new[] { notice.Ask.RecipientUserId, notice.Ask.RequestedByUserId };

        private static AskNotification ToPayload(AskNotice notice, string actorFullName) => new()
        {
            AskId = notice.Ask.Id,
            BoardItemId = notice.Ask.BoardItemId,
            BoardId = notice.BoardId,
            ItemTitle = notice.ItemTitle,
            Event = notice.Event.ToString(),
            ActorFullName = actorFullName,
            Note = notice.Ask.Note,
            Reason = notice.Reason,
            ActedOnBehalf = notice.Event == AskEvent.Completed
                && notice.Ask.State == BoardItemAskState.Completed
                && notice.Ask.CompletedByUserId != null
                && notice.Ask.CompletedByUserId != notice.Ask.RecipientUserId,
            ReplyId = notice.Reply?.Id,
            ReplyExcerpt = notice.Reply != null ? AskReplies.Excerpt(notice.Reply.Body) : null
        };
    }
}
