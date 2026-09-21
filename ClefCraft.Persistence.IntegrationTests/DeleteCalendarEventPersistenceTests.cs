using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using ClefCraft.Application.Features.Calendar.Commands.DeleteSeries;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
using ClefCraft.Persistence.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Builds a real ClefCraftDatabaseContext (EF Core InMemory provider) with a mocked
    // IUserService, plus the real repository/access-service implementations, so the
    // delete handlers run for real against real cascade-delete configuration rather
    // than mocked repositories. Verifies actual resulting row state for every entity
    // involved in a calendar-event delete: CalendarEvent, RecurrenceSeries,
    // CalendarEventSegment, CalendarEventException, CalendarEventCollaborator,
    // NotificationQueue and CalendarEventAttachment.
    public class DeleteCalendarEventPersistenceTests
    {
        private const string OwnerUserId = "owner-user";

        // File deletion is faked (no real disk I/O in a test) but the call itself is
        // recorded so the "attachment files are cleaned up" behavior is still verified.
        private class RecordingFileAttachmentService : IFileAttachmentService
        {
            public System.Collections.Generic.List<string> DeletedPaths { get; } = new();

            public Task<CalendarEventAttachmentDto> SaveAttachmentAsync(int eventId, IFormFile file, string userId)
                => throw new NotSupportedException();

            public Task DeleteAttachmentFileAsync(string relativePath)
            {
                DeletedPaths.Add(relativePath);
                return Task.CompletedTask;
            }
        }

        private static async Task<(
            ClefCraftDatabaseContext Context,
            CalendarEvent RootEvent,
            RecurrenceSeries Series,
            RecordingFileAttachmentService FileService
        )> SeedFullRecurringEventGraphAsync(ClefCraftDatabaseContext context)
        {
            var start = DateTimeOffset.UtcNow;
            var seriesUid = Guid.NewGuid().ToString();

            var rootEvent = new CalendarEvent
            {
                Subject = "Weekly sync",
                SeriesUid = seriesUid,
                IsRecurring = true,
                RecurrenceRuleJson = "{\"Frequency\":\"WEEKLY\",\"Interval\":1}",
                StartDate = start,
                EndDate = start.AddHours(1),
                UserId = OwnerUserId
            };
            await context.CalendarEvents.AddAsync(rootEvent);
            await context.SaveChangesAsync();

            var series = new RecurrenceSeries
            {
                UserId = OwnerUserId,
                SeriesUid = seriesUid,
                CreatedAt = DateTime.UtcNow
            };
            await context.RecurrenceSeries.AddAsync(series);
            await context.SaveChangesAsync();

            var segment = new CalendarEventSegment
            {
                RecurrenceSeriesId = series.Id,
                Subject = "Weekly sync",
                EffectiveFrom = start,
                EffectiveTo = null,
                StartDate = start,
                EndDate = start.AddHours(1),
                IsRecurring = true,
                RecurrenceRuleJson = "{\"Frequency\":\"WEEKLY\",\"Interval\":1}"
            };
            await context.CalendarEventSegments.AddAsync(segment);

            var exception = new CalendarEventException
            {
                SeriesUid = seriesUid,
                OccurrenceDate = start.AddDays(7),
                IsCancelled = true
            };
            await context.CalendarEventExceptions.AddAsync(exception);

            var collaborator = new CalendarEventCollaborator
            {
                CalendarEventId = rootEvent.Id,
                UserId = "collaborator-user"
            };
            await context.CalendarEventCollaborators.AddAsync(collaborator);

            var notification = new NotificationQueue
            {
                UserId = OwnerUserId,
                CalendarEventId = rootEvent.Id,
                ScheduledFor = start.AddMinutes(-15),
                IsProcessed = false,
                Message = "Reminder"
            };
            await context.NotificationQueues.AddAsync(notification);

            var attachment = new CalendarEventAttachment
            {
                CalendarEventId = rootEvent.Id,
                FileName = "agenda.pdf",
                StoredFilePath = @"C:\files\agenda.pdf",
                FileSize = 1024,
                ContentType = "application/pdf",
                UploadedAt = DateTime.UtcNow,
                UploadedBy = OwnerUserId
            };
            await context.CalendarEventAttachments.AddAsync(attachment);

            await context.SaveChangesAsync();

            // Detach everything seeded above so the handler under test starts from a clean
            // tracking slate, exactly as it would against a fresh per-request DbContext —
            // otherwise re-fetching the same rows through the repositories below collides
            // with the still-tracked seed instances (EF throws on the duplicate identity).
            context.ChangeTracker.Clear();

            var fileService = new RecordingFileAttachmentService();
            return (context, rootEvent, series, fileService);
        }

        [Fact]
        public async Task DeleteSeries_RemovesEverySeededDependency_AndDeletesAttachmentFiles()
        {
            var context = DatabaseContextFactory.CreateContext(OwnerUserId);
            var (_, rootEvent, series, fileService) = await SeedFullRecurringEventGraphAsync(context);
            var seriesUid = rootEvent.SeriesUid;
            var eventId = rootEvent.Id;

            var eventRepo = new CalendarEventRepository(context);
            var seriesRepo = new RecurrenceSeriesRepository(context);
            var segmentRepo = new CalendarEventSegmentRepository(context);
            var exceptionRepo = new CalendarEventExceptionRepository(context);
            var attachmentRepo = new CalendarEventAttachmentRepository(context);
            var collaboratorRepo = new CalendarEventCollaboratorRepository(context);
            var unitOfWork = new EfUnitOfWork(context);
            var notificationRepo = new NotificationQueueRepository(context, unitOfWork);
            // Authorization is already thoroughly covered by the mocked-repo handler unit
            // tests — this test is scoped to real persistence/cascade behavior, so the
            // access check itself is mocked authorized rather than pulling in
            // ClefCraft.Infrastructure just for CalendarAccessService.
            var accessService = new Mock<ICalendarAccessService>();
            accessService.Setup(s => s.EnsureSeriesOwnedByUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            var userService = DatabaseContextFactory.CreateUserServiceMock(OwnerUserId);

            var handler = new DeleteSeriesCommandHandler(
                seriesRepo, eventRepo, exceptionRepo, attachmentRepo, collaboratorRepo,
                notificationRepo, fileService, accessService.Object, userService.Object, unitOfWork);

            await handler.Handle(new DeleteSeriesCommand { SeriesUid = seriesUid }, CancellationToken.None);

            context.CalendarEvents.Any(e => e.Id == eventId).ShouldBeFalse();
            context.RecurrenceSeries.Any(s => s.SeriesUid == seriesUid).ShouldBeFalse();
            context.CalendarEventSegments.Any(s => s.RecurrenceSeriesId == series.Id).ShouldBeFalse();
            context.CalendarEventExceptions.Any(x => x.SeriesUid == seriesUid).ShouldBeFalse();
            context.CalendarEventCollaborators.Any(c => c.CalendarEventId == eventId).ShouldBeFalse();
            context.NotificationQueues.Any(n => n.CalendarEventId == eventId).ShouldBeFalse();
            context.CalendarEventAttachments.Any(a => a.CalendarEventId == eventId).ShouldBeFalse();

            fileService.DeletedPaths.ShouldContain(@"C:\files\agenda.pdf");
        }

        [Fact]
        public async Task DeleteCalendarEvent_NonRecurring_RemovesEventAndUnCascadedDependencies()
        {
            var context = DatabaseContextFactory.CreateContext(OwnerUserId);
            var start = DateTimeOffset.UtcNow;

            var rootEvent = new CalendarEvent
            {
                Subject = "One-off meeting",
                SeriesUid = Guid.NewGuid().ToString(),
                IsRecurring = false,
                StartDate = start,
                EndDate = start.AddHours(1),
                UserId = OwnerUserId
            };
            await context.CalendarEvents.AddAsync(rootEvent);
            await context.SaveChangesAsync();

            var collaborator = new CalendarEventCollaborator { CalendarEventId = rootEvent.Id, UserId = "collaborator-user" };
            await context.CalendarEventCollaborators.AddAsync(collaborator);

            var notification = new NotificationQueue
            {
                UserId = OwnerUserId,
                CalendarEventId = rootEvent.Id,
                ScheduledFor = start.AddMinutes(-15),
                IsProcessed = false,
                Message = "Reminder"
            };
            await context.NotificationQueues.AddAsync(notification);

            var attachment = new CalendarEventAttachment
            {
                CalendarEventId = rootEvent.Id,
                FileName = "notes.pdf",
                StoredFilePath = @"C:\files\notes.pdf",
                FileSize = 512,
                ContentType = "application/pdf",
                UploadedAt = DateTime.UtcNow,
                UploadedBy = OwnerUserId
            };
            await context.CalendarEventAttachments.AddAsync(attachment);

            await context.SaveChangesAsync();

            // Same rationale as SeedFullRecurringEventGraphAsync — start the handler under
            // test from a clean tracking slate rather than one still holding the seed instances.
            context.ChangeTracker.Clear();

            var eventId = rootEvent.Id;
            var eventRepo = new CalendarEventRepository(context);
            var attachmentRepo = new CalendarEventAttachmentRepository(context);
            var collaboratorRepo = new CalendarEventCollaboratorRepository(context);
            var unitOfWork = new EfUnitOfWork(context);
            var notificationRepo = new NotificationQueueRepository(context, unitOfWork);
            var fileService = new RecordingFileAttachmentService();
            var userService = DatabaseContextFactory.CreateUserServiceMock(OwnerUserId);

            var handler = new DeleteCalendarEventCommandHandler(
                eventRepo, attachmentRepo, collaboratorRepo, notificationRepo,
                fileService, userService.Object, unitOfWork);

            await handler.Handle(new DeleteCalendarEventCommand { Id = eventId }, CancellationToken.None);

            context.CalendarEvents.Any(e => e.Id == eventId).ShouldBeFalse();
            context.CalendarEventCollaborators.Any(c => c.CalendarEventId == eventId).ShouldBeFalse();
            context.NotificationQueues.Any(n => n.CalendarEventId == eventId).ShouldBeFalse();
            context.CalendarEventAttachments.Any(a => a.CalendarEventId == eventId).ShouldBeFalse();
            fileService.DeletedPaths.ShouldContain(@"C:\files\notes.pdf");
        }
    }
}
