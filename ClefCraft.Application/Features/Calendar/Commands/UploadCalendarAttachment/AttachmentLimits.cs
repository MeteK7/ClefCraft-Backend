namespace ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment
{
    /// <summary>
    /// Upload limits for calendar attachments. Any file type is accepted: downloads are always
    /// sent as attachments with a server-chosen content type and nosniff. The frontend mirrors
    /// these values in src/app/shared/attachment-limits.ts.
    /// </summary>
    public static class AttachmentLimits
    {
        public const int MaxFilesPerUpload = 10;
        public const long MaxFileSizeBytes = 100L * 1024 * 1024;   // 100 MB
        public const long MaxUploadSizeBytes = 500L * 1024 * 1024; // 500 MB per request

        /// <summary>
        /// Request body limit for the upload endpoint: the file limit plus room for multipart
        /// headers/boundaries, so an allowed upload is never cut off by Kestrel first.
        /// </summary>
        public const long MaxRequestBodyBytes = MaxUploadSizeBytes + 1024 * 1024;
    }
}
