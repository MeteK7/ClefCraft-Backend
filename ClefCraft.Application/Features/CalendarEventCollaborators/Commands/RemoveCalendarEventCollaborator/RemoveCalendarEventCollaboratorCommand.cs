using MediatR;

namespace ClefCraft.Application.Features.CalendarEventCollaborators.Commands.RemoveCalendarEventCollaborator
{
    public class RemoveCalendarEventCollaboratorCommand : IRequest
    {
        public int EventId { get; set; }
        public string CollaboratorUserId { get; set; } = default!;
    }
}
