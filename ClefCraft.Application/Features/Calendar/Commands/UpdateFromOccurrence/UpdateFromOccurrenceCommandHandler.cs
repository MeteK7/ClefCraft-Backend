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

            if (activeSegment == null)
                throw new NotFoundException(nameof(CalendarEventSegment), request.OccurrenceDate.ToString());

            // 1. Cap the old segment strictly BEFORE the targeted split occurrence date
            activeSegment.EffectiveTo = request.OccurrenceDate;
            await _segmentRepo.UpdateAsync(activeSegment);

            var originalDuration = activeSegment.EndDate - activeSegment.StartDate;

            // 2. The new segment recurrence anchors on the newly edited modifications
            var occurrenceStart = request.StartDate ?? request.OccurrenceDate;
            var occurrenceEnd = request.EndDate ?? (occurrenceStart + originalDuration);

            var newSegment = new CalendarEventSegment
            {
                RecurrenceSeriesId = series.Id,

                // EffectiveFrom matches the occurrenceDate exactly so it captures the "This" step
                EffectiveFrom = request.OccurrenceDate,
                EffectiveTo = null,

                Subject = request.Subject ?? activeSegment.Subject,
                Location = request.Location ?? activeSegment.Location,
                Comment = request.Comment ?? activeSegment.Comment,

                // Anchor point for generating future recurrences in this new chain
                StartDate = occurrenceStart,
                EndDate = occurrenceEnd,

                IsRecurring = activeSegment.IsRecurring,
                RecurrenceRuleJson = request.RecurrenceRuleJson ?? activeSegment.RecurrenceRuleJson,

                Importance = request.Importance ?? activeSegment.Importance,
                EventTypeId = request.EventTypeId ?? activeSegment.EventTypeId
            };

            await _segmentRepo.CreateAsync(newSegment);

            // 3. Clear downstream exceptions because they are overridden by the new design rules
            await _exceptionRepo.DeleteFromDateAsync(
                request.SeriesUid,
                request.OccurrenceDate);

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}