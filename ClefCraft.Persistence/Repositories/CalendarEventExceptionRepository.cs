using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClefCraft.Persistence.Repositories
{
    public class CalendarEventExceptionRepository
        : GenericRepository<CalendarEventException>,
          ICalendarEventExceptionRepository
    {
        public CalendarEventExceptionRepository(
            ClefCraftDatabaseContext context)
            : base(context)
        {
        }

        public async Task<CalendarEventException?> GetBySeriesAndDate(
            string seriesUid,
            DateTimeOffset occurrenceDate)
        {
            var date = occurrenceDate.UtcDateTime.Date;

            return await _context.CalendarEventExceptions
                .FirstOrDefaultAsync(x =>
                    x.SeriesUid == seriesUid &&
                    x.OccurrenceDate >= date);
        }

        public async Task<List<CalendarEventException>> GetBySeriesUid(
            string seriesUid)
        {
            return await _context.CalendarEventExceptions
                .Where(x => x.SeriesUid == seriesUid)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<CalendarEventException>> GetBySeriesUids(
            IEnumerable<string> seriesUids)
        {
            var uids = seriesUids.ToList();

            if (!uids.Any())
                return new List<CalendarEventException>();

            return await _context.CalendarEventExceptions
                .Where(x => uids.Contains(x.SeriesUid))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task UpsertAsync(CalendarEventException exception)
        {
            var existing = await GetBySeriesAndDate(
                exception.SeriesUid,
                exception.OccurrenceDate);

            if (existing == null)
            {
                await _context.CalendarEventExceptions.AddAsync(exception);
            }
            else
            {
                // Map all mutable fields onto the tracked entity so EF
                // picks up the change without detach/attach gymnastics.
                existing.Subject = exception.Subject;
                existing.Comment = exception.Comment;
                existing.StartDate = exception.StartDate;
                existing.EndDate = exception.EndDate;
                existing.Location = exception.Location;
                existing.EventTypeId = exception.EventTypeId;
                existing.IsCancelled = exception.IsCancelled;

                _context.CalendarEventExceptions.Update(existing);
            }
        }

        public async Task DeleteFromDateAsync(
            string seriesUid,
            DateTimeOffset fromDate)
        {
            var fromUtc = fromDate.ToUniversalTime();

            var toDelete = await _context.CalendarEventExceptions
                .Where(x =>
                    x.SeriesUid == seriesUid &&
                    x.OccurrenceDate >= fromUtc)
                .ToListAsync();

            if (toDelete.Any())
                _context.CalendarEventExceptions.RemoveRange(toDelete);
        }

        public async Task DeleteAllForSeriesAsync(string seriesUid)
        {
            var toDelete = await _context.CalendarEventExceptions
                .Where(x => x.SeriesUid == seriesUid)
                .ToListAsync();

            if (toDelete.Any())
                _context.CalendarEventExceptions.RemoveRange(toDelete);
        }
    }
}