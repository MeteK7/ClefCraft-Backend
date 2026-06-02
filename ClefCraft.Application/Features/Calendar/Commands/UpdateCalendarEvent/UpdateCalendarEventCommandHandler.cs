using AutoMapper;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateCalendarEvent
{
    public class UpdateCalendarEventCommandHandler : IRequestHandler<UpdateCalendarEventCommand, CalendarEventDto>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IActivityLogger _activityLogger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReminderSchedulerService _reminderSchedulerService;

        public UpdateCalendarEventCommandHandler(
            ICalendarEventRepository calendarEventRepository,
            IActivityLogger activityLogger,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IReminderSchedulerService reminderSchedulerService)
        {
            _calendarEventRepository = calendarEventRepository;
            _activityLogger = activityLogger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _reminderSchedulerService = reminderSchedulerService;
        }

        public async Task<CalendarEventDto> Handle(
            UpdateCalendarEventCommand request,
            CancellationToken cancellationToken)
        {
            var entity =
                await _calendarEventRepository
                    .GetByIdAsync(request.Id);

            if (entity == null)
                throw new NotFoundException(
                    nameof(CalendarEvent),
                    request.Id);

            if (string.IsNullOrWhiteSpace(request.Subject))
                throw new ValidationException(
                    "Subject is required.");

            if (!request.AllDayEvent &&
                request.StartDate >= request.EndDate)
            {
                throw new ValidationException(
                    "End time must be after start time.");
            }

            var wasRescheduled =
                entity.StartDate != request.StartDate ||
                entity.EndDate != request.EndDate;

            var importanceChanged =
                entity.Importance != request.Importance;

            var previousStart = entity.StartDate;
            var previousEnd = entity.EndDate;
            var previousImportance = entity.Importance;

            entity.Subject = request.Subject;
            entity.Location = request.Location;
            entity.StartDate = request.StartDate;
            entity.EndDate = request.EndDate;
            entity.AllDayEvent = request.AllDayEvent;
            entity.EventTypeId = request.EventTypeId;
            entity.Importance = request.Importance;
            entity.Comment = request.Comment;
            entity.IsRecurring = request.IsRecurring;
            entity.RecurrenceRuleJson = request.RecurrenceRuleJson;
            entity.DateModified = DateTime.UtcNow;

            await _calendarEventRepository.UpdateAsync(entity);

            if (wasRescheduled)
            {
                await _activityLogger.LogAsync(
                    "CalendarEvent",
                    entity.Id,
                    "EVENT_RESCHEDULED",
                    new
                    {
                        PreviousStart = previousStart,
                        PreviousEnd = previousEnd,
                        NewStart = request.StartDate,
                        NewEnd = request.EndDate,
                        DaysShifted =
                            (request.StartDate - previousStart)
                            .TotalDays
                    });
            }

            if (importanceChanged)
            {
                await _activityLogger.LogAsync(
                    "CalendarEvent",
                    entity.Id,
                    "IMPORTANCE_CHANGED",
                    new
                    {
                        Previous = previousImportance,
                        New = request.Importance
                    });
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            if (wasRescheduled)
            {
                await _reminderSchedulerService
                    .RescheduleAsync(
                        entity,
                        cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }

            return _mapper.Map<CalendarEventDto>(entity);
        }
    }
}
