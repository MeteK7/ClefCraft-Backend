using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using MediatR;

namespace ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarEvent
{
    public class DeleteCalendarEventCommandHandler
        : IRequestHandler<DeleteCalendarEventCommand>
    {
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly ICalendarEventAttachmentRepository _attachmentRepo;
        private readonly ICalendarEventCollaboratorRepository _collaboratorRepo;
        private readonly INotificationQueueRepository _notificationQueueRepo;
        private readonly IFileAttachmentService _fileService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCalendarEventCommandHandler(
            ICalendarEventRepository calendarEventRepository,
            ICalendarEventAttachmentRepository attachmentRepo,
            ICalendarEventCollaboratorRepository collaboratorRepo,
            INotificationQueueRepository notificationQueueRepo,
            IFileAttachmentService fileService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _calendarEventRepository = calendarEventRepository;
            _attachmentRepo = attachmentRepo;
            _collaboratorRepo = collaboratorRepo;
            _notificationQueueRepo = notificationQueueRepo;
            _fileService = fileService;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            DeleteCalendarEventCommand request,
            CancellationToken cancellationToken)
        {
            var entity = await _calendarEventRepository.GetByIdAsync(request.Id);

            if (entity == null)
                throw new NotFoundException(nameof(CalendarEvent), request.Id);

            if (entity.UserId != _userService.UserId)
                throw new ForbiddenAccessException();

            // Cascade only removes the CalendarEventAttachment DB rows — the physical
            // files on disk are not tracked by EF and must be deleted explicitly, same
            // ordering DeleteAttachmentCommandHandler already uses (file removal before
            // the DB commit, not wrapped in the same rollback boundary).
            var attachments = await _attachmentRepo.GetByEventIdAsync(entity.Id);
            foreach (var attachment in attachments)
            {
                await _fileService.DeleteAttachmentFileAsync(attachment.StoredFilePath);
            }

            // CalendarEventCollaborator and NotificationQueue have no FK/cascade
            // configured, so they must be purged manually before the event is removed.
            await _collaboratorRepo.RemoveAllForEventAsync(entity.Id);
            await _notificationQueueRepo.DeletePendingByEventIdAsync(entity.Id);

            await _calendarEventRepository.DeleteAsync(entity);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
