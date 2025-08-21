using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Features.Calendar.Queries; // For CalendarEventAttachmentDto
using Microsoft.AspNetCore.Hosting; // ✅ Required
using Microsoft.AspNetCore.Http;    // For IFormFile

namespace ClefCraft.Infrastructure.FileAttachmentService
{
    public class FileAttachmentService : IFileAttachmentService
    {
        private readonly IHostingEnvironment _env;

        public FileAttachmentService(IHostingEnvironment env)
        {
            _env = env;
        }

        public async Task<CalendarEventAttachmentDto> SaveAttachmentAsync(int eventId, IFormFile file, string userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            var folderPath = Path.Combine(_env.WebRootPath, "uploads", "calendar", eventId.ToString());
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
                StoredFilePath = Path.Combine("uploads", "calendar", eventId.ToString(), safeFileName).Replace("\\", "/"),
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = userId
            };
        }

        public Task DeleteAttachmentFileAsync(string relativePath)
        {
            var fullPath = Path.Combine(_env.WebRootPath, relativePath);
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }
    }
}
