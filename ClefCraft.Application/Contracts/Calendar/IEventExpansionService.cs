using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface IEventExpansionService
    {
        Task<List<CalendarEvent>> ExpandAsync(
            List<CalendarEvent> events,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd);
    }
}
