using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarAttachment
{
    public class DeleteAttachmentCommand : IRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
    }
}
