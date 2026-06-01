using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface IReminderSchedulerService
    {
        Task ScheduleAsync(
            CalendarEvent calendarEvent,
            CancellationToken cancellationToken);
    }
}
