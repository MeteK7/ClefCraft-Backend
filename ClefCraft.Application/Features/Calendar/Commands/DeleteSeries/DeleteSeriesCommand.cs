using MediatR;

namespace ClefCraft.Application.Features.Calendar.Commands.DeleteSeries
{
    /// <summary>
    /// Deletes an entire recurring event: every segment, every per-occurrence
    /// exception, the RecurrenceSeries itself, and the root CalendarEvent row.
    /// </summary>
    public class DeleteSeriesCommand : IRequest
    {
        public string SeriesUid { get; set; }
    }
}
