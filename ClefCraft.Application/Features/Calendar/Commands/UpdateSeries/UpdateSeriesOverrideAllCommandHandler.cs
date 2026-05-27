using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateSeries
{
    public class UpdateSeriesOverrideAllCommandHandler : IRequestHandler<UpdateSeriesOverrideAllCommand>
    {
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly IUnitOfWork _uow;

        public UpdateSeriesOverrideAllCommandHandler(
            ICalendarEventSegmentRepository segmentRepo,
            IUnitOfWork uow)
        {
            _segmentRepo = segmentRepo;
            _uow = uow;
        }

        public async Task<Unit> Handle(UpdateSeriesOverrideAllCommand request, CancellationToken cancellationToken)
        {
            var segments = await _segmentRepo
                .GetBySeriesUidAsync(request.SeriesUid);

            foreach (var segment in segments)
            {
                segment.Subject = request.Subject ?? segment.Subject;
                segment.Location = request.Location ?? segment.Location;
                segment.Comment = request.Comment ?? segment.Comment;

                segment.RecurrenceRuleJson = request.RecurrenceRuleJson;
            }

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
