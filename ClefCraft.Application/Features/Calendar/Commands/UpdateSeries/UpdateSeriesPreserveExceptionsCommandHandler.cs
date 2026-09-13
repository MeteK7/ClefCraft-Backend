using ClefCraft.Application.Common.Helpers;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateSeries
{
    public class UpdateSeriesPreserveExceptionsCommandHandler
        : IRequestHandler<UpdateSeriesPreserveExceptionsCommand>
    {
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _uow;

        public UpdateSeriesPreserveExceptionsCommandHandler(
            ICalendarEventSegmentRepository segmentRepo,
            ICalendarAccessService calendarAccessService,
            IUserService userService,
            IUnitOfWork uow)
        {
            _segmentRepo = segmentRepo;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
            _uow = uow;
        }

        public async Task<Unit> Handle(
            UpdateSeriesPreserveExceptionsCommand request,
            CancellationToken cancellationToken)
        {
            await _calendarAccessService.EnsureSeriesOwnedByUserAsync(request.SeriesUid, _userService.UserId);

            var segments =
                await _segmentRepo.GetBySeriesUidAsync(
                    request.SeriesUid);

            // RecurrenceRuleJson is optional here — null means "leave it untouched", so
            // only validate (and only against segments that will actually be updated) when
            // the caller is actually supplying a new rule. An invalid rule (e.g. Interval <= 0)
            // must never reach the live projection service, where it would infinite-loop.
            if (request.RecurrenceRuleJson != null)
            {
                var parsedRule = JsonSerializer.Deserialize<RecurrenceRule>(request.RecurrenceRuleJson);
                foreach (var segment in segments)
                {
                    RecurrenceHelper.ValidateRule(parsedRule!, segment.StartDate);
                }
            }

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
