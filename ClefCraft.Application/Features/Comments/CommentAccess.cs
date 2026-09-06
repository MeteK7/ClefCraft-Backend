using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Exceptions;

namespace ClefCraft.Application.Features.Comments
{
    // Shared entity-type dispatch used by every Comments query/command that needs to check
    // "can this user see/comment on this entity" — GetCommentsForEntity, GetMentionableUsers
    // and CreateComment all need the exact same switch, so it lives here once rather than
    // being copied three times (mirrors GetActivityLogForEntityHandler's inline switch, but
    // factored out since Comments has more than one call site for it).
    public static class CommentAccess
    {
        public static async Task EnsureCanAccessAsync(
            string entityType,
            int entityId,
            string userId,
            IBoardAccessService boardAccessService,
            ICalendarAccessService calendarAccessService)
        {
            switch (entityType)
            {
                case "BoardItem":
                    await boardAccessService.EnsureBoardItemOwnedByUserAsync(entityId, userId);
                    break;
                case "CalendarEvent":
                    await calendarAccessService.EnsureCanCommentOnEventAsync(entityId, userId);
                    break;
                default:
                    throw new ForbiddenAccessException();
            }
        }
    }
}
