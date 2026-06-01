using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class CalendarReminder : BaseEntity
    {
        public int CalendarEventId { get; set; }

        public CalendarEvent CalendarEvent { get; set; }

        public int MinutesBeforeStart { get; set; }

        public bool IsEnabled { get; set; } = true;
        public bool IsSent { get; set; }
        public DateTimeOffset? SentAt { get; set; }
    }
}
