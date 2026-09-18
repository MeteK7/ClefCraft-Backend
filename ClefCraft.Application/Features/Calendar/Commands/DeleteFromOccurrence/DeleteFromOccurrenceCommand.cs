using MediatR;

namespace ClefCraft.Application.Features.Calendar.Commands.DeleteFromOccurrence
{
    /// <summary>
    /// "This and following" delete — removes this occurrence and every
    /// occurrence from it onward, leaving earlier occurrences untouched.
    /// </summary>
    public class DeleteFromOccurrenceCommand : IRequest
    {
        public string SeriesUid { get; set; }

        public DateTimeOffset OccurrenceDate { get; set; }
    }
}
