using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries.GetCalendarAttachments
{
    public class GetAttachmentByIdQuery : IRequest<CalendarEventAttachmentDto>
    {
        public int Id { get; set; }
    }
}
