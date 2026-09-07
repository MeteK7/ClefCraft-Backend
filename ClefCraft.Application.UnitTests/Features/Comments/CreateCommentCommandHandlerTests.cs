using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Comments.Commands.CreateComment;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Application.UnitTests.Mocks;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Comments
{
    public class CreateCommentCommandHandlerTests
    {
        private const string CallerUserId = "user-1";

        private static (Mock<ICommentRepository> CommentRepo, Mock<ICalendarAccessService> CalendarAccessService,
            Mock<ICalendarEventRepository> CalendarEventRepo, Mock<ICalendarEventCollaboratorRepository> CollaboratorRepo,
            CreateCommentCommandHandler Handler) BuildHandler(
            Mock<INotificationHubService>? notificationHub = null,
            Mock<ICalendarAccessService>? calendarAccessService = null,
            Mock<ICalendarEventRepository>? calendarEventRepo = null,
            Mock<ICalendarEventCollaboratorRepository>? collaboratorRepo = null)
        {
            var commentRepo = new Mock<ICommentRepository>();
            var boardAccessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            calendarAccessService ??= MockAccessServices.GetMockCalendarAccessService(authorized: true);
            calendarEventRepo ??= new Mock<ICalendarEventRepository>();
            collaboratorRepo ??= new Mock<ICalendarEventCollaboratorRepository>();

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            userService.Setup(u => u.GetUser(CallerUserId))
                .ReturnsAsync(new User { Id = CallerUserId, Firstname = "Ada", Lastname = "Lovelace" });

            var handler = new CreateCommentCommandHandler(
                commentRepo.Object,
                boardAccessService.Object,
                calendarAccessService.Object,
                new Mock<IBoardItemRepository>().Object,
                calendarEventRepo.Object,
                collaboratorRepo.Object,
                userService.Object,
                (notificationHub ?? new Mock<INotificationHubService>()).Object,
                new Mock<IUnitOfWork>().Object);

            return (commentRepo, calendarAccessService, calendarEventRepo, collaboratorRepo, handler);
        }

        [Fact]
        public async Task Handle_TopLevelComment_CreatesAndReturnsDto()
        {
            var (commentRepo, _, _, _, handler) = BuildHandler();

            var result = await handler.Handle(
                new CreateCommentCommand { EntityType = "BoardItem", EntityId = 42, BodyHtml = "<p>hello team</p>" },
                CancellationToken.None);

            commentRepo.Verify(r => r.CreateAsync(It.Is<Comment>(c =>
                c.EntityType == "BoardItem" && c.EntityId == 42 && c.ParentCommentId == null)), Times.Once);

            result.AuthorFullName.ShouldBe("Ada Lovelace");
            result.IsDeleted.ShouldBeFalse();
        }

        [Fact]
        public async Task Handle_ReplyToAReply_ThrowsBadRequestException_BeforeAnyWrite()
        {
            var (commentRepo, _, _, _, handler) = BuildHandler();

            // The existing "parent" is itself a reply (has its own ParentCommentId) —
            // single-level replies only, so replying to it must be rejected.
            commentRepo.Setup(r => r.GetByIdReadOnlyAsync(5))
                .ReturnsAsync(new Comment { Id = 5, EntityType = "BoardItem", EntityId = 42, ParentCommentId = 1 });

            await Should.ThrowAsync<BadRequestException>(() =>
                handler.Handle(
                    new CreateCommentCommand { EntityType = "BoardItem", EntityId = 42, ParentCommentId = 5, BodyHtml = "<p>reply</p>" },
                    CancellationToken.None));

            commentRepo.Verify(r => r.CreateAsync(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithMentions_AddsMentionsAndNotifiesEachMentionedUser_ExcludingSelf()
        {
            var notificationHub = new Mock<INotificationHubService>();
            var (commentRepo, _, _, _, handler) = BuildHandler(notificationHub);

            await handler.Handle(
                new CreateCommentCommand
                {
                    EntityType = "BoardItem",
                    EntityId = 42,
                    BodyHtml = "<p>hi @bob</p>",
                    MentionedUserIds = new List<string> { "user-bob", CallerUserId }
                },
                CancellationToken.None);

            commentRepo.Verify(r => r.AddMentionsAsync(It.Is<IEnumerable<CommentMention>>(
                mentions => System.Linq.Enumerable.Count(mentions) == 1
                    && System.Linq.Enumerable.All(mentions, m => m.MentionedUserId == "user-bob"))), Times.Once);

            notificationHub.Verify(h => h.SendCommentMentionAsync(
                "user-bob", "BoardItem", 42, It.IsAny<int>(), "Ada Lovelace", It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once);

            // Self-mention is filtered out — no notification to yourself.
            notificationHub.Verify(h => h.SendCommentMentionAsync(
                CallerUserId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_OwnerMentionsSomeoneNewOnCalendarEvent_GrantsCollaboratorAccess_AndNotifiesWithGrantedAccessTrue()
        {
            var notificationHub = new Mock<INotificationHubService>();
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            calendarAccessService
                .Setup(s => s.GrantCollaboratorAccessAsync(42, CallerUserId, It.Is<IEnumerable<string>>(ids => ids.Contains("bob"))))
                .ReturnsAsync(new List<string> { "bob" });

            var calendarEventRepo = new Mock<ICalendarEventRepository>();
            calendarEventRepo.Setup(r => r.GetByIdReadOnlyAsync(42))
                .ReturnsAsync(new CalendarEvent { Id = 42, UserId = CallerUserId }); // caller is the owner

            var (_, _, _, collaboratorRepo, handler) = BuildHandler(notificationHub, calendarAccessService, calendarEventRepo);
            collaboratorRepo.Setup(r => r.IsCollaboratorAsync(42, "bob")).ReturnsAsync(false);

            await handler.Handle(
                new CreateCommentCommand
                {
                    EntityType = "CalendarEvent",
                    EntityId = 42,
                    BodyHtml = "<p>@bob can you take this?</p>",
                    MentionedUserIds = new List<string> { "bob" }
                },
                CancellationToken.None);

            notificationHub.Verify(h => h.SendCommentMentionAsync(
                "bob", "CalendarEvent", 42, It.IsAny<int>(), "Ada Lovelace", It.IsAny<string>(),
                null, true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_NonOwnerMentionsSomeoneOutsideTheEvent_DropsTheMention_NoCommentMentionRow_NoNotification()
        {
            const string OwnerUserId = "owner-1";

            var notificationHub = new Mock<INotificationHubService>();
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            // Caller (CallerUserId) is not the owner, so GrantCollaboratorAccessAsync no-ops —
            // matches the real CalendarAccessService's defensive behavior.
            calendarAccessService
                .Setup(s => s.GrantCollaboratorAccessAsync(42, CallerUserId, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<string>());

            var calendarEventRepo = new Mock<ICalendarEventRepository>();
            calendarEventRepo.Setup(r => r.GetByIdReadOnlyAsync(42))
                .ReturnsAsync(new CalendarEvent { Id = 42, UserId = OwnerUserId }); // caller is NOT the owner

            var (commentRepo, _, _, collaboratorRepo, handler) = BuildHandler(notificationHub, calendarAccessService, calendarEventRepo);
            collaboratorRepo.Setup(r => r.IsCollaboratorAsync(42, "stranger")).ReturnsAsync(false);

            await handler.Handle(
                new CreateCommentCommand
                {
                    EntityType = "CalendarEvent",
                    EntityId = 42,
                    BodyHtml = "<p>@stranger check this out</p>",
                    MentionedUserIds = new List<string> { "stranger" }
                },
                CancellationToken.None);

            commentRepo.Verify(r => r.AddMentionsAsync(It.IsAny<IEnumerable<CommentMention>>()), Times.Never);
            notificationHub.Verify(h => h.SendCommentMentionAsync(
                "stranger", It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
