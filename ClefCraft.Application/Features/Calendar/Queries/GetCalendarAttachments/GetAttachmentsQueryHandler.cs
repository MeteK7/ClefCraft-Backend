using AutoMapper;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
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
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IMapper _mapper;

        public GetAttachmentsQueryHandler(
            ICalendarEventAttachmentRepository attachmentRepo,
            ICalendarAccessService calendarAccessService,
            IMapper mapper)
        {
            _attachmentRepo = attachmentRepo;
            _calendarAccessService = calendarAccessService;
            _mapper = mapper;
        }

        public async Task<List<CalendarEventAttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
        {
            await _calendarAccessService.EnsureEventOwnedByUserAsync(request.EventId, request.UserId);

            var items = await _attachmentRepo.GetByEventIdAsync(request.EventId);
            return _mapper.Map<List<CalendarEventAttachmentDto>>(items);
        }
    }
}
