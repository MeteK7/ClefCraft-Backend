using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Comments.Commands.CreateComment;
using ClefCraft.Application.Features.Comments.Queries.GetCommentsForEntity;
using ClefCraft.Application.UnitTests.Mocks;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Comments
{
    // Comments reuse IBoardAccessService for "BoardItem" (existing BoardMember check) and the
    // new ICalendarAccessService.EnsureCanCommentOnEventAsync for "CalendarEvent" — both
    // dispatched through CommentAccess.EnsureCanAccessAsync. These tests confirm the dispatch
    // fails closed in both directions and for an unsupported entity type.
    public class CommentsAuthorizationTests
    {
        private const string CallerUserId = "user-1";

        [Fact]
        public async Task GetCommentsForEntity_BoardItemNotAccessibleToCaller_ThrowsForbiddenAccessException()
        {
            var commentRepo = new Mock<ICommentRepository>();
            var boardAccessService = MockAccessServices.GetMockBoardAccessService(authorized: false);
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: true);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new GetCommentsForEntityHandler(
                commentRepo.Object, boardAccessService.Object, calendarAccessService.Object, userService.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetCommentsForEntityQuery { EntityType = "BoardItem", EntityId = 1 }, CancellationToken.None));

            commentRepo.Verify(r => r.GetByEntityAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetCommentsForEntity_CalendarEventNotCommentableByCaller_ThrowsForbiddenAccessException()
        {
            var commentRepo = new Mock<ICommentRepository>();
            var boardAccessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new GetCommentsForEntityHandler(
                commentRepo.Object, boardAccessService.Object, calendarAccessService.Object, userService.Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new GetCommentsForEntityQuery { EntityType = "CalendarEvent", EntityId = 1 }, CancellationToken.None));

            commentRepo.Verify(r => r.GetByEntityAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateComment_CalendarEventNotCommentableByCaller_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var commentRepo = new Mock<ICommentRepository>();
            var boardAccessService = MockAccessServices.GetMockBoardAccessService(authorized: true);
            var calendarAccessService = MockAccessServices.GetMockCalendarAccessService(authorized: false);
            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            var notificationHub = new Mock<INotificationHubService>();

            var handler = new CreateCommentCommandHandler(
                commentRepo.Object, boardAccessService.Object, calendarAccessService.Object,
                new Mock<IBoardItemRepository>().Object, userService.Object, notificationHub.Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(
                    new CreateCommentCommand { EntityType = "CalendarEvent", EntityId = 1, BodyHtml = "<p>hi</p>" },
                    CancellationToken.None));

            commentRepo.Verify(r => r.CreateAsync(It.IsAny<Domain.Comment>()), Times.Never);
        }
    }
}
