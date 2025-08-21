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
        private readonly IFileAttachmentService _fileService;

        public DeleteAttachmentCommandHandler(
            ICalendarEventAttachmentRepository repo,
            IFileAttachmentService fileService)
        {
            _repo = repo;
            _fileService = fileService;
        }

        public async Task<Unit> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(request.Id);
            if (entity == null) return Unit.Value;

            await _fileService.DeleteAttachmentFileAsync(entity.StoredFilePath);
            await _repo.DeleteAsync(entity);
            return Unit.Value;
        }
    }
}