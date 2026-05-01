using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarAttachment;
using ClefCraft.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.UpdateSingleOccurrence;
using ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Application.Features.Calendar.Queries.GetCalendarAttachments;
using ClefCraft.Identity.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClefCraft.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _env;

        public CalendarController(
            IMediator mediator,
            IUserService userService,
            IWebHostEnvironment env)
        {
            _mediator = mediator;
            _userService = userService;
            _env = env;
        }

        [HttpPost]
        public async Task<ActionResult<CalendarEventDto>> CreateEvent(
            [FromBody] CreateCalendarEventCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CalendarEventDto>> UpdateEvent(
            int id,
            [FromBody] UpdateCalendarEventCommand command)
        {
            if (id != command.Id)
                return BadRequest();

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("events")]
        public async Task<ActionResult<List<CalendarEventDto>>> GetEvents(
            [FromQuery] DateTimeOffset rangeStart,
            [FromQuery] DateTimeOffset rangeEnd)
        {
            var result = await _mediator.Send(new GetCalendarEventsQuery
            {
                UserId = _userService.UserId,
                RangeStart = rangeStart,
                RangeEnd = rangeEnd
            });
            return Ok(result);
        }

        [HttpGet("event-types")]
        public async Task<ActionResult<List<EventTypeDto>>> GetEventTypes()
        {
            var result = await _mediator.Send(new GetEventTypesQuery
            {
                UserId = _userService.UserId
            });
            return Ok(result);
        }

        [HttpGet("work-history/{itemId}")]
        public async Task<ActionResult<List<WorkHistoryDto>>> GetWorkHistory(int itemId)
        {
            var result = await _mediator.Send(new GetWorkHistoryQuery { ItemId = itemId });
            return Ok(result);
        }

        [HttpPost("{eventId}/attachments")]
        public async Task<IActionResult> UploadAttachment(
            int eventId,
            [FromForm] List<IFormFile> files)
        {
            var command = new UploadCalendarAttachmentCommand
            {
                EventId = eventId,
                Files = files,
                UserId = _userService.UserId
            };
            var uploaded = await _mediator.Send(command);
            return Ok(uploaded);
        }

        [HttpGet("{eventId}/attachments")]
        public async Task<IActionResult> GetAttachments(int eventId)
        {
            var result = await _mediator.Send(new GetAttachmentsQuery { EventId = eventId });
            return Ok(result);
        }

        [HttpGet("attachments/download/{id}")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _mediator.Send(new GetAttachmentByIdQuery { Id = id });
            if (attachment == null)
                return NotFound();

            // Guard against path traversal: resolve and verify the path sits inside
            // the expected uploads directory before opening the file.
            var uploadsRoot = Path.GetFullPath(
                Path.Combine(_env.ContentRootPath, "uploads"));
            var fullPath = Path.GetFullPath(attachment.StoredFilePath);

            if (!fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid file path.");
            }

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var memory = new MemoryStream();
            await using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, attachment.ContentType, attachment.FileName);
        }

        [HttpDelete("attachments/{id}")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            await _mediator.Send(new DeleteAttachmentCommand { Id = id });
            return NoContent();
        }

        [HttpPut("occurrence")]
        public async Task<IActionResult> UpdateSingleOccurrence(
            [FromBody] UpdateSingleOccurrenceCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }
    }
}