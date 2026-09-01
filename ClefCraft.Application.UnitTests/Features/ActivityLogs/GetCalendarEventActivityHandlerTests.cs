using ClefCraft.Application.Contracts.ActivityLogs;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.ActivityLogs.Queries.GetCalendarEventActivity;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Application.UnitTests.Features.ActivityLogs
{
    public class GetCalendarEventActivityHandlerTests
    {
        private static Mock<IActivityLogRepository> MockActivityLogRepository(
            Dictionary<string, List<ActivityLog>> logsByEntityType)
        {
            var mock = new Mock<IActivityLogRepository>();

            mock.Setup(r => r.GetByEntityTypeAndIdsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync((string entityType, IEnumerable<int> ids) =>
                {
                    if (!logsByEntityType.TryGetValue(entityType, out var logs)) return new List<ActivityLog>();
                    var idSet = ids.ToHashSet();
                    return logs.Where(l => idSet.Contains(l.EntityId)).ToList();
                });

            return mock;
        }

        private static Mock<ICalendarEventSegmentRepository> MockSegmentRepository(List<CalendarEventSegment> segments)
        {
            var mock = new Mock<ICalendarEventSegmentRepository>();
            mock.Setup(r => r.GetBySeriesUidAsync(It.IsAny<string>())).ReturnsAsync(segments);
            return mock;
        }

        private static Mock<ICalendarEventExceptionRepository> MockExceptionRepository(List<CalendarEventException> exceptions)
        {
            var mock = new Mock<ICalendarEventExceptionRepository>();
            mock.Setup(r => r.GetBySeriesUid(It.IsAny<string>())).ReturnsAsync(exceptions);
            return mock;
        }

        private static Mock<IUserService> MockUserService(params User[] users)
        {
            var mock = new Mock<IUserService>();
            mock.Setup(u => u.GetUsersByIds(It.IsAny<List<string>>())).ReturnsAsync(users.ToList());
            return mock;
        }

        private static Mock<ICalendarAccessService> MockCalendarAccessService(bool authorized = true)
        {
            var mock = new Mock<ICalendarAccessService>();
            var eventSetup = mock.Setup(s => s.EnsureEventOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()));
            var seriesSetup = mock.Setup(s => s.EnsureSeriesOwnedByUserAsync(It.IsAny<string>(), It.IsAny<string>()));

            if (authorized)
            {
                eventSetup.Returns(Task.CompletedTask);
                seriesSetup.Returns(Task.CompletedTask);
            }
            else
            {
                eventSetup.ThrowsAsync(new ForbiddenAccessException());
                seriesSetup.ThrowsAsync(new ForbiddenAccessException());
            }

            return mock;
        }

        [Fact]
        public async Task Handle_NonRecurring_ReturnsOnlyEventScopedEntries_AndNeverQueriesSegmentsOrExceptions()
        {
            var eventLogs = new List<ActivityLog>
            {
                new ActivityLog { Id = 1, EntityType = "CalendarEvent", EntityId = 42, ActionType = "CREATED", UserId = "u1", Timestamp = DateTime.UtcNow }
            };

            var activityRepo = MockActivityLogRepository(new() { ["CalendarEvent"] = eventLogs });
            var segmentRepo = MockSegmentRepository(new List<CalendarEventSegment>());
            var exceptionRepo = MockExceptionRepository(new List<CalendarEventException>());
            var userService = MockUserService(new User { Id = "u1", Firstname = "Jane", Lastname = "Doe" });

            var handler = new GetCalendarEventActivityHandler(activityRepo.Object, segmentRepo.Object, exceptionRepo.Object, MockCalendarAccessService().Object, userService.Object);

            var result = await handler.Handle(new GetCalendarEventActivityQuery { EventId = 42 }, CancellationToken.None);

            result.Items.Count.ShouldBe(1);
            result.Items.Single().Scope.ShouldBe("Event");

            segmentRepo.Verify(r => r.GetBySeriesUidAsync(It.IsAny<string>()), Times.Never);
            exceptionRepo.Verify(r => r.GetBySeriesUid(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Recurring_MergesEventSegmentAndExceptionScopes_SortedByTimestampDesc()
        {
            var now = DateTime.UtcNow;

            var eventLogs = new List<ActivityLog>
            {
                new ActivityLog { Id = 1, EntityType = "CalendarEvent", EntityId = 42, ActionType = "CREATED", UserId = "u1", Timestamp = now.AddDays(-3) }
            };

            var segment = new CalendarEventSegment
            {
                Id = 5,
                EffectiveFrom = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
                EffectiveTo = null,
                Subject = "Standup",
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddHours(1)
            };
            var segmentLogs = new List<ActivityLog>
            {
                new ActivityLog { Id = 2, EntityType = "CalendarEventSegment", EntityId = 5, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddDays(-2), MetadataJson = "{\"Subject\":{\"Old\":\"Standup\",\"New\":\"Daily Standup\"}}" }
            };

            var exception = new CalendarEventException
            {
                Id = 9,
                SeriesUid = "series-1",
                OccurrenceDate = new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero)
            };
            var exceptionLogs = new List<ActivityLog>
            {
                new ActivityLog { Id = 3, EntityType = "CalendarEventException", EntityId = 9, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddDays(-1) }
            };

            var activityRepo = MockActivityLogRepository(new()
            {
                ["CalendarEvent"] = eventLogs,
                ["CalendarEventSegment"] = segmentLogs,
                ["CalendarEventException"] = exceptionLogs
            });
            var segmentRepo = MockSegmentRepository(new List<CalendarEventSegment> { segment });
            var exceptionRepo = MockExceptionRepository(new List<CalendarEventException> { exception });
            var userService = MockUserService(new User { Id = "u1", Firstname = "Jane", Lastname = "Doe" });

            var handler = new GetCalendarEventActivityHandler(activityRepo.Object, segmentRepo.Object, exceptionRepo.Object, MockCalendarAccessService().Object, userService.Object);

            var result = await handler.Handle(
                new GetCalendarEventActivityQuery { EventId = 42, SeriesUid = "series-1" },
                CancellationToken.None);

            result.TotalCount.ShouldBe(3);
            // Most recent first: Exception (-1d), Segment (-2d), Event (-3d)
            result.Items.Select(i => i.Scope).ShouldBe(new[] { "Exception", "Segment", "Event" });

            var exceptionEntry = result.Items.Single(i => i.Scope == "Exception");
            exceptionEntry.OccurrenceDate.ShouldBe(exception.OccurrenceDate);
            exceptionEntry.EffectiveFrom.ShouldBeNull();

            var segmentEntry = result.Items.Single(i => i.Scope == "Segment");
            segmentEntry.EffectiveFrom.ShouldBe(segment.EffectiveFrom);
            segmentEntry.Changes.Single().FieldName.ShouldBe("Subject");
        }

        [Fact]
        public async Task Handle_RecurringWithNoSegmentsOrExceptionsYet_BehavesLikeNonRecurring()
        {
            var eventLogs = new List<ActivityLog>
            {
                new ActivityLog { Id = 1, EntityType = "CalendarEvent", EntityId = 42, ActionType = "CREATED", UserId = "u1", Timestamp = DateTime.UtcNow }
            };

            var activityRepo = MockActivityLogRepository(new() { ["CalendarEvent"] = eventLogs });
            var segmentRepo = MockSegmentRepository(new List<CalendarEventSegment>());
            var exceptionRepo = MockExceptionRepository(new List<CalendarEventException>());
            var userService = MockUserService(new User { Id = "u1", Firstname = "Jane", Lastname = "Doe" });

            var handler = new GetCalendarEventActivityHandler(activityRepo.Object, segmentRepo.Object, exceptionRepo.Object, MockCalendarAccessService().Object, userService.Object);

            var result = await handler.Handle(
                new GetCalendarEventActivityQuery { EventId = 42, SeriesUid = "series-1" },
                CancellationToken.None);

            result.Items.Count.ShouldBe(1);
            result.Items.Single().Scope.ShouldBe("Event");
        }

        [Fact]
        public async Task Handle_PaginatesAcrossMergedResults()
        {
            var now = DateTime.UtcNow;
            var eventLogs = Enumerable.Range(1, 5)
                .Select(i => new ActivityLog { Id = i, EntityType = "CalendarEvent", EntityId = 42, ActionType = "UPDATED", UserId = "u1", Timestamp = now.AddMinutes(-i) })
                .ToList();

            var activityRepo = MockActivityLogRepository(new() { ["CalendarEvent"] = eventLogs });
            var segmentRepo = MockSegmentRepository(new List<CalendarEventSegment>());
            var exceptionRepo = MockExceptionRepository(new List<CalendarEventException>());
            var userService = MockUserService(new User { Id = "u1", Firstname = "Jane", Lastname = "Doe" });

            var handler = new GetCalendarEventActivityHandler(activityRepo.Object, segmentRepo.Object, exceptionRepo.Object, MockCalendarAccessService().Object, userService.Object);

            var result = await handler.Handle(
                new GetCalendarEventActivityQuery { EventId = 42, PageNumber = 2, PageSize = 2 },
                CancellationToken.None);

            result.TotalCount.ShouldBe(5);
            result.Items.Count.ShouldBe(2);
            result.Items.Select(i => i.Id).ShouldBe(new[] { 3, 4 });
        }

        [Fact]
        public async Task Handle_InvalidEventId_ThrowsBadRequestException()
        {
            var activityRepo = MockActivityLogRepository(new());
            var segmentRepo = MockSegmentRepository(new List<CalendarEventSegment>());
            var exceptionRepo = MockExceptionRepository(new List<CalendarEventException>());
            var userService = MockUserService();

            var handler = new GetCalendarEventActivityHandler(activityRepo.Object, segmentRepo.Object, exceptionRepo.Object, MockCalendarAccessService().Object, userService.Object);

            await Should.ThrowAsync<BadRequestException>(() =>
                handler.Handle(new GetCalendarEventActivityQuery { EventId = 0 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_EventNotOwnedByCaller_ThrowsForbiddenAccessException()
        {
            var activityRepo = MockActivityLogRepository(new());
            var segmentRepo = MockSegmentRepository(new List<CalendarEventSegment>());
            var exceptionRepo = MockExceptionRepository(new List<CalendarEventException>());
            var userService = MockUserService();
            var calendarAccessService = MockCalendarAccessService(authorized: false);

            var handler = new GetCalendarEventActivityHandler(activityRepo.Object, segmentRepo.Object, exceptionRepo.Object, calendarAccessService.Object, userService.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetCalendarEventActivityQuery { EventId = 42 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_SeriesUidNotOwnedByCaller_ThrowsForbiddenAccessException_EvenWhenEventIdIsOwned()
        {
            var activityRepo = MockActivityLogRepository(new());
            var segmentRepo = MockSegmentRepository(new List<CalendarEventSegment>());
            var exceptionRepo = MockExceptionRepository(new List<CalendarEventException>());
            var userService = MockUserService();

            var calendarAccessService = new Mock<ICalendarAccessService>();
            calendarAccessService
                .Setup(s => s.EnsureEventOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            calendarAccessService
                .Setup(s => s.EnsureSeriesOwnedByUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ForbiddenAccessException());

            var handler = new GetCalendarEventActivityHandler(activityRepo.Object, segmentRepo.Object, exceptionRepo.Object, calendarAccessService.Object, userService.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetCalendarEventActivityQuery { EventId = 42, SeriesUid = "someone-elses-series" }, CancellationToken.None));
        }
    }
}
