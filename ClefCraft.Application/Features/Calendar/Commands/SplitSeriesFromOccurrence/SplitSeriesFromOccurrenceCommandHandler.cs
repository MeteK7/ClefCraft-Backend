using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.SplitSeriesFromOccurrence
{
    public class SplitSeriesFromOccurrenceCommandHandler
        : IRequestHandler<SplitSeriesFromOccurrenceCommand>
    {
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly IUnitOfWork _uow;

        public SplitSeriesFromOccurrenceCommandHandler(
            ICalendarEventSegmentRepository segmentRepo,
            IUnitOfWork uow)
        {
            _segmentRepo = segmentRepo;
            _uow = uow;
        }

        public async Task<Unit> Handle(
            SplitSeriesFromOccurrenceCommand request,
            CancellationToken cancellationToken)
        {
            var segment = await _segmentRepo.GetByIdAsync(request.SegmentId);

            if (segment == null)
                throw new Exception("Segment not found");

            // close current segment
            segment.EffectiveTo = request.SplitDate;

            var newSegment = new CalendarEventSegment
            {
                RecurrenceSeriesId = segment.RecurrenceSeriesId,

                EffectiveFrom = request.SplitDate,
                EffectiveTo = null,

                Subject = request.Subject ?? segment.Subject,
                Location = request.Location ?? segment.Location,
                Comment = request.Comment ?? segment.Comment,

                StartDate = request.SplitDate,
                EndDate = request.SplitDate + (segment.EndDate - segment.StartDate),

                RecurrenceRuleJson = request.RecurrenceRuleJson ?? segment.RecurrenceRuleJson,

                IsRecurring = segment.IsRecurring,
                Importance = segment.Importance,
                EventTypeId = segment.EventTypeId
            };

            await _segmentRepo.CreateAsync(newSegment);

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}