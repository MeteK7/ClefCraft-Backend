using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface ICalendarEventExceptionRepository : IGenericRepository<CalendarEventException>
    {
        Task<CalendarEventException?> GetByEventAndDate(int eventId, DateTimeOffset date);
        Task UpsertAsync(CalendarEventException exception);
        Task<List<CalendarEventException>> GetByEventIdsAsync(List<int> eventIds);
    }
}
