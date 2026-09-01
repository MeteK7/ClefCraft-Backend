using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarAttachment;
using ClefCraft.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.UpdateFromOccurrence;
using ClefCraft.Application.Features.Calendar.Commands.UpdateSeries;
using ClefCraft.Application.Features.Calendar.Commands.UpdateSingleOccurrence;
using ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Application.Features.Calendar.Queries.GetCalendarAttachments;
using ClefCraft.Identity.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        // ======================================================================
        // SINGLE-EVENT CRUD
        // ======================================================================

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

        // ======================================================================
        // QUERY
        // ======================================================================

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
            var result = await _mediator.Send(
                new GetWorkHistoryQuery { ItemId = itemId });
            return Ok(result);
        }

        // ======================================================================
        // OCCURRENCE-LEVEL RECURRENCE EDITS
        // ======================================================================

        /// <summary>
        /// Edit or cancel a single occurrence without affecting any other
        /// occurrence in the series.
        /// Angular: updateSingleOccurrence()
        /// </summary>
        [HttpPut("occurrence")]
        public async Task<IActionResult> UpdateSingleOccurrence(
            [FromBody] UpdateSingleOccurrenceCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// "This and following" — splits the series at the given occurrence
        /// and applies new properties to everything from that point onward.
        /// Angular: updateFromOccurrence()
        /// </summary>
        [HttpPut("occurrence/from")]
        public async Task<IActionResult> UpdateFromOccurrence(
            [FromBody] UpdateFromOccurrenceCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        // ======================================================================
        // SERIES-LEVEL RECURRENCE EDITS
        // ======================================================================

        /// <summary>
        /// Update all occurrences AND clear per-occurrence exceptions.
        /// Use when the user accepts that their individual overrides will be lost.
        /// Angular: updateSeriesOverrideAll()
        /// </summary>
        [HttpPut("series/override-all")]
        public async Task<IActionResult> UpdateSeriesOverrideAll(
            [FromBody] UpdateSeriesOverrideAllCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Update series-level defaults while keeping per-occurrence
        /// CalendarEventException overrides intact.
        /// Angular: updateSeriesPreserveExceptions()
        /// </summary>
        [HttpPut("series/preserve-exceptions")]
        public async Task<IActionResult> UpdateSeriesPreserveExceptions(
            [FromBody] UpdateSeriesPreserveExceptionsCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        // ======================================================================
        // ATTACHMENTS
        // ======================================================================

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
            var result = await _mediator.Send(
                new GetAttachmentsQuery { EventId = eventId, UserId = _userService.UserId });
            return Ok(result);
        }

        [HttpGet("attachments/download/{id}")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await _mediator.Send(
                new GetAttachmentByIdQuery { Id = id, UserId = _userService.UserId });

            if (attachment == null)
                return NotFound();

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
            await using (var stream = new FileStream(
                fullPath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, attachment.ContentType, attachment.FileName);
        }

        [HttpDelete("attachments/{id}")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            await _mediator.Send(new DeleteAttachmentCommand { Id = id, UserId = _userService.UserId });
            return NoContent();
        }
    }
}