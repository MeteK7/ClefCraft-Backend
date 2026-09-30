using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Features.CalendarEventCollaborators;
using ClefCraft.Application.Features.CalendarEventCollaborators.Commands.RemoveCalendarEventCollaborator;
using ClefCraft.Application.Features.CalendarEventCollaborators.Queries.GetCalendarEventCollaborators;
using ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarAttachment;
using ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.DeleteFromOccurrence;
using ClefCraft.Application.Features.Calendar.Commands.DeleteSeries;
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
using Microsoft.AspNetCore.StaticFiles;

namespace ClefCraft.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IUserService _userService;
        private readonly IFileAttachmentService _fileService;

        private static readonly FileExtensionContentTypeProvider ContentTypes = new();

        public CalendarController(
            IMediator mediator,
            IUserService userService,
            IFileAttachmentService fileService)
        {
            _mediator = mediator;
            _userService = userService;
            _fileService = fileService;
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            await _mediator.Send(new DeleteCalendarEventCommand { Id = id });
            return NoContent();
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

        /// <summary>
        /// Deletes (cancels) a single occurrence without affecting any other
        /// occurrence in the series. Reuses UpdateSingleOccurrenceCommand's
        /// existing IsCancelled path — the exception-upsert-based
        /// cancellation this forces already fully implements a single-
        /// occurrence delete (RecurrenceHelper.ApplyException already
        /// filters cancelled occurrences out of projection).
        /// Angular: deleteSingleOccurrence()
        /// </summary>
        [HttpDelete("occurrence")]
        public async Task<IActionResult> DeleteSingleOccurrence(
            [FromBody] UpdateSingleOccurrenceCommand command)
        {
            command.IsCancelled = true;
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// "This and following" delete — removes this occurrence and every
        /// occurrence from it onward, leaving earlier occurrences untouched.
        /// Angular: deleteFromOccurrence()
        /// </summary>
        [HttpDelete("occurrence/from")]
        public async Task<IActionResult> DeleteFromOccurrence(
            [FromBody] DeleteFromOccurrenceCommand command)
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

        /// <summary>
        /// Deletes an entire recurring event: every segment, every
        /// per-occurrence exception, the RecurrenceSeries itself, and the
        /// root CalendarEvent row.
        /// Angular: deleteSeries()
        /// </summary>
        [HttpDelete("series")]
        public async Task<IActionResult> DeleteSeries(
            [FromBody] DeleteSeriesCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        // ======================================================================
        // ATTACHMENTS
        // ======================================================================

        // Just above AttachmentLimits.MaxUploadSizeBytes, so an allowed upload always reaches the
        // validator (which gives per-file 400s) and only a truly oversized body is refused with 413.
        [HttpPost("{eventId}/attachments")]
        [RequestSizeLimit(AttachmentLimits.MaxRequestBodyBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = AttachmentLimits.MaxRequestBodyBytes)]
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

            // Null when the file is gone or its stored path lies outside the storage root.
            var stream = await _fileService.OpenReadAsync(attachment.StoredFilePath);
            if (stream == null)
                return NotFound();

            // Any file type can be uploaded, so never echo the uploader's Content-Type: derive it
            // from the extension, force a download (File(..., fileName) sends
            // Content-Disposition: attachment) and forbid MIME sniffing.
            if (!ContentTypes.TryGetContentType(attachment.FileName, out var contentType))
                contentType = "application/octet-stream";

            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(stream, contentType, attachment.FileName);
        }

        [HttpDelete("attachments/{id}")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            await _mediator.Send(new DeleteAttachmentCommand { Id = id, UserId = _userService.UserId });
            return NoContent();
        }

        // ======================================================================
        // COLLABORATORS
        // ======================================================================

        [HttpGet("{eventId}/collaborators")]
        public async Task<ActionResult<List<CalendarEventCollaboratorDto>>> GetCollaborators(int eventId)
        {
            var result = await _mediator.Send(new GetCalendarEventCollaboratorsQuery { EventId = eventId });
            return Ok(result);
        }

        [HttpDelete("{eventId}/collaborators/{collaboratorUserId}")]
        public async Task<IActionResult> RemoveCollaborator(int eventId, string collaboratorUserId)
        {
            await _mediator.Send(new RemoveCalendarEventCollaboratorCommand
            {
                EventId = eventId,
                CollaboratorUserId = collaboratorUserId
            });
            return NoContent();
        }
    }
}