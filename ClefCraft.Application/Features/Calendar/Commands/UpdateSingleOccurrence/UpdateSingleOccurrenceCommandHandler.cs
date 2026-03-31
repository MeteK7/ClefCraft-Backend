using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateSingleOccurrence
{
    public class UpdateSingleOccurrenceCommandHandler
        : IRequestHandler<UpdateSingleOccurrenceCommand>
    {
        private readonly ICalendarEventExceptionRepository _repo;

        public UpdateSingleOccurrenceCommandHandler(
            ICalendarEventExceptionRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(
            UpdateSingleOccurrenceCommand request,
            CancellationToken cancellationToken)
        {
            var exception = await _repo.GetByEventAndDate(
                request.EventId,
                request.OccurrenceDate);

            if (exception == null)
            {
                exception = new CalendarEventException
                {
                    CalendarEventId = request.EventId,
                    OccurrenceDate = request.OccurrenceDate
                };
            }

            // Apply overrides
            exception.Comment = request.Comment ?? exception.Comment;
            exception.Subject = request.Subject ?? exception.Subject;
            exception.StartDate = request.StartDate ?? exception.StartDate;
            exception.EndDate = request.EndDate ?? exception.EndDate;

            if (request.IsCancelled.HasValue)
                exception.IsCancelled = request.IsCancelled.Value;

            await _repo.UpsertAsync(exception);

            return Unit.Value;
        }
    }
}
