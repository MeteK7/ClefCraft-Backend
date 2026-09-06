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
        private readonly IBoardMemberRepository _boardMemberRepository;

        public CalendarAccessService(
            ICalendarEventRepository eventRepository,
            ICalendarEventAttachmentRepository attachmentRepository,
            IBoardMemberRepository boardMemberRepository)
        {
            _eventRepository = eventRepository;
            _attachmentRepository = attachmentRepository;
            _boardMemberRepository = boardMemberRepository;
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

        public async Task EnsureCanCommentOnEventAsync(int eventId, string userId)
        {
            var calendarEvent = await _eventRepository.GetByIdReadOnlyAsync(eventId);

            if (calendarEvent == null)
                throw new NotFoundException(nameof(CalendarEvent), eventId);

            if (calendarEvent.UserId == userId)
                return;

            if (!await _boardMemberRepository.ShareAnyBoardAsync(userId, calendarEvent.UserId))
                throw new ForbiddenAccessException();
        }
    }
}
