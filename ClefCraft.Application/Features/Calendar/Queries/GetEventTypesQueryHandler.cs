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
    public class GetEventTypesQueryHandler
        : IRequestHandler<GetEventTypesQuery, List<EventTypeDto>>
    {
        private readonly IEventTypeRepository _eventTypeRepository;
        private readonly IMapper _mapper;

        public GetEventTypesQueryHandler(
            IEventTypeRepository eventTypeRepository,
            IMapper mapper)
        {
            _eventTypeRepository = eventTypeRepository;
            _mapper = mapper;
        }

        public async Task<List<EventTypeDto>> Handle(
            GetEventTypesQuery request,
            CancellationToken cancellationToken)
        {
            var types = await _eventTypeRepository
                .GetByUserIdAsync(request.UserId);

            var result = _mapper.Map<List<EventTypeDto>>(types);

            return result;
        }
    }
}