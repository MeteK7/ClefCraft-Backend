using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.Calendar.Commands.UpdateSeries;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Commands
{
    // "Edit ALL occurrences" scope, in its two variants. The two handlers are covered
    // together because their contrast is the point: OverrideAll unconditionally replaces
    // RecurrenceRuleJson and purges every per-occurrence exception, while PreserveExceptions
    // only touches fields explicitly provided and never touches exceptions at all (it has no
    // exception-repository dependency in its constructor).
    public class UpdateSeriesScopeCommandHandlerTests
    {
        private const string CallerUserId = "user-1";
        private const string SeriesUid = "series-1";

        private static CalendarEventSegment MakeSegment(string subject, string ruleJson) =>
            new CalendarEventSegment
            {
                RecurrenceSeriesId = 5, Subject = subject, RecurrenceRuleJson = ruleJson,
                IsRecurring = true
            };

        [Fact]
        public async Task OverrideAll_Handle_UnconditionallyReplacesRecurrenceRuleJsonOnEverySegment()
        {
            var segment1 = MakeSegment("Subject A", "{\"Frequency\":\"DAILY\",\"Interval\":1}");
            var segment2 = MakeSegment("Subject B", "{\"Frequency\":\"WEEKLY\",\"Interval\":1}");

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            segmentRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid))
                .ReturnsAsync(new List<CalendarEventSegment> { segment1, segment2 });

            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();
            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateSeriesOverrideAllCommandHandler(
                segmentRepo.Object, exceptionRepo.Object, accessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            var newRuleJson = "{\"Frequency\":\"MONTHLY\",\"Interval\":1}";

            await handler.Handle(new UpdateSeriesOverrideAllCommand
            {
                SeriesUid = SeriesUid,
                RecurrenceRuleJson = newRuleJson
            }, CancellationToken.None);

            segment1.RecurrenceRuleJson.ShouldBe(newRuleJson);
            segment2.RecurrenceRuleJson.ShouldBe(newRuleJson);
        }

        [Fact]
        public async Task OverrideAll_Handle_OnlyUpdatesSubjectLocationCommentWhenProvided_LeavesUntouchedWhenNull()
        {
            var segment = MakeSegment("Original subject", "{\"Frequency\":\"DAILY\",\"Interval\":1}");
            segment.Location = "Original location";
            segment.Comment = "Original comment";

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            segmentRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid))
                .ReturnsAsync(new List<CalendarEventSegment> { segment });

            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();
            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateSeriesOverrideAllCommandHandler(
                segmentRepo.Object, exceptionRepo.Object, accessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            await handler.Handle(new UpdateSeriesOverrideAllCommand
            {
                SeriesUid = SeriesUid,
                Subject = "New subject",
                Location = null, // not provided — should be left as-is
                Comment = null,  // not provided — should be left as-is
                RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            }, CancellationToken.None);

            segment.Subject.ShouldBe("New subject");
            segment.Location.ShouldBe("Original location");
            segment.Comment.ShouldBe("Original comment");
        }

        [Fact]
        public async Task OverrideAll_Handle_PurgesAllExceptionsForTheSeries()
        {
            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            segmentRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid))
                .ReturnsAsync(new List<CalendarEventSegment> { MakeSegment("Subject", "{\"Frequency\":\"DAILY\",\"Interval\":1}") });

            var exceptionRepo = new Mock<ICalendarEventExceptionRepository>();
            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateSeriesOverrideAllCommandHandler(
                segmentRepo.Object, exceptionRepo.Object, accessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            await handler.Handle(new UpdateSeriesOverrideAllCommand
            {
                SeriesUid = SeriesUid,
                RecurrenceRuleJson = "{\"Frequency\":\"DAILY\",\"Interval\":1}"
            }, CancellationToken.None);

            exceptionRepo.Verify(r => r.DeleteAllForSeriesAsync(SeriesUid), Times.Once);
        }

        [Fact]
        public async Task PreserveExceptions_Handle_OnlyReplacesRecurrenceRuleJsonWhenProvided_LeavesUntouchedWhenNull()
        {
            var segment = MakeSegment("Subject", "{\"Frequency\":\"DAILY\",\"Interval\":1}");

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            segmentRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid))
                .ReturnsAsync(new List<CalendarEventSegment> { segment });

            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateSeriesPreserveExceptionsCommandHandler(
                segmentRepo.Object, accessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            // RecurrenceRuleJson not provided (null) — unlike OverrideAll, this must be
            // left untouched rather than being cleared/overwritten.
            await handler.Handle(new UpdateSeriesPreserveExceptionsCommand
            {
                SeriesUid = SeriesUid,
                Subject = "New subject",
                RecurrenceRuleJson = null
            }, CancellationToken.None);

            segment.Subject.ShouldBe("New subject");
            segment.RecurrenceRuleJson.ShouldBe("{\"Frequency\":\"DAILY\",\"Interval\":1}");
        }

        [Fact]
        public async Task PreserveExceptions_Handle_MultipleSegmentsInSeries_AllSegmentsUpdatedConsistently()
        {
            var segment1 = MakeSegment("Old A", "{\"Frequency\":\"DAILY\",\"Interval\":1}");
            var segment2 = MakeSegment("Old B", "{\"Frequency\":\"WEEKLY\",\"Interval\":1}");

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            segmentRepo.Setup(r => r.GetBySeriesUidAsync(SeriesUid))
                .ReturnsAsync(new List<CalendarEventSegment> { segment1, segment2 });

            var accessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateSeriesPreserveExceptionsCommandHandler(
                segmentRepo.Object, accessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            var newRuleJson = "{\"Frequency\":\"MONTHLY\",\"Interval\":1}";

            await handler.Handle(new UpdateSeriesPreserveExceptionsCommand
            {
                SeriesUid = SeriesUid,
                Subject = "New subject",
                RecurrenceRuleJson = newRuleJson
            }, CancellationToken.None);

            segment1.Subject.ShouldBe("New subject");
            segment2.Subject.ShouldBe("New subject");
            segment1.RecurrenceRuleJson.ShouldBe(newRuleJson);
            segment2.RecurrenceRuleJson.ShouldBe(newRuleJson);
        }
    }
}
