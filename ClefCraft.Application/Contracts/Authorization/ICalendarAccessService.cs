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
        /// Comment-access check only — deliberately independent of ownership and of
        /// LinkedBoardItemId. Grants access to the event's owner, or to anyone who
        /// shares at least one Board with the owner (the same "teammate" relationship
        /// GetUserFullNameHandler already uses). Does not affect the event's own
        /// visibility/edit rules.
        /// </summary>
        Task EnsureCanCommentOnEventAsync(int eventId, string userId);
    }
}
