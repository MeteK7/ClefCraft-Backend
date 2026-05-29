using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    public interface IRecurrenceSeriesRepository : IGenericRepository<RecurrenceSeries>
    {
        /// <summary>
        /// Retrieves the series — including its ordered segments — by the
        /// stable SeriesUid.  SeriesUid is shared between CalendarEvent and
        /// RecurrenceSeries; this is the primary cross-architecture bridge.
        /// </summary>
        Task<RecurrenceSeries?> GetBySeriesUidAsync(string seriesUid);

        Task<List<RecurrenceSeries>> GetByUserIdAsync(string userId);

        Task CreateAsync(RecurrenceSeries series);
    }
}