using ClefCraft.Application.Features.Calendar.Queries;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Contracts.FileAttachment
{
    public interface IFileAttachmentService
    {
        Task<CalendarEventAttachmentDto> SaveAttachmentAsync(int eventId, IFormFile file, string userId);
        Task DeleteAttachmentFileAsync(string relativePath);
    }
}
