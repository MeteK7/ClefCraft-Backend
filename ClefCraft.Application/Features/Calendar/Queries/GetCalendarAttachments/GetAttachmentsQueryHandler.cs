using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries.GetCalendarAttachments
{
    public class GetAttachmentsQueryHandler : IRequestHandler<GetAttachmentsQuery, List<CalendarEventAttachmentDto>>
    {
        private readonly ICalendarEventAttachmentRepository _repo;
        private readonly IMapper _mapper;

        public GetAttachmentsQueryHandler(ICalendarEventAttachmentRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<CalendarEventAttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
        {
            var items = await _repo.GetByEventIdAsync(request.EventId);
            return _mapper.Map<List<CalendarEventAttachmentDto>>(items);
        }
    }
}
