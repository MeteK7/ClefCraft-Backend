using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Persistence;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarAttachment
{
    public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand>
    {
        private readonly ICalendarEventAttachmentRepository _repo;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IFileAttachmentService _fileService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAttachmentCommandHandler(
            ICalendarEventAttachmentRepository repo,
            ICalendarAccessService calendarAccessService,
            IFileAttachmentService fileService,
            IUnitOfWork unitOfWork)
        {
            _repo = repo;
            _calendarAccessService = calendarAccessService;
            _fileService = fileService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(request.Id);
            if (entity == null) return Unit.Value;

            await _calendarAccessService.EnsureEventOwnedByUserAsync(entity.CalendarEventId, request.UserId);

            await _fileService.DeleteAttachmentFileAsync(entity.StoredFilePath);
            await _repo.DeleteAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}