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
    public class GetAttachmentByIdQueryHandler : IRequestHandler<GetAttachmentByIdQuery, CalendarEventAttachmentDto>
    {
        private readonly ICalendarEventAttachmentRepository _attachmentRepo;
        private readonly IMapper _mapper;

        public GetAttachmentByIdQueryHandler(ICalendarEventAttachmentRepository attachmentRepo, IMapper mapper)
        {
            _attachmentRepo = attachmentRepo;
            _mapper = mapper;
        }

        public async Task<CalendarEventAttachmentDto> Handle(GetAttachmentByIdQuery request, CancellationToken cancellationToken)
        {
            var attachment = await _attachmentRepo.GetByIdReadOnlyAsync(request.Id);
            if (attachment == null) return null;

            return _mapper.Map<CalendarEventAttachmentDto>(attachment);
        }
    }

}
