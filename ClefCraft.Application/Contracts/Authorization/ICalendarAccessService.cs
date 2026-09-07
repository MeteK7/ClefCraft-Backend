namespace ClefCraft.Application.Contracts.Authorization
{
    /// <summary>
    /// Resource-level ownership checks for the Calendar domain.
    /// CalendarEvent.UserId is treated as the single source of truth for
    /// ownership — series/segment/exception records reached via SeriesUid
    /// are authorized by resolving back to the owning CalendarEvent, not by
    /// trusting RecurrenceSeries.UserId (which is set independently and can drift).
    /// </summary>
    public interface ICalendarAccessService
    {
        Task EnsureEventOwnedByUserAsync(int eventId, string userId);

        Task EnsureSeriesOwnedByUserAsync(string seriesUid, string userId);

        Task EnsureAttachmentOwnedByUserAsync(int attachmentId, string userId);

        /// <summary>
        /// Read-only access check — governs both viewing the event's own details and
        /// viewing/posting comments on it. Deliberately independent of LinkedBoardItemId:
        /// grants access to the event's owner, or to anyone explicitly granted
        /// CalendarEventCollaborator access on this specific event. There is no implicit
        /// "shares a board" carve-out — a private event stays private until the owner
        /// explicitly shares it (see GrantCollaboratorAccessAsync). Never grants mutation
        /// rights — editing/deleting still requires EnsureEventOwnedByUserAsync.
        /// </summary>
        Task EnsureCanAccessEventAsync(int eventId, string userId);

        /// <summary>
        /// Read-only variant of EnsureAttachmentOwnedByUserAsync — owner or collaborator can
        /// download an attachment; upload/delete stay owner-only via EnsureAttachmentOwnedByUserAsync.
        /// </summary>
        Task EnsureCanAccessAttachmentAsync(int attachmentId, string userId);

        /// <summary>
        /// The only way a CalendarEvent gains collaborators: called from CreateComment/
        /// UpdateComment when the requester mentions someone new. Only the event's owner can
        /// grant — a no-op (returns an empty list, no exception) if the requester isn't the
        /// owner, since by the time this runs the caller has already filtered non-owner
        /// mentions down to existing participants; this is the defensive second layer.
        /// Already-granted users are skipped (idempotent). Returns the userIds that were
        /// actually newly granted, so the caller knows who to notify with "shared with you"
        /// framing versus a plain mention ping.
        /// </summary>
        Task<List<string>> GrantCollaboratorAccessAsync(int eventId, string granterId, IEnumerable<string> targetUserIds);
    }
}
