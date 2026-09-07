using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using ClefCraft.Infrastructure.Services.Authorization;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Comments
{
    // EnsureCanAccessEventAsync and GrantCollaboratorAccessAsync are new, real branching logic
    // (unlike the mocked-through interface used everywhere else), so they're tested directly
    // against the concrete service rather than only through a handler-level mock.
    public class CalendarAccessServiceCommentTests
    {
        private const string OwnerUserId = "owner-1";

        private static (CalendarAccessService Service, Mock<ICalendarEventCollaboratorRepository> CollaboratorRepo) BuildService(
            CalendarEvent calendarEvent, bool isCollaborator = false)
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.GetByIdReadOnlyAsync(calendarEvent.Id)).ReturnsAsync(calendarEvent);

            var attachmentRepo = new Mock<ICalendarEventAttachmentRepository>();

            var collaboratorRepo = new Mock<ICalendarEventCollaboratorRepository>();
            collaboratorRepo.Setup(r => r.IsCollaboratorAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(isCollaborator);

            var service = new CalendarAccessService(eventRepo.Object, attachmentRepo.Object, collaboratorRepo.Object);

            return (service, collaboratorRepo);
        }

        [Fact]
        public async Task EnsureCanAccessEventAsync_Owner_Allowed()
        {
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId };
            var (service, _) = BuildService(calendarEvent, isCollaborator: false);

            await Should.NotThrowAsync(() => service.EnsureCanAccessEventAsync(1, OwnerUserId));
        }

        [Fact]
        public async Task EnsureCanAccessEventAsync_GrantedCollaborator_Allowed_RegardlessOfLinkedBoardItemId()
        {
            // Standalone event — no LinkedBoardItemId at all — proving the check never depends
            // on it or on board co-membership: only an explicit CalendarEventCollaborator grant
            // (or ownership) matters.
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId, LinkedBoardItemId = null };
            var (service, _) = BuildService(calendarEvent, isCollaborator: true);

            await Should.NotThrowAsync(() => service.EnsureCanAccessEventAsync(1, "collaborator-1"));
        }

        [Fact]
        public async Task EnsureCanAccessEventAsync_NotGrantedUser_ThrowsForbiddenAccessException()
        {
            // Board co-membership with the owner is no longer sufficient by itself — only an
            // explicit grant is.
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId };
            var (service, _) = BuildService(calendarEvent, isCollaborator: false);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                service.EnsureCanAccessEventAsync(1, "stranger"));
        }

        [Fact]
        public async Task EnsureCanAccessEventAsync_EventNotFound_ThrowsNotFoundException()
        {
            var eventRepo = new Mock<ICalendarEventRepository>();
            eventRepo.Setup(r => r.GetByIdReadOnlyAsync(It.IsAny<int>())).ReturnsAsync((CalendarEvent?)null);

            var service = new CalendarAccessService(
                eventRepo.Object, new Mock<ICalendarEventAttachmentRepository>().Object,
                new Mock<ICalendarEventCollaboratorRepository>().Object);

            await Should.ThrowAsync<NotFoundException>(() => service.EnsureCanAccessEventAsync(999, "anyone"));
        }

        [Fact]
        public async Task GrantCollaboratorAccessAsync_Owner_GrantsNewCollaborator_AndReturnsIt()
        {
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId };
            var (service, collaboratorRepo) = BuildService(calendarEvent, isCollaborator: false);

            var granted = await service.GrantCollaboratorAccessAsync(1, OwnerUserId, new[] { "bob" });

            granted.ShouldBe(new List<string> { "bob" });
            collaboratorRepo.Verify(r => r.CreateAsync(It.Is<CalendarEventCollaborator>(
                c => c.CalendarEventId == 1 && c.UserId == "bob" && c.CreatedBy == OwnerUserId)), Times.Once);
        }

        [Fact]
        public async Task GrantCollaboratorAccessAsync_NonOwnerGranter_NoOps_DoesNotGrantAnything()
        {
            // The defensive second layer: even though a handler should already have filtered
            // this down, a direct call from a non-owner must never create a grant.
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId };
            var (service, collaboratorRepo) = BuildService(calendarEvent, isCollaborator: false);

            var granted = await service.GrantCollaboratorAccessAsync(1, "not-the-owner", new[] { "bob" });

            granted.ShouldBeEmpty();
            collaboratorRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventCollaborator>()), Times.Never);
        }

        [Fact]
        public async Task GrantCollaboratorAccessAsync_AlreadyCollaborator_IsIdempotent_NotReGranted()
        {
            var calendarEvent = new CalendarEvent { Id = 1, UserId = OwnerUserId };
            var (service, collaboratorRepo) = BuildService(calendarEvent, isCollaborator: true);

            var granted = await service.GrantCollaboratorAccessAsync(1, OwnerUserId, new[] { "bob" });

            granted.ShouldBeEmpty();
            collaboratorRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventCollaborator>()), Times.Never);
        }
    }
}
