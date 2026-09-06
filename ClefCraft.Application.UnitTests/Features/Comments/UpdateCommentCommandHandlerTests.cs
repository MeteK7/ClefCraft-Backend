using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Comments.Commands.UpdateComment;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Comments
{
    public class UpdateCommentCommandHandlerTests
    {
        private const string CallerUserId = "user-1";

        [Fact]
        public async Task Handle_NotTheAuthor_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var commentRepo = new Mock<ICommentRepository>();
            commentRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Comment { Id = 1, EntityType = "BoardItem", EntityId = 42, CreatedBy = "someone-else", BodyHtml = "<p>original</p>" });

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateCommentCommandHandler(
                commentRepo.Object, new Mock<IBoardItemRepository>().Object, userService.Object,
                new Mock<INotificationHubService>().Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new UpdateCommentCommand { Id = 1, BodyHtml = "<p>edited</p>" }, CancellationToken.None));

            commentRepo.Verify(r => r.UpdateAsync(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Author_UpdatesBodyAndReturnsDto()
        {
            var comment = new Comment { Id = 1, EntityType = "BoardItem", EntityId = 42, CreatedBy = CallerUserId, BodyHtml = "<p>original</p>" };

            var commentRepo = new Mock<ICommentRepository>();
            commentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);
            commentRepo.Setup(r => r.GetMentionsByCommentIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new List<CommentMention>());

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);
            userService.Setup(u => u.GetUser(CallerUserId))
                .ReturnsAsync(new User { Id = CallerUserId, Firstname = "Ada", Lastname = "Lovelace" });

            var handler = new UpdateCommentCommandHandler(
                commentRepo.Object, new Mock<IBoardItemRepository>().Object, userService.Object,
                new Mock<INotificationHubService>().Object, new Mock<IUnitOfWork>().Object);

            var result = await handler.Handle(new UpdateCommentCommand { Id = 1, BodyHtml = "<p>edited</p>" }, CancellationToken.None);

            comment.BodyHtml.ShouldBe("<p>edited</p>");
            commentRepo.Verify(r => r.UpdateAsync(comment), Times.Once);
            result.BodyHtml.ShouldBe("<p>edited</p>");
        }

        [Fact]
        public async Task Handle_DeletedComment_ThrowsNotFoundException()
        {
            var commentRepo = new Mock<ICommentRepository>();
            commentRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Comment { Id = 1, CreatedBy = CallerUserId, IsDeleted = true });

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new UpdateCommentCommandHandler(
                commentRepo.Object, new Mock<IBoardItemRepository>().Object, userService.Object,
                new Mock<INotificationHubService>().Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<NotFoundException>(() =>
                handler.Handle(new UpdateCommentCommand { Id = 1, BodyHtml = "<p>edited</p>" }, CancellationToken.None));
        }
    }
}
