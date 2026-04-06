using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class ActivityLog : BaseEntity
    {
        public string UserId { get; set; }

        // What entity was affected
        public string EntityType { get; set; } // "BoardItem", "CalendarEvent"
        public int EntityId { get; set; }

        // Action type
        public string ActionType { get; set; }
        // Examples:
        // "CREATED", "UPDATED", "DELETED"
        // "STATUS_CHANGED", "MOVED_COLUMN"
        // "SNOOZED", "POSTPONED", "REASSIGNED"

        public string? MetadataJson { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
