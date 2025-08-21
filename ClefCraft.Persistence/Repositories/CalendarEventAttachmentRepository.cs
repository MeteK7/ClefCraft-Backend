using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Repositories
{
    public class CalendarEventAttachmentRepository : GenericRepository<CalendarEventAttachment>, ICalendarEventAttachmentRepository
    {
        private readonly ClefCraftDatabaseContext _context;

        public CalendarEventAttachmentRepository(ClefCraftDatabaseContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<CalendarEventAttachment>> GetByEventIdAsync(int eventId)
        {
            return await _context.CalendarEventAttachments
                .Where(a => a.CalendarEventId == eventId)
                .ToListAsync();
        }
    }
}
