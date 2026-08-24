using AutoMapper;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
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
        private readonly ICalendarReminderRepository _reminderRepo; // Added missing dependency
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReminderSchedulerService _reminderSchedulerService;

        public UpdateCalendarEventCommandHandler(
            ICalendarEventRepository calendarEventRepository,
            ICalendarReminderRepository reminderRepo, // Added to constructor injection
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IReminderSchedulerService reminderSchedulerService)
        {
            _calendarEventRepository = calendarEventRepository;
            _reminderRepo = reminderRepo;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _reminderSchedulerService = reminderSchedulerService;
        }

        public async Task<CalendarEventDto> Handle(
            UpdateCalendarEventCommand request,
            CancellationToken cancellationToken)
        {
            var entity = await _calendarEventRepository.GetByIdAsync(request.Id);

            if (entity == null)
                throw new NotFoundException(nameof(CalendarEvent), request.Id);

            if (string.IsNullOrWhiteSpace(request.Subject))
                throw new ValidationException("Subject is required.");

            if (!request.AllDayEvent && request.StartDate >= request.EndDate)
                throw new ValidationException("End time must be after start time.");

            // Map request properties to domain entity
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

            // Save basic event details to db
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ====================================================================
            // REMINDER SYNCHRONIZATION 
            // ====================================================================
            // Clears and updates reminders unconditionally on every update cycle, 
            // allowing user preference adjustments even when times stay static.
            var existingReminders = await _reminderRepo.GetByEventIdAsync(entity.Id);
            foreach (var rem in existingReminders)
            {
                await _reminderRepo.DeleteAsync(rem);
            }

            if (request.ReminderMinutes?.Any() == true)
            {
                foreach (var minutes in request.ReminderMinutes.Distinct())
                {
                    await _reminderRepo.CreateAsync(new CalendarReminder
                    {
                        CalendarEventId = entity.Id,
                        MinutesBeforeStart = minutes,
                        IsEnabled = true,
                        IsSent = false
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Trigger engine recalculation/reschedule for the notification queues
            await _reminderSchedulerService.RescheduleAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<CalendarEventDto>(entity);
        }
    }
}