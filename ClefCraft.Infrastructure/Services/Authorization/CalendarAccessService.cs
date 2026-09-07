using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;

namespace ClefCraft.Infrastructure.Services.Authorization
{
    public class CalendarAccessService : ICalendarAccessService
    {
        private readonly ICalendarEventRepository _eventRepository;
        private readonly ICalendarEventAttachmentRepository _attachmentRepository;
        private readonly ICalendarEventCollaboratorRepository _collaboratorRepository;

        public CalendarAccessService(
            ICalendarEventRepository eventRepository,
            ICalendarEventAttachmentRepository attachmentRepository,
            ICalendarEventCollaboratorRepository collaboratorRepository)
        {
            _eventRepository = eventRepository;
            _attachmentRepository = attachmentRepository;
            _collaboratorRepository = collaboratorRepository;
        }

        public async Task EnsureEventOwnedByUserAsync(int eventId, string userId)
        {
            var calendarEvent = await _eventRepository.GetByIdReadOnlyAsync(eventId);

            if (calendarEvent == null)
                throw new NotFoundException(nameof(CalendarEvent), eventId);

            if (calendarEvent.UserId != userId)
                throw new ForbiddenAccessException();
        }

        public async Task EnsureSeriesOwnedByUserAsync(string seriesUid, string userId)
        {
            var calendarEvent = await _eventRepository.GetBySeriesUidAsync(seriesUid);

            if (calendarEvent == null)
                throw new NotFoundException(nameof(CalendarEvent), seriesUid);

            if (calendarEvent.UserId != userId)
                throw new ForbiddenAccessException();
        }

        public async Task EnsureAttachmentOwnedByUserAsync(int attachmentId, string userId)
        {
            var attachment = await _attachmentRepository.GetByIdReadOnlyAsync(attachmentId);

            if (attachment == null)
                throw new NotFoundException(nameof(CalendarEventAttachment), attachmentId);

            await EnsureEventOwnedByUserAsync(attachment.CalendarEventId, userId);
        }

        public async Task EnsureCanAccessEventAsync(int eventId, string userId)
        {
            var calendarEvent = await _eventRepository.GetByIdReadOnlyAsync(eventId);

            if (calendarEvent == null)
                throw new NotFoundException(nameof(CalendarEvent), eventId);

            if (calendarEvent.UserId == userId)
                return;

            if (!await _collaboratorRepository.IsCollaboratorAsync(eventId, userId))
                throw new ForbiddenAccessException();
        }

        public async Task EnsureCanAccessAttachmentAsync(int attachmentId, string userId)
        {
            var attachment = await _attachmentRepository.GetByIdReadOnlyAsync(attachmentId);

            if (attachment == null)
                throw new NotFoundException(nameof(CalendarEventAttachment), attachmentId);

            await EnsureCanAccessEventAsync(attachment.CalendarEventId, userId);
        }

        public async Task<List<string>> GrantCollaboratorAccessAsync(int eventId, string granterId, IEnumerable<string> targetUserIds)
        {
            var calendarEvent = await _eventRepository.GetByIdReadOnlyAsync(eventId);

            if (calendarEvent == null)
                throw new NotFoundException(nameof(CalendarEvent), eventId);

            // Only the owner can expand who has access — a defensive second layer behind the
            // caller already restricting non-owner mentions to existing participants.
            if (calendarEvent.UserId != granterId)
                return new List<string>();

            var newlyGranted = new List<string>();

            foreach (var targetUserId in targetUserIds.Distinct())
            {
                if (targetUserId == calendarEvent.UserId)
                    continue; // owner doesn't need a collaborator row

                if (await _collaboratorRepository.IsCollaboratorAsync(eventId, targetUserId))
                    continue; // already granted — idempotent

                await _collaboratorRepository.CreateAsync(new CalendarEventCollaborator
                {
                    CalendarEventId = eventId,
                    UserId = targetUserId,
                    CreatedBy = granterId
                });

                newlyGranted.Add(targetUserId);
            }

            return newlyGranted;
        }
    }
}
