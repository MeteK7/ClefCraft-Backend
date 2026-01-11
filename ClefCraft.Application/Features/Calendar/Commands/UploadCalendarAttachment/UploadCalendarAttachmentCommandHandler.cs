using AutoMapper;
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

namespace ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment
{
    public class UploadCalendarAttachmentCommandHandler : IRequestHandler<UploadCalendarAttachmentCommand, List<CalendarEventAttachmentDto>>
    {
        private readonly IFileAttachmentService _fileService;
        private readonly ICalendarEventAttachmentRepository _attachmentRepo;

        public UploadCalendarAttachmentCommandHandler(
            IFileAttachmentService fileService,
            ICalendarEventAttachmentRepository attachmentRepo)
        {
            _fileService = fileService;
            _attachmentRepo = attachmentRepo;
        }

        public async Task<List<CalendarEventAttachmentDto>> Handle(UploadCalendarAttachmentCommand request, CancellationToken cancellationToken)
        {
            var uploaded = new List<CalendarEventAttachmentDto>();

            foreach (var file in request.Files)
            {
                // 1️⃣ Save file to disk
                var dto = await _fileService.SaveAttachmentAsync(request.EventId, file, request.UserId);

                // 2️⃣ Save record to database
                var entity = new ClefCraft.Domain.CalendarEventAttachment
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

                // 3️⃣ Return DTO with database-generated Id
                dto.Id = entity.Id;
                uploaded.Add(dto);
            }

            return uploaded;
        }
    }
}