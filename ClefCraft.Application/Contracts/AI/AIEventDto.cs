using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.AI
{
    public enum EventImportance
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public class AIEventDto
    {
        public string UserId { get; set; }
        public int EventId { get; set; }

        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public double DurationMinutes { get; set; }
        public int HourOfDay { get; set; }
        public int DayOfWeek { get; set; }
        public bool IsRecurring { get; set; }
        public EventImportance Importance { get; set; }
        public int RescheduleCount { get; set; }
        public double AvgDaysRescheduled { get; set; }
        public int EditCount { get; set; }
        public double ViewSignalValue { get; set; }
        public bool HasLinkedTask { get; set; }
        public int LinkedTaskReopenCount { get; set; }
        public int LinkedTaskStatusChanges { get; set; }
        public double LinkedTaskCompletionRate { get; set; }
    }
}