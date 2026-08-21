namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity
{
    public class ActivityFieldChangeDto
    {
        public string FieldName { get; set; } = default!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
