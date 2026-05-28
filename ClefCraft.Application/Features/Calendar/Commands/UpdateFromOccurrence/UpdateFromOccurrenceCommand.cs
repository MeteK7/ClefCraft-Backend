using ClefCraft.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateFromOccurrence
{
    /// <summary>
    /// "This and following" edit.
    ///
    /// Splits the recurrence series at OccurrenceDate so that:
    ///   - All occurrences BEFORE OccurrenceDate remain unchanged (existing segment).
    ///   - All occurrences FROM OccurrenceDate onward use the new properties
    ///     supplied in this command (new segment).
    ///
    /// Any per-occurrence CalendarEventExceptions that fall on or after
    /// OccurrenceDate are deleted because they were overrides against the OLD
    /// series definition and are no longer meaningful after the split.
    /// Exceptions that predate OccurrenceDate are preserved.
    /// </summary>
    public class UpdateFromOccurrenceCommand : IRequest
    {
        /// <summary>
        /// Stable identity of the recurrence series being split.
        /// </summary>
        public string SeriesUid { get; set; }

        /// <summary>
        /// The occurrence at which the split begins ("this" occurrence).
        /// The new segment's EffectiveFrom is set to this value.
        /// </summary>
        public DateTimeOffset OccurrenceDate { get; set; }

        // ---------------------------------------------------------------
        // Properties for the NEW segment (from OccurrenceDate onward).
        // Nulls mean "inherit from the current active segment".
        // ---------------------------------------------------------------

        public string? Subject { get; set; }
        public string? Location { get; set; }
        public string? Comment { get; set; }

        /// <summary>
        /// Start time of the occurrence (controls the new segment's StartDate
        /// and therefore the time-of-day for all future occurrences).
        /// </summary>
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// End time of the occurrence (controls duration for future occurrences).
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        public string? RecurrenceRuleJson { get; set; }

        public ImportanceLevel? Importance { get; set; }
        public int? EventTypeId { get; set; }
    }
}