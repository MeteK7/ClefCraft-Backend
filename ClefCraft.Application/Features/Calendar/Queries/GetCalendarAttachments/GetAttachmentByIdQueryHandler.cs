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

        public GetAttachmentByIdQueryHandler(
            ICalendarEventAttachmentRepository attachmentRepo,
            ICalendarAccessService calendarAccessService)
        {
            _attachmentRepo = attachmentRepo;
            _calendarAccessService = calendarAccessService;
        }

        public async Task<CalendarEventAttachmentDto> Handle(GetAttachmentByIdQuery request, CancellationToken cancellationToken)
        {
            // Read-only: owner or a granted collaborator can download. Upload/delete stay
            // owner-only via EnsureAttachmentOwnedByUserAsync elsewhere.
            await _calendarAccessService.EnsureCanAccessAttachmentAsync(request.Id, request.UserId);

            var attachment = await _attachmentRepo.GetByIdReadOnlyAsync(request.Id);
            if (attachment == null) return null;

            return CalendarMapper.ToDto(attachment);
        }
    }

}
