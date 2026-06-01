using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface ICalendarReminderRepository
    {
        Task<List<CalendarReminder>> GetByEventIdAsync(
            int eventId);

        Task CreateAsync(
            CalendarReminder reminder);

        Task DeleteByEventIdAsync(
            int eventId);
    }
}
