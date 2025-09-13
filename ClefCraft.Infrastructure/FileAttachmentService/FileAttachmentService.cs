using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Features.Calendar.Queries;
using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Http;    

namespace ClefCraft.Infrastructure.FileAttachmentService
{
    public class FileAttachmentService : IFileAttachmentService
    {
        private readonly string _attachmentsRoot;

        public FileAttachmentService()
        {
            _attachmentsRoot = @"C:\Projects\Backend\Files\CalendarAttachments";
            Directory.CreateDirectory(_attachmentsRoot);
        }

        public async Task<CalendarEventAttachmentDto> SaveAttachmentAsync(int eventId, IFormFile file, string userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            var folderPath = Path.Combine(_attachmentsRoot, eventId.ToString());
            Directory.CreateDirectory(folderPath);

            var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(folderPath, safeFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new CalendarEventAttachmentDto
            {
                FileName = file.FileName,
                StoredFilePath = fullPath,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = userId
            };
        }

        public Task DeleteAttachmentFileAsync(string relativeOrFullPath)
        {
            var path = relativeOrFullPath;

            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }
    }
}
