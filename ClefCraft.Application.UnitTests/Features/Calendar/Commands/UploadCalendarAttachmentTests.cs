using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.UploadCalendarAttachment;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Commands
{
    // Sizes come from IFormFile.Length alone, so no real large files are needed.
    public class UploadCalendarAttachmentTests
    {
        private const long MB = 1024 * 1024;

        private static IFormFile File(string name, long length)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(name);
            file.Setup(f => f.Length).Returns(length);
            return file.Object;
        }

        private static UploadCalendarAttachmentCommand Command(params IFormFile[] files) =>
            new() { EventId = 7, UserId = "owner-1", Files = files.ToList() };

        private static FluentValidation.Results.ValidationResult Validate(UploadCalendarAttachmentCommand command) =>
            new UploadCalendarAttachmentCommandValidator().Validate(command);

        [Fact]
        public void Validator_AcceptsUpToTheLimits_AndAnyFileType()
        {
            var files = Enumerable.Range(1, AttachmentLimits.MaxFilesPerUpload)
                .Select(i => File($"part{i}.{(i % 2 == 0 ? "exe" : "gp5")}", 50 * MB))   // 10 × 50 MB = 500 MB
                .ToArray();

            Validate(Command(files)).IsValid.ShouldBeTrue();
            Validate(Command(File("score.pdf", AttachmentLimits.MaxFileSizeBytes))).IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Validator_RejectsAnEmptyUpload()
        {
            Validate(Command()).IsValid.ShouldBeFalse();
            Validate(new UploadCalendarAttachmentCommand { EventId = 7, Files = null! }).IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Validator_RejectsMoreThanTenFiles()
        {
            var files = Enumerable.Range(1, AttachmentLimits.MaxFilesPerUpload + 1).Select(i => File($"f{i}.txt", 1)).ToArray();

            var result = Validate(Command(files));

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.ErrorMessage.Contains("at most 10 files"));
        }

        [Fact]
        public void Validator_RejectsAFileOver100MB()
        {
            var result = Validate(Command(File("take.wav", AttachmentLimits.MaxFileSizeBytes + 1)));

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.ErrorMessage.Contains("\"take.wav\" is larger than 100 MB"));
        }

        [Fact]
        public void Validator_RejectsMoreThan500MBInTotal()
        {
            var files = Enumerable.Range(1, 6).Select(i => File($"take{i}.wav", 90 * MB)).ToArray(); // 540 MB, each under 100 MB

            var result = Validate(Command(files));

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.ErrorMessage.Contains("at most 500 MB in total"));
        }

        [Fact]
        public void Validator_RejectsAnEmptyFile()
        {
            var result = Validate(Command(File("notes.txt", 10), File("blank.txt", 0)));

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(e => e.ErrorMessage.Contains("\"blank.txt\" is empty"));
        }

        [Fact]
        public async Task Handler_InvalidBatch_WritesNoFileAndNoRow()
        {
            var fileService = new Mock<IFileAttachmentService>();
            var attachmentRepo = new Mock<ICalendarEventAttachmentRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var handler = new UploadCalendarAttachmentCommandHandler(
                fileService.Object,
                attachmentRepo.Object,
                MockAccessServices.GetMockCalendarAccessService(authorized: true).Object,
                unitOfWork.Object);

            // One valid file plus one oversized one: nothing at all may be stored.
            var command = Command(File("ok.pdf", 1 * MB), File("huge.wav", AttachmentLimits.MaxFileSizeBytes + 1));

            var ex = await Should.ThrowAsync<BadRequestException>(() => handler.Handle(command, CancellationToken.None));

            ex.ValidationErrors.ShouldNotBeEmpty();
            fileService.Verify(f => f.SaveAttachmentAsync(It.IsAny<int>(), It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
            attachmentRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventAttachment>()), Times.Never);
            unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
