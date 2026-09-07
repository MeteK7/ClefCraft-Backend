using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace ClefCraft.Persistence.Repositories
{
    public class CalendarEventCollaboratorRepository : GenericRepository<CalendarEventCollaborator>, ICalendarEventCollaboratorRepository
    {
        public CalendarEventCollaboratorRepository(ClefCraftDatabaseContext context) : base(context)
        {
        }

        public async Task<bool> IsCollaboratorAsync(int calendarEventId, string userId)
        {
            return await _context.CalendarEventCollaborators
                .AnyAsync(c => c.CalendarEventId == calendarEventId && c.UserId == userId);
        }

        public async Task<List<CalendarEventCollaborator>> GetByEventIdAsync(int calendarEventId)
        {
            return await _context.CalendarEventCollaborators
                .Where(c => c.CalendarEventId == calendarEventId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task RemoveAsync(int calendarEventId, string userId)
        {
            var existing = await _context.CalendarEventCollaborators
                .FirstOrDefaultAsync(c => c.CalendarEventId == calendarEventId && c.UserId == userId);

            if (existing != null)
                _context.CalendarEventCollaborators.Remove(existing);
        }
    }
}
