namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity
{
    public class ActivityFieldChangeDto
    {
        public string FieldName { get; set; } = default!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        /// <summary>
        /// Human-readable form of OldValue/NewValue where the raw value is an id the client can't
        /// resolve on its own. Set for AssigneeId (the user's full name, resolved regardless of
        /// current board membership); null for every other field.
        /// </summary>
        public string? OldDisplayValue { get; set; }
        public string? NewDisplayValue { get; set; }
    }
}
