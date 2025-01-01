using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface ICalendarEventRepository : IGenericRepository<CalendarEvent>
    {
        Task<CalendarEvent> GetByDateAsync(DateTime date);
        Task<List<CalendarEvent>> GetByUserIdAsync(string userId);
    }
}
