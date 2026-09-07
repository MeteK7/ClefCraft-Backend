using MediatR;

namespace ClefCraft.Application.Features.CalendarEventCollaborators.Queries.GetCalendarEventCollaborators
{
    public class GetCalendarEventCollaboratorsQuery : IRequest<List<CalendarEventCollaboratorDto>>
    {
        public int EventId { get; set; }
    }
}
