using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class CalendarEventDto
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string? Location { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public bool AllDayEvent { get; set; }
        public int? EventTypeId { get; set; }
        public string? EventTypeName { get; set; }
        public string? EventColor { get; set; }
        public string Importance { get; set; }
        public string? Comment { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrenceRuleJson { get; set; }
        public int? LinkedBoardItemId { get; set; }
        public string? LinkedBoardItemTitle { get; set; }
        public double? AttendanceScore { get; set; }
    }
}
