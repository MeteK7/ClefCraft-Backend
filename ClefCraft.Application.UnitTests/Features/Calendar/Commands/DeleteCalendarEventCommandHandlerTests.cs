using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Commands
{
    public class DeleteCalendarEventCommandHandlerTests
    {
        private const string CallerUserId = "user-1";

        private static (
            DeleteCalendarEventCommandHandler Handler,
            Mock<ICalendarEventRepository> EventRepo,
            Mock<ICalendarEventAttachmentRepository> AttachmentRepo,
            Mock<ICalendarEventCollaboratorRepository> CollaboratorRepo,
            Mock<INotificationQueueRepository> NotificationRepo,
            Mock<IFileAttachmentService> FileService
        ) MakeHandler(CalendarEvent? entity)
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);

            var attachmentRepo = new Mock<ICalendarEventAttachmentRepository>();
            attachmentRepo.Setup(r => r.GetByEventIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<CalendarEventAttachment>());

            var collaboratorRepo = new Mock<ICalendarEventCollaboratorRepository>();
            var notificationRepo = new Mock<INotificationQueueRepository>();
            var fileService = new Mock<IFileAttachmentService>();

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new DeleteCalendarEventCommandHandler(
                eventRepo.Object,
                attachmentRepo.Object,
                collaboratorRepo.Object,
                notificationRepo.Object,
                fileService.Object,
                userService.Object,
                unitOfWork.Object);

            return (handler, eventRepo, attachmentRepo, collaboratorRepo, notificationRepo, fileService);
        }

        [Fact]
        public async Task Handle_ExistingOwnedEvent_DeletesEventAndPurgesUnCascadedDependencies()
        {
            var entity = new CalendarEvent { Id = 42, UserId = CallerUserId, Subject = "One-off" };
            var (handler, eventRepo, attachmentRepo, collaboratorRepo, notificationRepo, _) = MakeHandler(entity);

            await handler.Handle(new DeleteCalendarEventCommand { Id = 42 }, CancellationToken.None);

            eventRepo.Verify(r => r.DeleteAsync(entity), Times.Once);
            collaboratorRepo.Verify(r => r.RemoveAllForEventAsync(42), Times.Once);
            notificationRepo.Verify(r => r.DeletePendingByEventIdAsync(42), Times.Once);
            attachmentRepo.Verify(r => r.GetByEventIdAsync(42), Times.Once);
        }

        [Fact]
        public async Task Handle_EventWithAttachments_DeletesEachPhysicalFileBeforeRemovingTheEventRow()
        {
            var entity = new CalendarEvent { Id = 42, UserId = CallerUserId, Subject = "One-off" };
            var (handler, eventRepo, attachmentRepo, _, _, fileService) = MakeHandler(entity);

            var attachments = new List<CalendarEventAttachment>
            {
                new CalendarEventAttachment { Id = 1, CalendarEventId = 42, StoredFilePath = @"C:\files\a.pdf" },
                new CalendarEventAttachment { Id = 2, CalendarEventId = 42, StoredFilePath = @"C:\files\b.pdf" },
            };
            attachmentRepo.Setup(r => r.GetByEventIdAsync(42)).ReturnsAsync(attachments);

            var callOrder = new List<string>();
            fileService.Setup(f => f.DeleteAttachmentFileAsync(It.IsAny<string>()))
                .Callback(() => callOrder.Add("file"))
                .Returns(Task.CompletedTask);
            eventRepo.Setup(r => r.DeleteAsync(entity))
                .Callback(() => callOrder.Add("event"))
                .Returns(Task.CompletedTask);

            await handler.Handle(new DeleteCalendarEventCommand { Id = 42 }, CancellationToken.None);

            fileService.Verify(f => f.DeleteAttachmentFileAsync(@"C:\files\a.pdf"), Times.Once);
            fileService.Verify(f => f.DeleteAttachmentFileAsync(@"C:\files\b.pdf"), Times.Once);
            callOrder.ShouldBe(new[] { "file", "file", "event" });
        }

        [Fact]
        public async Task Handle_EventNotFound_ThrowsNotFoundException()
        {
            var (handler, eventRepo, _, _, _, _) = MakeHandler(entity: null);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new DeleteCalendarEventCommand { Id = 999 }, CancellationToken.None));

            eventRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEvent>()), Times.Never);
        }

        [Fact]
        public async Task Handle_RepeatDelete_ThrowsNotFoundException_MatchingUpdateCalendarEventConvention()
        {
            // Once an event is gone, a second delete call must 404 — the same convention
            // UpdateCalendarEventCommandHandler already uses for a missing entity, not the
            // silent-success idempotency DeleteAttachmentCommandHandler uses for attachments.
            var (handler, _, _, _, _, _) = MakeHandler(entity: null);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new DeleteCalendarEventCommand { Id = 42 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_EventOwnedBySomeoneElse_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var entity = new CalendarEvent { Id = 42, UserId = "someone-else", Subject = "One-off" };
            var (handler, eventRepo, _, collaboratorRepo, notificationRepo, _) = MakeHandler(entity);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new DeleteCalendarEventCommand { Id = 42 }, CancellationToken.None));

            eventRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEvent>()), Times.Never);
            collaboratorRepo.Verify(r => r.RemoveAllForEventAsync(It.IsAny<int>()), Times.Never);
            notificationRepo.Verify(r => r.DeletePendingByEventIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
