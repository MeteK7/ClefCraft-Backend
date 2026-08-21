using System.Collections.Generic;

namespace ClefCraft.Application.Features.ActivityLogs
{
    // The single source of truth for which EntityType values GetActivityLogForEntityQuery will
    // accept. Values here are case-sensitive and must exactly match entry.Entity.GetType().Name
    // as produced by ClefCraftDatabaseContext.SaveChangesAsync (e.g. "BoardItem", not
    // "boarditem"). Starts intentionally minimal — add an entity type here only once its
    // ActivityLog data is known-safe to expose (see the History feature plan's audit notes on
    // field-level redaction and duplicate-logging paths before adding "CalendarEvent").
    public static class AllowedEntityTypes
    {
        public static readonly HashSet<string> Values = new()
        {
            "BoardItem"
        };
    }
}
