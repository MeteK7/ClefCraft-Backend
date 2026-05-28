using AutoMapper;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent
{
    public class CreateCalendarEventCommandHandler
        : IRequestHandler<CreateCalendarEventCommand, CalendarEventDto>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IRecurrenceSeriesRepository _seriesRepo;
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCalendarEventCommandHandler(
            ICalendarEventRepository calendarEventRepository,
            IRecurrenceSeriesRepository seriesRepo,
            ICalendarEventSegmentRepository segmentRepo,
            IMapper mapper,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _calendarEventRepository = calendarEventRepository;
            _seriesRepo = seriesRepo;
            _segmentRepo = segmentRepo;
            _mapper = mapper;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<CalendarEventDto> Handle(
            CreateCalendarEventCommand request,
            CancellationToken cancellationToken)
        {
            if (!request.AllDayEvent && request.StartDate >= request.EndDate)
                throw new ValidationException("End time must be after start time.");

            // ---------------------------------------------------------------
            // 1. Assign a stable SeriesUid for recurring events.
            //    Non-recurring events also get one so that if the user later
            //    turns the event recurring the identity is already in place.
            // ---------------------------------------------------------------
            var seriesUid = Guid.NewGuid().ToString();

            var calendarEvent = new CalendarEvent
            {
                Subject = request.Subject,
                Location = request.Location,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                AllDayEvent = request.AllDayEvent,
                EventTypeId = request.EventTypeId,
                Importance = request.Importance,
                Comment = request.Comment,
                LinkedBoardItemId = request.LinkedBoardItemId,
                UserId = _userService.UserId,
                IsRecurring = request.IsRecurring,
                RecurrenceRuleJson = request.RecurrenceRuleJson,
                SeriesUid = seriesUid,
                DateCreated = DateTime.UtcNow,
                DateModified = DateTime.UtcNow
            };

            await _calendarEventRepository.CreateAsync(calendarEvent);

            // ---------------------------------------------------------------
            // 2. Bootstrap the segment architecture for recurring events.
            //
            //    We always create a RecurrenceSeries + one initial segment,
            //    even for events that arrive without a recurrence rule.
            //    This ensures that:
            //      - The projection service never falls through to the legacy
            //        path for newly created events.
            //      - "This and following" splits work immediately without
            //        requiring a migration step.
            //
            //    The segment's EffectiveTo is null (open-ended) so it covers
            //    the entire future timeline until explicitly closed by a split.
            // ---------------------------------------------------------------
            if (request.IsRecurring)
            {
                var series = new RecurrenceSeries
                {
                    UserId = _userService.UserId,
                    SeriesUid = seriesUid,
                    CreatedAt = DateTime.UtcNow
                };

                await _seriesRepo.CreateAsync(series);

                // Save the series first so we have its Id for the FK.
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var initialSegment = new CalendarEventSegment
                {
                    RecurrenceSeriesId = series.Id,
                    EffectiveFrom = request.StartDate,
                    EffectiveTo = null,                  // open-ended

                    Subject = request.Subject,
                    Location = request.Location,
                    Comment = request.Comment,

                    StartDate = request.StartDate,
                    EndDate = request.EndDate,

                    IsRecurring = request.IsRecurring,
                    RecurrenceRuleJson = request.RecurrenceRuleJson,

                    Importance = request.Importance,
                    EventTypeId = request.EventTypeId
                };

                await _segmentRepo.CreateAsync(initialSegment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CalendarEventDto>(calendarEvent);
        }
    }
}
