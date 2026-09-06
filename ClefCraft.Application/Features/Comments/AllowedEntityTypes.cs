namespace ClefCraft.Application.Features.Comments
{
    // A separate allow-list from ActivityLogs.AllowedEntityTypes on purpose: that one gates
    // "is this type's MetadataJson diff shape known-safe to expose", which has nothing to do
    // with "does this type support commenting". Comments addresses BoardItem/CalendarEvent by
    // their stable root id in both cases, so both ship from day one (see the Comments feature
    // plan for why Comments doesn't hit the recurring-event merge problem History did).
    public static class AllowedEntityTypes
    {
        public static readonly HashSet<string> Values = new()
        {
            "BoardItem",
            "CalendarEvent"
        };
    }
}
