using ClefCraft.Application.Features.Calendar.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment
{
    public class UploadCalendarAttachmentCommand : IRequest<List<CalendarEventAttachmentDto>>
    {
        public int EventId { get; set; }
        public List<IFormFile> Files { get; set; }
        public string UserId { get; set; }
    }
}
