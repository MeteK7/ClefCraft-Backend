using ClefCraft.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Queries
{
    public class CalendarEventDto
    {
        // Physical DB row
        public int Id { get; set; }

        // Analytics/tracking identity
        public int BaseEventId { get; set; }

        // Logical recurrence family identity
        public string? SeriesUid { get; set; }

        // Stable UI identity for generated occurrences
        public string? OccurrenceKey { get; set; }
        
        // Original occurrence date
        public DateTimeOffset? OccurrenceDate { get; set; }
        public string Subject { get; set; }
        public string? Location { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public bool AllDayEvent { get; set; }
        public int? EventTypeId { get; set; }
        public string? EventTypeName { get; set; }
        public string? EventColor { get; set; }
        public ImportanceLevel Importance { get; set; }
        public string? Comment { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrenceRuleJson { get; set; }
        public int? LinkedBoardItemId { get; set; }
        public string? LinkedBoardItemTitle { get; set; }
        public double? AttendanceScore { get; set; }
        public List<int> ReminderMinutes { get; set; } = new();
    }
}