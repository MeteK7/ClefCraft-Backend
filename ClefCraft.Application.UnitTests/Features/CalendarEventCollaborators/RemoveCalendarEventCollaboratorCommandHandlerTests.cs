using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.CalendarEventCollaborators.Commands.RemoveCalendarEventCollaborator;
using ClefCraft.Application.UnitTests.Mocks;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.CalendarEventCollaborators
{
    public class RemoveCalendarEventCollaboratorCommandHandlerTests
    {
        private const string CallerUserId = "user-1";

        [Fact]
        public async Task Handle_NotTheOwner_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            // Revocation is an owner-only control — not something a collaborator can do to
            // themselves or to another collaborator.
            var collaboratorRepo = new Mock<ICalendarEventCollaboratorRepository>();
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new RemoveCalendarEventCollaboratorCommandHandler(
                collaboratorRepo.Object, calendarAccessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new RemoveCalendarEventCollaboratorCommand { EventId = 42, CollaboratorUserId = "bob" }, CancellationToken.None));

            collaboratorRepo.Verify(r => r.RemoveAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Owner_RemovesCollaborator()
        {
            var collaboratorRepo = new Mock<ICalendarEventCollaboratorRepository>();
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new RemoveCalendarEventCollaboratorCommandHandler(
                collaboratorRepo.Object, calendarAccessService.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            await handler.Handle(new RemoveCalendarEventCollaboratorCommand { EventId = 42, CollaboratorUserId = "bob" }, CancellationToken.None);

            collaboratorRepo.Verify(r => r.RemoveAsync(42, "bob"), Times.Once);
        }
    }
}
