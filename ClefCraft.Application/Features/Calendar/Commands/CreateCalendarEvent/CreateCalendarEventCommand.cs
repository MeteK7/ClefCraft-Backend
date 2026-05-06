using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent
{
    public class CreateCalendarEventCommand : IRequest<CalendarEventDto>
    {
        public string Subject { get; set; }
        public string? Location { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public bool AllDayEvent { get; set; }
        public int? EventTypeId { get; set; }
        public ImportanceLevel Importance { get; set; }
        public string? Comment { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrenceRuleJson { get; set; }
        public int? LinkedBoardItemId { get; set; }
    }
}
