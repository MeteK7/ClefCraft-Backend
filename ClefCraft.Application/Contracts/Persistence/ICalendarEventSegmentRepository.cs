using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface ICalendarEventSegmentRepository
        : IGenericRepository<CalendarEventSegment>
    {
        Task<List<CalendarEventSegment>> GetBySeriesUidAsync(string seriesUid);

        Task<CalendarEventSegment?> GetActiveSegmentAsync(
            int recurrenceSeriesId,
            DateTimeOffset date);
    }
}
