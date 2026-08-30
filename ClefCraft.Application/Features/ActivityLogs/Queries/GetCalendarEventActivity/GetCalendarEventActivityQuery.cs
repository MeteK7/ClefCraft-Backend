using ClefCraft.Application.Common.Models;
using MediatR;

namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetCalendarEventActivity
{
    public class GetCalendarEventActivityQuery : IRequest<PagedResult<CalendarActivityLogEntryDto>>
    {
        public int EventId { get; set; }

        // Present only for recurring events; when set, Segment- and Exception-scoped entries for
        // this series are merged in alongside the root CalendarEvent's own entries.
        public string? SeriesUid { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
