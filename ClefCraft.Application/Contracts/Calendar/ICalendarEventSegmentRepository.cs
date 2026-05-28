using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface ICalendarEventSegmentRepository
    {
        Task<CalendarEventSegment?> GetByIdAsync(int id);

        /// <summary>
        /// Returns all segments for a series ordered by EffectiveFrom.
        /// </summary>
        Task<List<CalendarEventSegment>> GetBySeriesUidAsync(string seriesUid);

        /// <summary>
        /// Returns the segment whose [EffectiveFrom, EffectiveTo) interval
        /// contains the given date.  Returns null if no segment covers it
        /// (e.g. the event existed only in the legacy system).
        /// </summary>
        Task<CalendarEventSegment?> GetActiveSegmentAsync(
            int recurrenceSeriesId,
            DateTimeOffset date);

        Task CreateAsync(CalendarEventSegment segment);
    }
}