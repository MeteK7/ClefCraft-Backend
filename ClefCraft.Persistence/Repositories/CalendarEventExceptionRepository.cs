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

        public async Task<CalendarEventException?> GetBySeriesAndDate(string seriesUid, DateTimeOffset date)
        {
            return await _context.CalendarEventExceptions
                .FirstOrDefaultAsync(x =>
                    x.SeriesUid == seriesUid &&
                    x.OccurrenceDate == date);
        }

        public async Task<List<CalendarEventException>> GetBySeriesUid(string seriesUid)
        {
            return await _context.CalendarEventExceptions
                .Where(x => x.SeriesUid == seriesUid)
                .ToListAsync();
        }

        public async Task<List<CalendarEventException>> GetBySeriesUids(List<string> seriesUids)
        {
            return await _context.CalendarEventExceptions
                .Where(x => seriesUids.Contains(x.SeriesUid))
                .ToListAsync();
        }

        public async Task UpsertAsync(CalendarEventException exception)
        {
            var existing = await GetBySeriesAndDate(exception.SeriesUid, exception.OccurrenceDate);

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
                existing.Location = exception.Location;
                existing.EventTypeId = exception.EventTypeId;

                existing.DateModified = DateTime.UtcNow;
            }
        }
    }
}