using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class NotificationQueue : BaseEntity
    {
        public string UserId { get; set; }

        public int CalendarEventId { get; set; }

        public DateTimeOffset ScheduledFor { get; set; }

        public bool IsProcessed { get; set; }

        public DateTimeOffset? ProcessedAt { get; set; }

        public string Message { get; set; }
    }
}
