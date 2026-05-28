using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateSeries
{
    public class UpdateSeriesPreserveExceptionsCommandHandler
        : IRequestHandler<UpdateSeriesPreserveExceptionsCommand>
    {
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly IUnitOfWork _uow;

        public UpdateSeriesPreserveExceptionsCommandHandler(
            ICalendarEventSegmentRepository segmentRepo,
            IUnitOfWork uow)
        {
            _segmentRepo = segmentRepo;
            _uow = uow;
        }

        public async Task<Unit> Handle(
            UpdateSeriesPreserveExceptionsCommand request,
            CancellationToken cancellationToken)
        {
            var segments =
                await _segmentRepo.GetBySeriesUidAsync(
                    request.SeriesUid);

            foreach (var segment in segments)
            {
                if (request.Subject != null)
                    segment.Subject = request.Subject;

                if (request.Location != null)
                    segment.Location = request.Location;

                if (request.Comment != null)
                    segment.Comment = request.Comment;

                if (request.RecurrenceRuleJson != null)
                    segment.RecurrenceRuleJson =
                        request.RecurrenceRuleJson;
            }

            await _uow.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
