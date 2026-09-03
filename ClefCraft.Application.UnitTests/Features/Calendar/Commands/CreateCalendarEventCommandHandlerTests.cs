using AutoMapper;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Calendar.Commands.CreateCalendarEvent;
using ClefCraft.Application.Features.Calendar.Queries;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Moq;
using Shouldly;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Calendar.Commands
{
    public class CreateCalendarEventCommandHandlerTests
    {
        private static (
            CreateCalendarEventCommandHandler handler,
            Mock<ICalendarEventRepository> eventRepo,
            Mock<IRecurrenceSeriesRepository> seriesRepo,
            Mock<ICalendarEventSegmentRepository> segmentRepo,
            Mock<IBoardAccessService> boardAccessService
        ) MakeHandler(bool linkedBoardItemAuthorized = true)
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.CreateAsync(It.IsAny<CalendarEvent>()))
                .Callback<CalendarEvent>(e => e.Id = 42)
                .Returns(Task.CompletedTask);

            var seriesRepo = new Mock<IRecurrenceSeriesRepository>();
            seriesRepo.Setup(r => r.CreateAsync(It.IsAny<RecurrenceSeries>()))
                .Callback<RecurrenceSeries>(s => s.Id = 7)
                .Returns(Task.CompletedTask);

            var segmentRepo = new Mock<ICalendarEventSegmentRepository>();
            var reminderRepo = new Mock<ICalendarReminderRepository>();
            var reminderScheduler = new Mock<IReminderSchedulerService>();

            var boardAccessService = MockAccessServices.GetMockBoardAccessService(authorized: linkedBoardItemAuthorized);

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns("user-1");

            var mapper = new Mock<IMapper>();
            mapper.Setup(m => m.Map<CalendarEventDto>(It.IsAny<CalendarEvent>())).Returns(new CalendarEventDto());

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new CreateCalendarEventCommandHandler(
                eventRepo.Object,
                seriesRepo.Object,
                segmentRepo.Object,
                reminderRepo.Object,
                reminderScheduler.Object,
                boardAccessService.Object,
                mapper.Object,
                userService.Object,
                unitOfWork.Object);

            return (handler, eventRepo, seriesRepo, segmentRepo, boardAccessService);
        }

        [Fact]
        public async Task Handle_NewRecurringEvent_CreatesSeriesAndSegmentWithTheConfiguredRule()
        {
            // Matrix row B
            var (handler, _, seriesRepo, segmentRepo, _) = MakeHandler();
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);
            var ruleJson = "{\"Frequency\":\"WEEKLY\",\"Interval\":1,\"DaysOfWeek\":[1,3,5],\"Count\":10}";

            var request = new CreateCalendarEventCommand
            {
                Subject = "Standup",
                StartDate = start,
                EndDate = start.AddHours(1),
                AllDayEvent = false,
                Importance = ImportanceLevel.Normal,
                IsRecurring = true,
                RecurrenceRuleJson = ruleJson
            };

            await handler.Handle(request, CancellationToken.None);

            seriesRepo.Verify(r => r.CreateAsync(It.Is<RecurrenceSeries>(s => s.UserId == "user-1")), Times.Once);
            segmentRepo.Verify(r => r.CreateAsync(It.Is<CalendarEventSegment>(s =>
                s.RecurrenceRuleJson == ruleJson &&
                s.EffectiveFrom == start &&
                s.EffectiveTo == null)), Times.Once);
        }

        [Fact]
        public async Task Handle_NewNonRecurringEvent_NeverCreatesSeriesOrSegment()
        {
            // Matrix row A
            var (handler, _, seriesRepo, segmentRepo, _) = MakeHandler();
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            var request = new CreateCalendarEventCommand
            {
                Subject = "One-off meeting",
                StartDate = start,
                EndDate = start.AddHours(1),
                AllDayEvent = false,
                Importance = ImportanceLevel.Normal,
                IsRecurring = false
            };

            await handler.Handle(request, CancellationToken.None);

            seriesRepo.Verify(r => r.CreateAsync(It.IsAny<RecurrenceSeries>()), Times.Never);
            segmentRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventSegment>()), Times.Never);
        }

        [Theory]
        [InlineData("{\"Frequency\":\"DAILY\",\"Interval\":0}")]
        [InlineData("{\"Frequency\":\"DAILY\",\"Interval\":1,\"Count\":0}")]
        [InlineData("{\"Frequency\":\"UNSUPPORTED\",\"Interval\":1}")]
        public async Task Handle_InvalidRecurrenceRule_ThrowsBeforeAnyRepositoryWrite(string invalidRuleJson)
        {
            var (handler, eventRepo, seriesRepo, _, _) = MakeHandler();
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            var request = new CreateCalendarEventCommand
            {
                Subject = "Bad rule",
                StartDate = start,
                EndDate = start.AddHours(1),
                AllDayEvent = false,
                Importance = ImportanceLevel.Normal,
                IsRecurring = true,
                RecurrenceRuleJson = invalidRuleJson
            };

            await Should.ThrowAsync<ValidationException>(() => handler.Handle(request, CancellationToken.None));

            eventRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEvent>()), Times.Never);
            seriesRepo.Verify(r => r.CreateAsync(It.IsAny<RecurrenceSeries>()), Times.Never);
        }

        [Fact]
        public async Task Handle_LinkedBoardItemNotOwnedByCaller_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var (handler, eventRepo, _, _, boardAccessService) = MakeHandler(linkedBoardItemAuthorized: false);
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            var request = new CreateCalendarEventCommand
            {
                Subject = "Practice session",
                StartDate = start,
                EndDate = start.AddHours(1),
                AllDayEvent = false,
                Importance = ImportanceLevel.Normal,
                IsRecurring = false,
                LinkedBoardItemId = 999
            };

            await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(request, CancellationToken.None));

            boardAccessService.Verify(s => s.EnsureBoardItemOwnedByUserAsync(999, "user-1"), Times.Once);
            eventRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEvent>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NoLinkedBoardItem_NeverChecksBoardAccess()
        {
            var (handler, _, _, _, boardAccessService) = MakeHandler();
            var start = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

            var request = new CreateCalendarEventCommand
            {
                Subject = "Unlinked event",
                StartDate = start,
                EndDate = start.AddHours(1),
                AllDayEvent = false,
                Importance = ImportanceLevel.Normal,
                IsRecurring = false
            };

            await handler.Handle(request, CancellationToken.None);

            boardAccessService.Verify(
                s => s.EnsureBoardItemOwnedByUserAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }
    }
}
