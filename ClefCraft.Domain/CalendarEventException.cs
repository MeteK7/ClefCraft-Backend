using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class CalendarEventException : BaseEntity
    {
        public int CalendarEventId { get; set; }

        // Identifies WHICH occurrence
        public DateTimeOffset OccurrenceDate { get; set; }

        // Overrides (only applied if not null)
        public string? Subject { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }

        // Special cases
        public bool IsCancelled { get; set; } = false;

        public CalendarEvent CalendarEvent { get; set; }
    }
}
