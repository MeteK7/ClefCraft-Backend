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
    }
}
