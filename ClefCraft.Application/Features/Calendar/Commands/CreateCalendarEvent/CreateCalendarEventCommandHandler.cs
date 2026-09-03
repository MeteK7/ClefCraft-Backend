using AutoMapper;
using ClefCraft.Application.Common.Helpers;
using ClefCraft.Application.Contracts.Authorization;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent
{
    public class CreateCalendarEventCommandHandler
        : IRequestHandler<CreateCalendarEventCommand, CalendarEventDto>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IRecurrenceSeriesRepository _seriesRepo;
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly ICalendarReminderRepository _reminderRepo;
        private readonly IReminderSchedulerService _reminderSchedulerService;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCalendarEventCommandHandler(
            ICalendarEventRepository calendarEventRepository,
            IRecurrenceSeriesRepository seriesRepo,
            ICalendarEventSegmentRepository segmentRepo,
            ICalendarReminderRepository reminderRepo,
            IReminderSchedulerService reminderSchedulerService,
            IBoardAccessService boardAccessService,
            IMapper mapper,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _calendarEventRepository = calendarEventRepository;
            _seriesRepo = seriesRepo;
            _segmentRepo = segmentRepo;
            _reminderRepo = reminderRepo;
            _reminderSchedulerService = reminderSchedulerService;
            _boardAccessService = boardAccessService;
            _mapper = mapper;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<CalendarEventDto> Handle(
            CreateCalendarEventCommand request,
            CancellationToken cancellationToken)
        {
            if (!request.AllDayEvent &&
                request.StartDate >= request.EndDate)
            {
                throw new ValidationException(
                    "End time must be after start time.");
            }

            if (request.IsRecurring)
            {
                var parsedRule = string.IsNullOrWhiteSpace(request.RecurrenceRuleJson)
                    ? null
                    : JsonSerializer.Deserialize<RecurrenceRule>(request.RecurrenceRuleJson);

                RecurrenceHelper.ValidateRule(parsedRule!, request.StartDate);
            }

            var userId = _userService.UserId;

            if (request.LinkedBoardItemId.HasValue)
            {
                await _boardAccessService.EnsureBoardItemOwnedByUserAsync(request.LinkedBoardItemId.Value, userId);
            }

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

            // Persist first so Event.Id exists
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // =====================================================
            // REMINDERS
            // =====================================================

            if (request.ReminderMinutes?.Any() == true)
            {
                foreach (var minutes in request.ReminderMinutes.Distinct())
                {
                    await _reminderRepo.CreateAsync(
                        new CalendarReminder
                        {
                            CalendarEventId = calendarEvent.Id,
                            MinutesBeforeStart = minutes,
                            IsEnabled = true,
                            IsSent = false,
                            SentAt = null
                        });
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _reminderSchedulerService.ScheduleAsync(
                    calendarEvent,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // =====================================================
            // RECURRING SERIES
            // =====================================================

            if (request.IsRecurring)
            {
                var series = new RecurrenceSeries
                {
                    UserId = _userService.UserId,
                    SeriesUid = seriesUid,
                    CreatedAt = DateTime.UtcNow
                };

                await _seriesRepo.CreateAsync(series);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var initialSegment = new CalendarEventSegment
                {
                    RecurrenceSeriesId = series.Id,

                    EffectiveFrom = request.StartDate,
                    EffectiveTo = null,

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

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return _mapper.Map<CalendarEventDto>(calendarEvent);
        }
    }
}