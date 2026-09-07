using ClefCraft.Domain;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface ICalendarEventCollaboratorRepository : IGenericRepository<CalendarEventCollaborator>
    {
        Task<bool> IsCollaboratorAsync(int calendarEventId, string userId);
        Task<List<CalendarEventCollaborator>> GetByEventIdAsync(int calendarEventId);
        Task RemoveAsync(int calendarEventId, string userId);
    }
}
