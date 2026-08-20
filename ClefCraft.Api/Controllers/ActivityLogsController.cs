using ClefCraft.Application.Common.Models;
using ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity;
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
    }
}
