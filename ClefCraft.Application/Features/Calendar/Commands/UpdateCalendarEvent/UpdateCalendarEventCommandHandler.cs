using AutoMapper;
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
        private readonly IMapper _mapper;

        public UpdateCalendarEventCommandHandler(ICalendarEventRepository calendarEventRepository, IMapper mapper)
        {
            _calendarEventRepository = calendarEventRepository;
            _mapper = mapper;
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

            if (!request.AllDayEvent)
            {
                if (request.StartDate >= request.EndDate)
                    throw new ValidationException("End time must be after start time.");
            }

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

            return _mapper.Map<CalendarEventDto>(entity);
        }
    }
}
