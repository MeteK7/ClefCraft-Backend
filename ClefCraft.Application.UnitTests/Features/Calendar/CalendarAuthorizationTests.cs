using AutoMapper;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarAttachment;
using ClefCraft.Application.Features.Calendar.Commands.UpdateSingleOccurrence;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Application.Features.Calendar.Queries.GetCalendarAttachments;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar
{
    // Regression coverage for the P0 authorization fix across the Calendar
    // domain: occurrence/series edits are keyed by SeriesUid, and ownership
    // for those must resolve back to CalendarEvent.UserId (see
    // ICalendarAccessService); attachments resolve ownership through their
    // parent CalendarEvent; work-history resolves ownership through the
    // linked BoardItem's board.
    public class CalendarAuthorizationTests
    {
        private const string CallerUserId = "user-1";

        [Fact]
        public async Task UpdateSingleOccurrence_SeriesNotOwnedByCaller_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();
            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateSingleOccurrenceCommandHandler(
                exceptionRepo.Object,
                accessService.Object,
                userService.Object,
                new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(
                    new UpdateSingleOccurrenceCommand { SeriesUid = "someone-elses-series", OccurrenceDate = System.DateTimeOffset.UtcNow },
                    CancellationToken.None));

            exceptionRepo.Verify(r => r.UpsertAsync(It.IsAny<CalendarEventException>()), Times.Never);
        }

        [Fact]
        public async Task GetAttachmentById_NotOwnedByCaller_ThrowsForbiddenAccessException()
        {
            var attachmentRepo = new Mock<ICalendarEventAttachmentRepository>();
            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: false);

            var handler = new GetAttachmentByIdQueryHandler(attachmentRepo.Object, accessService.Object, new Mock<IMapper>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetAttachmentByIdQuery { Id = 1, UserId = CallerUserId }, CancellationToken.None));

            attachmentRepo.Verify(r => r.GetByIdReadOnlyAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAttachment_NotOwnedByCaller_ThrowsForbiddenAccessException_BeforeAnyDelete()
        {
            var attachment = new CalendarEventAttachment { Id = 1, CalendarEventId = 42, StoredFilePath = "uploads/1/file.pdf" };
            var attachmentRepo = new Mock<ICalendarEventAttachmentRepository>();
            attachmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(attachment);

            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: false);
            var fileService = new Mock<IFileAttachmentService>();

            var handler = new DeleteAttachmentCommandHandler(
                attachmentRepo.Object,
                accessService.Object,
                fileService.Object,
                new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new DeleteAttachmentCommand { Id = 1, UserId = CallerUserId }, CancellationToken.None));

            attachmentRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEventAttachment>()), Times.Never);
            fileService.Verify(f => f.DeleteAttachmentFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetWorkHistory_ItemNotOwnedByCaller_ThrowsForbiddenAccessException()
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            var boardAccessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new GetWorkHistoryQueryHandler(eventRepo.Object, boardAccessService.Object, new Mock<IMapper>().Object, userService.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetWorkHistoryQuery { ItemId = 123 }, CancellationToken.None));

            eventRepo.Verify(r => r.GetWorkHistoryByItemIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
