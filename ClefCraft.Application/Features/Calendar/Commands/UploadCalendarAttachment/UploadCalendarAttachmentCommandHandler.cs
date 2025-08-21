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

        public UploadCalendarAttachmentCommandHandler(IFileAttachmentService fileService)
        {
            _fileService = fileService;
        }

        public async Task<List<CalendarEventAttachmentDto>> Handle(UploadCalendarAttachmentCommand request, CancellationToken cancellationToken)
        {
            var uploaded = new List<CalendarEventAttachmentDto>();

            foreach (var file in request.Files)
            {
                var result = await _fileService.SaveAttachmentAsync(request.EventId, file, request.UserId);
                uploaded.Add(result);
            }

            return uploaded;
        }
    }
}
