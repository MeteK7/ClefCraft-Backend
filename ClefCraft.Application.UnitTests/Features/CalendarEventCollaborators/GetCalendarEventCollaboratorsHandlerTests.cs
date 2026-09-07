using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.CalendarEventCollaborators.Queries.GetCalendarEventCollaborators;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.CalendarEventCollaborators
{
    public class GetCalendarEventCollaboratorsHandlerTests
    {
        private const string CallerUserId = "user-1";

        [Fact]
        public async Task Handle_CallerCannotAccessEvent_ThrowsForbiddenAccessException()
        {
            var collaboratorRepo = new Mock<ICalendarEventCollaboratorRepository>();
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new GetCalendarEventCollaboratorsHandler(
                collaboratorRepo.Object, calendarAccessService.Object, userService.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetCalendarEventCollaboratorsQuery { EventId = 42 }, CancellationToken.None));

            collaboratorRepo.Verify(r => r.GetByEventIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Authorized_ReturnsCollaboratorsWithResolvedNames()
        {
            var collaboratorRepo = new Mock<ICalendarEventCollaboratorRepository>();
            collaboratorRepo.Setup(r => r.GetByEventIdAsync(42)).ReturnsAsync(new List<CalendarEventCollaborator>
            {
                new() { CalendarEventId = 42, UserId = "bob", CreatedBy = CallerUserId }
            });

            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            userService.Setup(u => u.GetUsersByIds(It.Is<List<string>>(ids => ids.Contains("bob"))))
                .ReturnsAsync(new List<User> { new() { Id = "bob", Firstname = "Bob", Lastname = "Builder" } });

            var handler = new GetCalendarEventCollaboratorsHandler(
                collaboratorRepo.Object, calendarAccessService.Object, userService.Object);

            var result = await handler.Handle(new GetCalendarEventCollaboratorsQuery { EventId = 42 }, CancellationToken.None);

            result.Count.ShouldBe(1);
            result[0].UserId.ShouldBe("bob");
            result[0].FullName.ShouldBe("Bob Builder");
        }
    }
}
