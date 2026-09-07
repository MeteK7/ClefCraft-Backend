namespace ClefCraft.Application.Features.CalendarEventCollaborators
{
    public class CalendarEventCollaboratorDto
    {
        public string UserId { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public DateTime? DateGranted { get; set; }
    }
}
