using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent
{
    public class CreateCalendarEventCommandHandler : IRequestHandler<CreateCalendarEventCommand, CalendarEventDto>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IMapper _mapper;

        public CreateCalendarEventCommandHandler(ICalendarEventRepository calendarEventRepository, IMapper mapper)
        {
            _calendarEventRepository = calendarEventRepository;
            _mapper = mapper;
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

            var calendarEvent = new CalendarEvent
            {
                Subject = request.Subject,
                Location = request.Location,
                StartDate = request.StartDate.ToUniversalTime(),
                EndDate = request.EndDate.ToUniversalTime(),
                AllDayEvent = request.AllDayEvent,
                Importance = request.Importance,
                Comment = request.Comment,
                LinkedBoardItemId = request.LinkedBoardItemId,
                UserId = request.UserId,
                DateCreated = DateTime.UtcNow // Always use UTC for server timestamps
            };
             

            await _calendarEventRepository.CreateAsync(calendarEvent);

            return _mapper.Map<CalendarEventDto>(calendarEvent);
        }
    }
}
