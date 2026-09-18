using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.DeleteSeries;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Commands
{
    public class DeleteSeriesCommandHandlerTests
    {
        private const string CallerUserId = "user-1";
        private const string SeriesUid = "series-1";

        private static (
            DeleteSeriesCommandHandler Handler,
            Mock<IRecurrenceSeriesRepository> SeriesRepo,
            Mock<ICalendarEventRepository> EventRepo,
            Mock<ICalendarEventExceptionRepository> ExceptionRepo,
            Mock<ICalendarEventCollaboratorRepository> CollaboratorRepo,
            Mock<INotificationQueueRepository> NotificationRepo
        ) MakeHandler(RecurrenceSeries? series, CalendarEvent? rootEvent, bool authorized = true)
        {
            var seriesRepo = new Mock<IRecurrenceSeriesRepository>();
            seriesRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(series);

            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid)).ReturnsAsync(rootEvent);

            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();
            var attachmentRepo = new Mock<ICalendarEventAttachmentRepository>();
            attachmentRepo.Setup(r => r.GetByEventIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<CalendarEventAttachment>());
            var collaboratorRepo = new Mock<ICalendarEventCollaboratorRepository>();
            var notificationRepo = new Mock<INotificationQueueRepository>();
            var fileService = new Mock<IFileAttachmentService>();
            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized);

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new DeleteSeriesCommandHandler(
                seriesRepo.Object,
                eventRepo.Object,
                exceptionRepo.Object,
                attachmentRepo.Object,
                collaboratorRepo.Object,
                notificationRepo.Object,
                fileService.Object,
                accessService.Object,
                userService.Object,
                unitOfWork.Object);

            return (handler, seriesRepo, eventRepo, exceptionRepo, collaboratorRepo, notificationRepo);
        }

        [Fact]
        public async Task Handle_ExistingSeries_DeletesSeriesExceptionsAndRootEvent()
        {
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, UserId = CallerUserId, Segments = new List<CalendarEventSegment>() };
            var rootEvent = new CalendarEvent { Id = 42, SeriesUid = SeriesUid, UserId = CallerUserId, IsRecurring = true };

            var (handler, seriesRepo, eventRepo, exceptionRepo, collaboratorRepo, notificationRepo) = MakeHandler(series, rootEvent);

            await handler.Handle(new DeleteSeriesCommand { SeriesUid = SeriesUid }, CancellationToken.None);

            seriesRepo.Verify(r => r.DeleteAsync(series), Times.Once);
            exceptionRepo.Verify(r => r.DeleteAllForSeriesAsync(SeriesUid), Times.Once);
            eventRepo.Verify(r => r.DeleteAsync(rootEvent), Times.Once);
            collaboratorRepo.Verify(r => r.RemoveAllForEventAsync(42), Times.Once);
            notificationRepo.Verify(r => r.DeletePendingByEventIdAsync(42), Times.Once);
        }

        [Fact]
        public async Task Handle_RootEventMissing_ThrowsNotFoundException_BeforeDeletingTheSeries()
        {
            // The root event must be loaded and validated BEFORE the series is marked for
            // deletion, so a missing root row is caught before anything is scheduled to change.
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, UserId = CallerUserId, Segments = new List<CalendarEventSegment>() };
            var (handler, seriesRepo, _, _, _, _) = MakeHandler(series, rootEvent: null);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new DeleteSeriesCommand { SeriesUid = SeriesUid }, CancellationToken.None));

            seriesRepo.Verify(r => r.DeleteAsync(It.IsAny<RecurrenceSeries>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SeriesNotFound_ThrowsNotFoundException()
        {
            var (handler, seriesRepo, eventRepo, _, _, _) = MakeHandler(series: null, rootEvent: null);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new DeleteSeriesCommand { SeriesUid = SeriesUid }, CancellationToken.None));

            seriesRepo.Verify(r => r.DeleteAsync(It.IsAny<RecurrenceSeries>()), Times.Never);
            eventRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEvent>()), Times.Never);
        }

        [Fact]
        public async Task Handle_RepeatDelete_ThrowsNotFoundException_ViaEnsureSeriesOwnedByUserAsync()
        {
            // EnsureSeriesOwnedByUserAsync already throws NotFoundException once the series'
            // root event is gone — same convention every other scope command already follows.
            var (handler, _, _, _, _, _) = MakeHandler(series: null, rootEvent: null);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new DeleteSeriesCommand { SeriesUid = SeriesUid }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_SeriesOwnedBySomeoneElse_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var series = new RecurrenceSeries { Id = 5, SeriesUid = SeriesUid, UserId = "someone-else", Segments = new List<CalendarEventSegment>() };
            var rootEvent = new CalendarEvent { Id = 42, SeriesUid = SeriesUid, UserId = "someone-else", IsRecurring = true };
            var (handler, seriesRepo, eventRepo, _, _, _) = MakeHandler(series, rootEvent, authorized: false);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new DeleteSeriesCommand { SeriesUid = SeriesUid }, CancellationToken.None));

            seriesRepo.Verify(r => r.DeleteAsync(It.IsAny<RecurrenceSeries>()), Times.Never);
            eventRepo.Verify(r => r.DeleteAsync(It.IsAny<CalendarEvent>()), Times.Never);
        }
    }
}
