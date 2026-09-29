using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Features.Calendar.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace ClefCraft.Infrastructure.FileAttachmentService
{
    /// <summary>
    /// Stores calendar attachments under AttachmentStorageOptions.RootPath. StoredFilePath is kept
    /// relative to that root ("{eventId}/{guid}{ext}"), and every read or delete goes through
    /// ResolvePath, which refuses anything that resolves outside the root (absolute paths from
    /// before the root was configurable, "../" tricks) — those behave as missing files.
    /// </summary>
    public class FileAttachmentService : IFileAttachmentService
    {
        private static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // Only the extension of the client's file name reaches the disk, and only letters/digits.
        private static readonly Regex SafeExtension = new("^\\.[A-Za-z0-9]{1,16}$", RegexOptions.Compiled);

        private readonly string _root;

        public FileAttachmentService(IOptions<AttachmentStorageOptions> options)
        {
            var rootPath = options.Value.RootPath;
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new InvalidOperationException($"{AttachmentStorageOptions.SectionName}:RootPath is not configured.");

            _root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public async Task<CalendarEventAttachmentDto> SaveAttachmentAsync(int eventId, IFormFile file, string userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            var extension = Path.GetExtension(file.FileName);
            if (!SafeExtension.IsMatch(extension))
                extension = string.Empty;

            var relativePath = $"{eventId}/{Guid.NewGuid()}{extension}";
            var fullPath = ResolvePath(relativePath)!;

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return new CalendarEventAttachmentDto
            {
                FileName = file.FileName,
                StoredFilePath = relativePath,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = userId
            };
        }

        public Task<Stream?> OpenReadAsync(string storedPath)
        {
            var fullPath = ResolvePath(storedPath);

            Stream? stream = fullPath != null && File.Exists(fullPath)
                ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true)
                : null;

            return Task.FromResult(stream);
        }

        public Task DeleteAttachmentFileAsync(string storedPath)
        {
            var fullPath = ResolvePath(storedPath);

            if (fullPath != null && File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }

        /// <summary>The absolute path for a stored path, or null if it lies outside the storage root.</summary>
        private string? ResolvePath(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                return null;

            var fullPath = Path.GetFullPath(Path.Combine(_root, storedPath));

            return fullPath.StartsWith(_root + Path.DirectorySeparatorChar, PathComparison) ? fullPath : null;
        }
    }
}
