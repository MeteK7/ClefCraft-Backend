using ClefCraft.Application.Common.Models;
using ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity;
using ClefCraft.Application.Features.ActivityLogs.Queries.GetCalendarEventActivity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClefCraft.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityLogsController : Controller
    {
        private readonly IMediator _mediator;

        public ActivityLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/ActivityLogs/BoardItem/88?pageNumber=1&pageSize=20
        [HttpGet("{entityType}/{entityId}")]
        public async Task<ActionResult<PagedResult<ActivityLogEntryDto>>> GetActivityLog(
            string entityType, int entityId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetActivityLogForEntityQuery
            {
                EntityType = entityType,
                EntityId = entityId,
                PageNumber = pageNumber,
                PageSize = pageSize
            });

            return Ok(result);
        }

        // GET: api/ActivityLogs/calendar-event/42?seriesUid=abc-123&pageNumber=1&pageSize=20
        // Distinct from the generic {entityType}/{entityId} route above: a calendar event's
        // "history" isn't a single entity's diff — recurring-event edits land on
        // CalendarEventSegment/CalendarEventException rows, never the root CalendarEvent, so this
        // merges all three sources for the series instead of forcing Calendar through the
        // single-entity shape.
        [HttpGet("calendar-event/{eventId}")]
        public async Task<ActionResult<PagedResult<CalendarActivityLogEntryDto>>> GetCalendarEventActivity(
            int eventId, [FromQuery] string? seriesUid, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetCalendarEventActivityQuery
            {
                EventId = eventId,
                SeriesUid = seriesUid,
                PageNumber = pageNumber,
                PageSize = pageSize
            });

            return Ok(result);
        }
    }
}
