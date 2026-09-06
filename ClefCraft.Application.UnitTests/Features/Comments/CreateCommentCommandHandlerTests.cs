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

        private static (Mock<ICommentRepository>, CreateCommentCommandHandler) BuildHandler(
            Mock<INotificationHubService>? notificationHub = null)
        {
            var commentRepo = new Mock<ICommentRepository>();
            var boardAccessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            userService.Setup(u => u.GetUser(CallerUserId))
                .ReturnsAsync(new User { Id = CallerUserId, Firstname = "Ada", Lastname = "Lovelace" });

            var handler = new CreateCommentCommandHandler(
                commentRepo.Object,
                boardAccessService.Object,
                calendarAccessService.Object,
                new Mock<IBoardItemRepository>().Object,
                userService.Object,
                (notificationHub ?? new Mock<INotificationHubService>()).Object,
                new Mock<IUnitOfWork>().Object);

            return (commentRepo, handler);
        }

        [Fact]
        public async Task Handle_TopLevelComment_CreatesAndReturnsDto()
        {
            var (commentRepo, handler) = BuildHandler();

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
            var (commentRepo, handler) = BuildHandler();

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
            var (commentRepo, handler) = BuildHandler(notificationHub);

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
                "user-bob", "BoardItem", 42, It.IsAny<int>(), "Ada Lovelace", It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Once);

            // Self-mention is filtered out — no notification to yourself.
            notificationHub.Verify(h => h.SendCommentMentionAsync(
                CallerUserId, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
