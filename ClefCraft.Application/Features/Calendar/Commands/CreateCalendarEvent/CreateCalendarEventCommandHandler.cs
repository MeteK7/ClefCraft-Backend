using AutoMapper;
using ClefCraft.Application.Contracts.Identity;
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
    public class CreateCalendarEventCommandHandler : IRequestHandler<CreateCalendarEventCommand, CalendarEventDto>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public CreateCalendarEventCommandHandler(ICalendarEventRepository calendarEventRepository, IMapper mapper, IUserService userService)
        {
            _calendarEventRepository = calendarEventRepository;
            _mapper = mapper;
            _userService = userService;
        }

        public async Task<CalendarEventDto> Handle(CreateCalendarEventCommand request, CancellationToken cancellationToken)
        {
            //var calendarEvent = new CalendarEvent
            //{
            //    Subject = request.Subject,
            //    Location = request.Location,
            //    StartDate = request.StartDate,
            //    EndDate = request.EndDate,
            //    AllDayEvent = request.AllDayEvent,
            //    Importance = request.Importance,
            //    //Label = request.Label,
            //    Comment = request.Comment,
            //    LinkedBoardItemId = request.LinkedBoardItemId,
            //    UserId = request.UserId
            //};


            //var startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc); // Convert to server time zone
            //var endDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);   // Convert to server time zone

            if (!request.AllDayEvent)
            {
                if (request.StartDate >= request.EndDate)
                    throw new ValidationException("End time must be after start time.");
            }

            var calendarEvent = new CalendarEvent
            {
                Subject = request.Subject,
                Location = request.Location,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                AllDayEvent = request.AllDayEvent,
                Importance = request.Importance,
                Comment = request.Comment,
                LinkedBoardItemId = request.LinkedBoardItemId,
                UserId = _userService.UserId,
                DateCreated = DateTime.UtcNow, // Always use UTC for server timestamps
                DateModified = DateTime.UtcNow
            };
             

            await _calendarEventRepository.CreateAsync(calendarEvent);

            return _mapper.Map<CalendarEventDto>(calendarEvent);
        }
    }
}
