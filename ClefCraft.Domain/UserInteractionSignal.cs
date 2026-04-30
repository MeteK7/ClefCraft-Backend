using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Domain
{
    public class UserInteractionSignal : BaseEntity
    {
        public string UserId { get; set; }
        public string SignalType { get; set; }
        // "SNOOZE", "DRAG_DROP", "EDIT", "COMMENT", "VIEW"
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public double Value { get; set; } // intensity, duration, etc.
        public DateTime Timestamp { get; set; }
    }
}
