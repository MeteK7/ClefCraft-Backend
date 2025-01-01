using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class GetCalendarEventsQuery : IRequest<List<CalendarEventDto>>
    {
        public string UserId { get; set; }
    }

}
