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
        private readonly ICalendarEventAttachmentRepository _attachmentRepo;
        private readonly IMapper _mapper;

        public GetAttachmentsQueryHandler(ICalendarEventAttachmentRepository attachmentRepo, IMapper mapper)
        {
            _attachmentRepo = attachmentRepo;
            _mapper = mapper;
        }

        public async Task<List<CalendarEventAttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
        {
            var items = await _attachmentRepo.GetByEventIdAsync(request.EventId);
            return _mapper.Map<List<CalendarEventAttachmentDto>>(items);
        }
    }
}
