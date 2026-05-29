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
            var series = await _seriesRepo.GetBySeriesUidAsync(request.SeriesUid);

            if (series == null)
                throw new NotFoundException(
                    nameof(RecurrenceSeries), request.SeriesUid);

            var activeSegment = await _segmentRepo.GetActiveSegmentAsync(
                series.Id,
                request.OccurrenceDate);

            // IMPORTANT: close the current segment at the split point
            activeSegment.EffectiveTo = request.OccurrenceDate;
            await _segmentRepo.UpdateAsync(activeSegment);

            var originalDuration =
                activeSegment.EndDate - activeSegment.StartDate;

            // IMPORTANT:
            // The new segment recurrence MUST anchor on the edited occurrence.
            // Otherwise the occurrence at the split boundary is skipped.
            var occurrenceStart =
                request.StartDate ?? request.OccurrenceDate;

            var occurrenceEnd =
                request.EndDate ?? (occurrenceStart + originalDuration);

            var newSegment = new CalendarEventSegment
            {
                RecurrenceSeriesId = series.Id,

                // start strictly AFTER split
                EffectiveFrom = request.OccurrenceDate,

                EffectiveTo = null,

                Subject = request.Subject ?? activeSegment.Subject,
                Location = request.Location ?? activeSegment.Location,
                Comment = request.Comment ?? activeSegment.Comment,

                StartDate = occurrenceStart,
                EndDate = occurrenceEnd,

                IsRecurring = activeSegment.IsRecurring,
                RecurrenceRuleJson = request.RecurrenceRuleJson ?? activeSegment.RecurrenceRuleJson,

                Importance = request.Importance ?? activeSegment.Importance,
                EventTypeId = request.EventTypeId ?? activeSegment.EventTypeId
            };

            await _segmentRepo.CreateAsync(newSegment);

            await _exceptionRepo.DeleteFromDateAsync(
                request.SeriesUid,
                request.OccurrenceDate);

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}