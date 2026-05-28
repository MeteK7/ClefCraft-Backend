using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface ICalendarEventAttachmentRepository : IGenericRepository<CalendarEventAttachment>
    {
        Task<List<CalendarEventAttachment>> GetByEventIdAsync(int eventId);
    }
}
