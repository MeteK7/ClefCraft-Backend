using ClefCraft.Application.Features.Calendar.Queries;
using Microsoft.AspNetCore.Http;

namespace ClefCraft.Application.Contracts.FileAttachment
{
    public interface IFileAttachmentService
    {
        /// <summary>Writes the file to storage; the returned StoredFilePath is relative to the storage root.</summary>
        Task<CalendarEventAttachmentDto> SaveAttachmentAsync(int eventId, IFormFile file, string userId);

        /// <summary>Opens a stored file for reading, or null if it is missing or lies outside the storage root.</summary>
        Task<Stream?> OpenReadAsync(string storedPath);

        /// <summary>Deletes a stored file; a missing file, or a path outside the storage root, is ignored.</summary>
        Task DeleteAttachmentFileAsync(string storedPath);
    }
}
