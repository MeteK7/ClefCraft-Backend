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
    public class CalendarEventExceptionRepository
        : GenericRepository<CalendarEventException>, ICalendarEventExceptionRepository
    {
        public CalendarEventExceptionRepository(ClefCraftDatabaseContext context)
            : base(context)
        {
        }

        public async Task<CalendarEventException?> GetByEventAndDate(
            int eventId,
            DateOnly date)
        {
            return await _context.CalendarEventExceptions
                .FirstOrDefaultAsync(x =>
                    x.CalendarEventId == eventId &&
                    x.OccurrenceDate == date);
        }

        public async Task<List<CalendarEventException>> GetByEventIdsAsync(
            List<int> eventIds)
        {
            return await _context.CalendarEventExceptions
                .Where(x => eventIds.Contains(x.CalendarEventId))
                .ToListAsync();
        }

        public async Task UpsertAsync(CalendarEventException exception)
        {
            var existing = await _context.CalendarEventExceptions
                .FirstOrDefaultAsync(x =>
                    x.CalendarEventId == exception.CalendarEventId &&
                    x.OccurrenceDate == exception.OccurrenceDate);

            if (existing == null)
            {
                exception.DateCreated = DateTime.UtcNow;
                exception.DateModified = DateTime.UtcNow;

                await _context.CalendarEventExceptions.AddAsync(exception);
            }
            else
            {
                existing.Subject = exception.Subject;
                existing.Comment = exception.Comment;
                existing.StartDate = exception.StartDate;
                existing.EndDate = exception.EndDate;
                existing.IsCancelled = exception.IsCancelled;

                existing.DateModified = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}