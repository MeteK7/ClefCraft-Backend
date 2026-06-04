using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface ICalendarReminderRepository : IGenericRepository<CalendarReminder>
    {
        Task<List<CalendarReminder>> GetByEventIdAsync(
            int eventId);
        Task<List<CalendarReminder>> GetByEventIdsAsync(List<int> eventIds);
    }
}
