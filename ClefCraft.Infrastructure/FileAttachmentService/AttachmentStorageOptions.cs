namespace ClefCraft.Infrastructure.FileAttachmentService
{
    /// <summary>
    /// Where calendar attachment files live on local disk. Bound from "AttachmentStorage".
    /// RootPath may be absolute or relative to the content root; when unset it defaults to
    /// {contentRoot}/App_Data/calendar-attachments (see InfrastructureServicesRegistration).
    /// </summary>
    public class AttachmentStorageOptions
    {
        public const string SectionName = "AttachmentStorage";

        public string? RootPath { get; set; }
    }
}
