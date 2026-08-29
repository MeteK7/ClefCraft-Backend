using AutoMapper;
using ClefCraft.Application.Common.Helpers;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UpdateCalendarEvent
{
    public class UpdateCalendarEventCommandHandler : IRequestHandler<UpdateCalendarEventCommand, CalendarEventDto>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly ICalendarReminderRepository _reminderRepo; // Added missing dependency
        private readonly IRecurrenceSeriesRepository _seriesRepo;
        private readonly ICalendarEventSegmentRepository _segmentRepo;
        private readonly ICalendarEventExceptionRepository _exceptionRepo;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReminderSchedulerService _reminderSchedulerService;

        public UpdateCalendarEventCommandHandler(
            ICalendarEventRepository calendarEventRepository,
            ICalendarReminderRepository reminderRepo, // Added to constructor injection
            IRecurrenceSeriesRepository seriesRepo,
            ICalendarEventSegmentRepository segmentRepo,
            ICalendarEventExceptionRepository exceptionRepo,
            IUserService userService,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IReminderSchedulerService reminderSchedulerService)
        {
            _calendarEventRepository = calendarEventRepository;
            _reminderRepo = reminderRepo;
            _seriesRepo = seriesRepo;
            _segmentRepo = segmentRepo;
            _exceptionRepo = exceptionRepo;
            _userService = userService;
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

            if (request.IsRecurring)
            {
                var parsedRule = string.IsNullOrWhiteSpace(request.RecurrenceRuleJson)
                    ? null
                    : JsonSerializer.Deserialize<RecurrenceRule>(request.RecurrenceRuleJson);

                RecurrenceHelper.ValidateRule(parsedRule!, request.StartDate);
            }

            var wasRecurring = entity.IsRecurring;

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
            // RECURRENCE SERIES SYNCHRONIZATION
            // ====================================================================
            // This endpoint has no "scope" concept (unlike the occurrence/series
            // endpoints) — it always represents the whole event. When recurrence
            // is turned on for a previously non-recurring event, the
            // RecurrenceSeries/CalendarEventSegment pair the projection/scope
            // endpoints rely on did not exist yet and must be created here (it
            // was previously only ever created at event-creation time). When
            // recurrence is turned off, any leftover series/segments/exceptions
            // are removed so they don't linger as orphaned data. Skipped
            // entirely for a plain non-recurring -> non-recurring edit (the
            // overwhelmingly common case) so it doesn't pay for a series
            // lookup it will never need.
            if (request.IsRecurring || wasRecurring)
            {
                var existingSeries = await _seriesRepo.GetBySeriesUidAsync(entity.SeriesUid);

                if (request.IsRecurring)
                {
                    if (existingSeries == null)
                    {
                        var series = new RecurrenceSeries
                        {
                            UserId = _userService.UserId,
                            SeriesUid = entity.SeriesUid,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _seriesRepo.CreateAsync(series);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        var initialSegment = new CalendarEventSegment
                        {
                            RecurrenceSeriesId = series.Id,
                            EffectiveFrom = entity.StartDate,
                            EffectiveTo = null,
                            Subject = entity.Subject,
                            Location = entity.Location,
                            Comment = entity.Comment,
                            StartDate = entity.StartDate,
                            EndDate = entity.EndDate,
                            IsRecurring = true,
                            RecurrenceRuleJson = entity.RecurrenceRuleJson,
                            Importance = entity.Importance,
                            EventTypeId = entity.EventTypeId
                        };

                        await _segmentRepo.CreateAsync(initialSegment);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        // A series already exists for this event (defensive —
                        // this endpoint is not normally reached for an
                        // already-recurring event once the scope dialog is
                        // used, but handle it safely if it ever is). This
                        // endpoint has no scope concept, so treat it as
                        // replacing the whole series definition.
                        foreach (var segment in existingSeries.Segments)
                        {
                            segment.Subject = entity.Subject;
                            segment.Location = entity.Location;
                            segment.Comment = entity.Comment;
                            segment.RecurrenceRuleJson = entity.RecurrenceRuleJson;
                        }

                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }
                else if (existingSeries != null)
                {
                    await _seriesRepo.DeleteAsync(existingSeries);
                    await _exceptionRepo.DeleteAllForSeriesAsync(entity.SeriesUid);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

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