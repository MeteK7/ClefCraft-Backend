using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.Calendar
{
    /// <summary>
    /// Repository for per-occurrence overrides and cancellations.
    ///
    /// NOTE: The lookup key is always (SeriesUid + OccurrenceDate).
    /// CalendarEventId is intentionally NOT used as a key here because
    /// the physical CalendarEvent row may be superseded by a segment split
    /// while the SeriesUid remains the stable series identity.
    /// </summary>
    public interface ICalendarEventExceptionRepository
    {
        /// <summary>
        /// Returns the exception for a specific occurrence in a series, or null.
        /// </summary>
        Task<CalendarEventException?> GetBySeriesAndDate(
            string seriesUid,
            DateTimeOffset occurrenceDate);

        /// <summary>
        /// Returns all exceptions for a single series.
        /// Used by the projection service during segment expansion.
        /// </summary>
        Task<List<CalendarEventException>> GetBySeriesUid(string seriesUid);

        /// <summary>
        /// Batch version — returns exceptions for multiple series in one query.
        /// Used by the legacy EventExpansionService.
        /// </summary>
        Task<List<CalendarEventException>> GetBySeriesUids(
            IEnumerable<string> seriesUids);

        /// <summary>
        /// Inserts or updates a single exception record.
        /// Keyed on (SeriesUid, OccurrenceDate).
        /// </summary>
        Task UpsertAsync(CalendarEventException exception);

        /// <summary>
        /// Deletes all exceptions whose OccurrenceDate is >= fromDate for a series.
        /// Called by UpdateFromOccurrence ("this and following") so that future
        /// per-occurrence overrides do not bleed into the newly created segment.
        /// </summary>
        Task DeleteFromDateAsync(string seriesUid, DateTimeOffset fromDate);

        /// <summary>
        /// Deletes ALL exceptions for a series.
        /// Called by UpdateSeriesOverrideAll so that stale per-occurrence
        /// overrides do not ghost into the freshly overridden series definition.
        /// </summary>
        Task DeleteAllForSeriesAsync(string seriesUid);
    }
}