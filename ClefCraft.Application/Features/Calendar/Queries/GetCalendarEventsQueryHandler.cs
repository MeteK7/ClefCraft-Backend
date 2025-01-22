using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class GetCalendarEventsQueryHandler : IRequestHandler<GetCalendarEventsQuery, List<CalendarEventDto>>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IMapper _mapper;

        public GetCalendarEventsQueryHandler(ICalendarEventRepository calendarEventRepository, IMapper mapper)
        {
            _calendarEventRepository = calendarEventRepository;
            _mapper = mapper;
        }

        public async Task<List<CalendarEventDto>> Handle(GetCalendarEventsQuery request, CancellationToken cancellationToken)
        {
            var events = await _calendarEventRepository.GetByUserIdAsync(request.UserId);
            return _mapper.Map<List<CalendarEventDto>>(events);
        }
    }
}
