using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
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
    public class UpdateSingleOccurrenceCommandHandler : IRequestHandler<UpdateSingleOccurrenceCommand>
    {
        private readonly ICalendarEventExceptionRepository _repo;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSingleOccurrenceCommandHandler(
            ICalendarEventExceptionRepository repo,
            ICalendarAccessService calendarAccessService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateSingleOccurrenceCommand request, CancellationToken cancellationToken)
        {
            await _calendarAccessService.EnsureSeriesOwnedByUserAsync(request.SeriesUid, _userService.UserId);

            // IMPORTANT: now lookup is SeriesUid + OccurrenceDate
            var exception = await _repo.GetBySeriesAndDate(
                request.SeriesUid,
                request.OccurrenceDate);

            if (exception == null)
            {
                exception = new CalendarEventException
                {
                    SeriesUid = request.SeriesUid,
                    OccurrenceDate = request.OccurrenceDate
                };
            }

            exception.Subject = request.Subject ?? exception.Subject;
            exception.Comment = request.Comment ?? exception.Comment;
            exception.StartDate = request.StartDate ?? exception.StartDate;
            exception.EndDate = request.EndDate ?? exception.EndDate;

            if (request.Location != null)
                exception.Location = request.Location;

            exception.EventTypeId = request.EventTypeId ?? exception.EventTypeId;
            exception.IsCancelled = request.IsCancelled ?? exception.IsCancelled;

            await _repo.UpsertAsync(exception);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}