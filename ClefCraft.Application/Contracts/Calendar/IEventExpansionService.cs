using ClefCraft.Application.Features.Calendar.Queries;
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
        Task<List<CalendarEventInstanceDto>> ExpandAsync(
            List<CalendarEvent> events,
            DateTimeOffset rangeStart,
            DateTimeOffset rangeEnd);
    }
}