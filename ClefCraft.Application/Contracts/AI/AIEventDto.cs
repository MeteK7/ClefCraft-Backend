using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.AI
{
    public class AIEventDto
    {
        // Identity
        public string UserId { get; set; }
        public int EventId { get; set; }

        // Temporal
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public double DurationMinutes { get; set; }
        public int HourOfDay { get; set; }
        public int DayOfWeek { get; set; }
        public bool IsRecurring { get; set; }

        // User-declared importance (weak signal)
        public string Importance { get; set; }

        // Behavioral signals (strong signals)
        public int RescheduleCount { get; set; }       // from ActivityLog EVENT_RESCHEDULED
        public double AvgDaysRescheduled { get; set; } // how far it gets pushed each time
        public int EditCount { get; set; }             // UPDATED actions on this event
        public double ViewSignalValue { get; set; }    // from UserInteractionSignal VIEW
        public bool HasLinkedTask { get; set; }        // linked board item = commitment signal

        // Task context signals (if linked)
        public int? LinkedTaskReopenCount { get; set; }
        public int? LinkedTaskStatusChanges { get; set; }
        public double? LinkedTaskCompletionRate { get; set; } // historical for this user
    }
}
