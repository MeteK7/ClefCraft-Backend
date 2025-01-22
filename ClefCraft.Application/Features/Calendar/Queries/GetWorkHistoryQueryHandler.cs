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
    public class GetWorkHistoryQueryHandler : IRequestHandler<GetWorkHistoryQuery, List<WorkHistoryDto>>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IMapper _mapper;

        public GetWorkHistoryQueryHandler(ICalendarEventRepository calendarEventRepository, IMapper mapper)
        {
            _calendarEventRepository = calendarEventRepository;
            _mapper = mapper;
        }
        public async Task<List<WorkHistoryDto>> Handle(GetWorkHistoryQuery request, CancellationToken cancellationToken)
        {
            var history = await _calendarEventRepository.GetWorkHistoryByItemIdAsync(request.ItemId);
            return _mapper.Map<List<WorkHistoryDto>>(history);
        }
    }
}
