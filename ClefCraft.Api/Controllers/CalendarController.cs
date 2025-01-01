using MediatR;
using Microsoft.AspNetCore.Mvc;
using ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Queries;

[Route("api/[controller]")]
[ApiController]
public class CalendarController : Controller
{
    private readonly IMediator _mediator;

    public CalendarController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("create")]
    public async Task<ActionResult<CalendarEventDto>> CreateEvent([FromBody] CreateCalendarEventCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpGet("events")]
    public async Task<ActionResult<List<CalendarEventDto>>> GetEvents([FromQuery] string userId)
    {
        var result = await _mediator.Send(new GetCalendarEventsQuery { UserId = userId });
        return Ok(result);
    }

}
