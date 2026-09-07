using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;

namespace ClefCraft.Application.Features.Comments
{
    // CalendarEvent-only mention policy, applied by both CreateComment and UpdateComment.
    // BoardItem mentions never touch this — board access is already implicit via membership,
    // so mentioning a board member is always just a ping, never a grant.
    //
    // For CalendarEvent: mentioning the owner or an existing collaborator is always a valid
    // ping. Mentioning someone new only takes effect — and only grants CalendarEventCollaborator
    // access — when the requester IS the event's owner. A non-owner's mention of an outsider is
    // silently dropped before any CommentMention row or notification is created, so a granted
    // collaborator can never use a reply to pull a stranger into someone else's private event.
    public static class CalendarMentionAccess
    {
        public static async Task<(List<string> ValidMentionedUserIds, List<string> NewlyGrantedUserIds)> ResolveAsync(
            string entityType,
            int entityId,
            string requesterId,
            List<string> mentionedUserIds,
            ICalendarEventRepository calendarEventRepository,
            ICalendarAccessService calendarAccessService,
            ICalendarEventCollaboratorRepository collaboratorRepository)
        {
            if (entityType != "CalendarEvent" || mentionedUserIds.Count == 0)
                return (mentionedUserIds, new List<string>());

            var calendarEvent = await calendarEventRepository.GetByIdReadOnlyAsync(entityId);
            if (calendarEvent == null)
                return (new List<string>(), new List<string>());

            // No-ops (returns empty) if requesterId isn't the owner — the defensive second
            // layer behind the filtering below.
            var newlyGranted = await calendarAccessService.GrantCollaboratorAccessAsync(entityId, requesterId, mentionedUserIds);

            var validIds = new List<string>();
            foreach (var id in mentionedUserIds)
            {
                var alreadyHasAccess = id == calendarEvent.UserId
                    || newlyGranted.Contains(id)
                    || await collaboratorRepository.IsCollaboratorAsync(entityId, id);

                if (alreadyHasAccess)
                    validIds.Add(id);
            }

            return (validIds, newlyGranted);
        }
    }
}
