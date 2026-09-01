using AutoMapper;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Calendar;

namespace ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment
{
    public class UploadCalendarAttachmentCommandHandler : IRequestHandler<UploadCalendarAttachmentCommand, List<CalendarEventAttachmentDto>>
    {
        private readonly IFileAttachmentService _fileService;
        private readonly ICalendarEventAttachmentRepository _attachmentRepo;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUnitOfWork _unitOfWork;

        public UploadCalendarAttachmentCommandHandler(
            IFileAttachmentService fileService,
            ICalendarEventAttachmentRepository attachmentRepo,
            ICalendarAccessService calendarAccessService,
            IUnitOfWork unitOfWork)
        {
            _fileService = fileService;
            _attachmentRepo = attachmentRepo;
            _calendarAccessService = calendarAccessService;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<CalendarEventAttachmentDto>> Handle(
            UploadCalendarAttachmentCommand request,
            CancellationToken cancellationToken)
        {
            await _calendarAccessService.EnsureEventOwnedByUserAsync(request.EventId, request.UserId);

            var entities = new List<(CalendarEventAttachmentDto Dto, CalendarEventAttachment Entity)>();

            foreach (var file in request.Files)
            {
                var dto = await _fileService.SaveAttachmentAsync(request.EventId, file, request.UserId);

                var entity = new CalendarEventAttachment
                {
                    CalendarEventId = request.EventId,
                    FileName = dto.FileName,
                    StoredFilePath = dto.StoredFilePath,
                    FileSize = dto.FileSize,
                    ContentType = dto.ContentType,
                    UploadedAt = dto.UploadedAt,
                    UploadedBy = dto.UploadedBy,
                    DateCreated = DateTime.UtcNow,
                    CreatedBy = request.UserId
                };

                await _attachmentRepo.CreateAsync(entity);
                entities.Add((dto, entity));
            }

            // Save all at once so EF assigns real IDs
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Now entity.Id is populated
            foreach (var (dto, entity) in entities)
                dto.Id = entity.Id;

            return entities.Select(x => x.Dto).ToList();
        }
    }
}