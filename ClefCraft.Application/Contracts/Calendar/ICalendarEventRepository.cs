using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface ICalendarEventRepository : IGenericRepository<CalendarEvent>
    {
        Task<CalendarEvent> GetByDateAsync(DateTime date);
        Task<List<CalendarEvent>> GetByUserIdAsync(string userId, DateTimeOffset windowStart, DateTimeOffset windowEnd);
        Task<List<CalendarEvent>> GetWorkHistoryByItemIdAsync(int itemId);
    }
}
