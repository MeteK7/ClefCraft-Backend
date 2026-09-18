using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using MediatR;

namespace ClefCraft.Application.Features.Calendar.Commands.DeleteSeries
{
    public class DeleteSeriesCommandHandler
        : IRequestHandler<DeleteSeriesCommand>
    {
        private readonly IRecurrenceSeriesRepository _seriesRepo;
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly ICalendarEventExceptionRepository _exceptionRepo;
        private readonly ICalendarEventAttachmentRepository _attachmentRepo;
        private readonly ICalendarEventCollaboratorRepository _collaboratorRepo;
        private readonly INotificationQueueRepository _notificationQueueRepo;
        private readonly IFileAttachmentService _fileService;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteSeriesCommandHandler(
            IRecurrenceSeriesRepository seriesRepo,
            ICalendarEventRepository calendarEventRepository,
            ICalendarEventExceptionRepository exceptionRepo,
            ICalendarEventAttachmentRepository attachmentRepo,
            ICalendarEventCollaboratorRepository collaboratorRepo,
            INotificationQueueRepository notificationQueueRepo,
            IFileAttachmentService fileService,
            ICalendarAccessService calendarAccessService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _seriesRepo = seriesRepo;
            _calendarEventRepository = calendarEventRepository;
            _exceptionRepo = exceptionRepo;
            _attachmentRepo = attachmentRepo;
            _collaboratorRepo = collaboratorRepo;
            _notificationQueueRepo = notificationQueueRepo;
            _fileService = fileService;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            DeleteSeriesCommand request,
            CancellationToken cancellationToken)
        {
            await _calendarAccessService.EnsureSeriesOwnedByUserAsync(request.SeriesUid, _userService.UserId);

            var series = await _seriesRepo.GetBySeriesUidAsync(request.SeriesUid);

            if (series == null)
                throw new NotFoundException(nameof(RecurrenceSeries), request.SeriesUid);

            // Load and validate the root event BEFORE marking anything for deletion, so a
            // missing root row is caught before the series delete is even scheduled. All
            // deletes below still land in a single SaveChangesAsync — this is ordering of
            // in-memory ChangeTracker operations, not a separate transaction.
            var rootEvent = await _calendarEventRepository.GetBySeriesUidAsync(request.SeriesUid);

            if (rootEvent == null)
                throw new NotFoundException(nameof(CalendarEvent), request.SeriesUid);

            // Cascade only removes the CalendarEventAttachment DB rows — the physical
            // files on disk must be deleted explicitly, same ordering
            // DeleteAttachmentCommandHandler already uses (file removal before the DB
            // commit, not wrapped in the same rollback boundary).
            var attachments = await _attachmentRepo.GetByEventIdAsync(rootEvent.Id);
            foreach (var attachment in attachments)
            {
                await _fileService.DeleteAttachmentFileAsync(attachment.StoredFilePath);
            }

            // CalendarEventCollaborator and NotificationQueue have no FK/cascade
            // configured, so they must be purged manually before the event is removed.
            await _collaboratorRepo.RemoveAllForEventAsync(rootEvent.Id);
            await _notificationQueueRepo.DeletePendingByEventIdAsync(rootEvent.Id);

            // Deleting the RecurrenceSeries cascades its CalendarEventSegment rows.
            await _seriesRepo.DeleteAsync(series);

            await _exceptionRepo.DeleteAllForSeriesAsync(request.SeriesUid);

            await _calendarEventRepository.DeleteAsync(rootEvent);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
