using MediatR;

namespace ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarEvent
{
    /// <summary>
    /// Deletes a non-recurring calendar event entirely.
    /// </summary>
    public class DeleteCalendarEventCommand : IRequest
    {
        public int Id { get; set; }
    }
}
