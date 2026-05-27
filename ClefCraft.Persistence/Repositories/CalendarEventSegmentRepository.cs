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
    public class CalendarEventSegmentRepository
        : GenericRepository<CalendarEventSegment>,
          ICalendarEventSegmentRepository
    {
        public CalendarEventSegmentRepository(
            ClefCraftDatabaseContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Returns all segments belonging to a recurrence series.
        /// </summary>
        public async Task<List<CalendarEventSegment>> GetBySeriesUidAsync(
            string seriesUid)
        {
            return await _context.Set<CalendarEventSegment>()
                .Include(x => x.RecurrenceSeries)
                .Where(x => x.RecurrenceSeries.SeriesUid == seriesUid)
                .OrderBy(x => x.EffectiveFrom)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Returns the segment active at a specific date.
        /// Uses [EffectiveFrom, EffectiveTo) interval semantics.
        /// </summary>
        public async Task<CalendarEventSegment?> GetActiveSegmentAsync(
            int recurrenceSeriesId,
            DateTimeOffset date)
        {
            return await _context.Set<CalendarEventSegment>()
                .Where(x =>
                    x.RecurrenceSeriesId == recurrenceSeriesId &&
                    x.EffectiveFrom <= date &&
                    (x.EffectiveTo == null || date < x.EffectiveTo))
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefaultAsync();
        }
    }
}
