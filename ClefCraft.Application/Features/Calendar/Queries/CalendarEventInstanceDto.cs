using ClefCraft.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class CalendarEventInstanceDto
    {
        public int Id { get; set; }

        // IMPORTANT: original event identity for grouping / analytics
        public int BaseEventId { get; set; }

        public string Subject { get; set; }
        public string? Location { get; set; }
        public string? Comment { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }

        public bool AllDayEvent { get; set; }
        public int? EventTypeId { get; set; }

        public ImportanceLevel Importance { get; set; }
    }
}