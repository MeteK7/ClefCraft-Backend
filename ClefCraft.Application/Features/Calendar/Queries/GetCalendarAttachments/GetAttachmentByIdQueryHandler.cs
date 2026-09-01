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
    public class GetAttachmentByIdQueryHandler : IRequestHandler<GetAttachmentByIdQuery, CalendarEventAttachmentDto>
    {
        private readonly ICalendarEventAttachmentRepository _attachmentRepo;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IMapper _mapper;

        public GetAttachmentByIdQueryHandler(
            ICalendarEventAttachmentRepository attachmentRepo,
            ICalendarAccessService calendarAccessService,
            IMapper mapper)
        {
            _attachmentRepo = attachmentRepo;
            _calendarAccessService = calendarAccessService;
            _mapper = mapper;
        }

        public async Task<CalendarEventAttachmentDto> Handle(GetAttachmentByIdQuery request, CancellationToken cancellationToken)
        {
            await _calendarAccessService.EnsureAttachmentOwnedByUserAsync(request.Id, request.UserId);

            var attachment = await _attachmentRepo.GetByIdReadOnlyAsync(request.Id);
            if (attachment == null) return null;

            return _mapper.Map<CalendarEventAttachmentDto>(attachment);
        }
    }

}
