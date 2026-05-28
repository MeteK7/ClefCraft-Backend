using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateFromOccurrence
{
    public class UpdateFromOccurrenceCommandHandler
        : IRequestHandler<UpdateFromOccurrenceCommand>
    {
        private readonly IRecurrenceSeriesRepository _seriesRepo;
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly ICalendarEventExceptionRepository _exceptionRepo;
        private readonly IUnitOfWork _uow;

        public UpdateFromOccurrenceCommandHandler(
            IRecurrenceSeriesRepository seriesRepo,
            ICalendarEventSegmentRepository segmentRepo,
            ICalendarEventExceptionRepository exceptionRepo,
            IUnitOfWork uow)
        {
            _seriesRepo = seriesRepo;
            _segmentRepo = segmentRepo;
            _exceptionRepo = exceptionRepo;
            _uow = uow;
        }

        public async Task<Unit> Handle(
            UpdateFromOccurrenceCommand request,
            CancellationToken cancellationToken)
        {
            // ------------------------------------------------------------------
            // 1. Resolve the series.
            // ------------------------------------------------------------------
            var series = await _seriesRepo.GetBySeriesUidAsync(request.SeriesUid);

            if (series == null)
                throw new NotFoundException(
                    nameof(RecurrenceSeries), request.SeriesUid);

            // ------------------------------------------------------------------
            // 2. Find the segment that is active at OccurrenceDate.
            //    Uses [EffectiveFrom, EffectiveTo) interval semantics — the same
            //    convention used by GetActiveSegmentAsync.
            // ------------------------------------------------------------------
            var activeSegment = await _segmentRepo.GetActiveSegmentAsync(
                series.Id,
                request.OccurrenceDate);

            if (activeSegment == null)
            {
                // The series exists but has no segment covering OccurrenceDate.
                // This can happen for legacy events that were never migrated.
                // Raise a clear error so the caller can decide whether to
                // trigger a migration or surface a UX message.
                throw new InvalidOperationException(
                    $"No active segment found for series '{request.SeriesUid}' " +
                    $"at occurrence date {request.OccurrenceDate:O}. " +
                    "The series may need to be migrated to the segment architecture first.");
            }

            // ------------------------------------------------------------------
            // 3. Close the active segment at OccurrenceDate.
            //
            //    EffectiveTo is exclusive (half-open interval), so setting it
            //    to OccurrenceDate means the last occurrence covered by this
            //    segment is the one immediately before OccurrenceDate.
            // ------------------------------------------------------------------
            activeSegment.EffectiveTo = request.OccurrenceDate;

            // ------------------------------------------------------------------
            // 4. Build the new segment starting at OccurrenceDate.
            //    Inherit everything from the current segment, then apply the
            //    caller-supplied overrides on top.
            // ------------------------------------------------------------------
            var duration = activeSegment.EndDate - activeSegment.StartDate;
            var newStart = request.StartDate ?? request.OccurrenceDate;
            var newEnd = request.EndDate ?? (newStart + duration);

            var newSegment = new CalendarEventSegment
            {
                RecurrenceSeriesId = series.Id,
                EffectiveFrom = request.OccurrenceDate,
                EffectiveTo = null,              // open-ended

                Subject = request.Subject ?? activeSegment.Subject,
                Location = request.Location ?? activeSegment.Location,
                Comment = request.Comment ?? activeSegment.Comment,

                StartDate = newStart,
                EndDate = newEnd,

                IsRecurring = activeSegment.IsRecurring,
                RecurrenceRuleJson =
                    request.RecurrenceRuleJson ?? activeSegment.RecurrenceRuleJson,

                Importance = request.Importance ?? activeSegment.Importance,
                EventTypeId = request.EventTypeId ?? activeSegment.EventTypeId
            };

            await _segmentRepo.CreateAsync(newSegment);

            // ------------------------------------------------------------------
            // 5. Purge future exceptions.
            //
            //    Per-occurrence overrides that were created against the OLD
            //    segment definition are no longer semantically valid once the
            //    segment has been replaced.  Delete them so they don't ghost
            //    into the newly defined recurrence pattern.
            //
            //    Exceptions BEFORE OccurrenceDate are intentionally preserved
            //    — they belong to the unchanged historical segment.
            // ------------------------------------------------------------------
            await _exceptionRepo.DeleteFromDateAsync(
                request.SeriesUid,
                request.OccurrenceDate);

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}