using FluentValidation;

namespace ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment
{
    public class UploadCalendarAttachmentCommandValidator : AbstractValidator<UploadCalendarAttachmentCommand>
    {
        private const long BytesPerMegabyte = 1024 * 1024;

        public UploadCalendarAttachmentCommandValidator()
        {
            RuleFor(c => c.Files)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Select at least one file to upload.")
                .Must(files => files.Count <= AttachmentLimits.MaxFilesPerUpload)
                .WithMessage($"You can upload at most {AttachmentLimits.MaxFilesPerUpload} files at a time.")
                .Must(files => files.Sum(f => f?.Length ?? 0) <= AttachmentLimits.MaxUploadSizeBytes)
                .WithMessage($"An upload can be at most {AttachmentLimits.MaxUploadSizeBytes / BytesPerMegabyte} MB in total.");

            RuleForEach(c => c.Files)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Invalid file.")
                .Must(f => f.Length > 0).WithMessage((_, f) => $"\"{f?.FileName}\" is empty.")
                .Must(f => f.Length <= AttachmentLimits.MaxFileSizeBytes)
                .WithMessage((_, f) => $"\"{f?.FileName}\" is larger than {AttachmentLimits.MaxFileSizeBytes / BytesPerMegabyte} MB.");
        }
    }
}
