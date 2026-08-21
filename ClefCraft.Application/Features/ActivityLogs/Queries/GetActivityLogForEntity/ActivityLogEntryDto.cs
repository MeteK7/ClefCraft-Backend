using System;
using System.Collections.Generic;

namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity
{
    public class ActivityLogEntryDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = default!;
        public int EntityId { get; set; }
        public string ActionType { get; set; } = default!;
        public DateTime Timestamp { get; set; }
        public string ActorUserId { get; set; } = default!;
        public string ActorFullName { get; set; } = default!;
        public List<ActivityFieldChangeDto> Changes { get; set; } = new();
    }
}
