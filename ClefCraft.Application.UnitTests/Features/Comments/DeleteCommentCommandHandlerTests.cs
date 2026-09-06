using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.Comments.Commands.DeleteComment;
using ClefCraft.Domain;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;

namespace ClefCraft.Application.UnitTests.Features.Comments
{
    public class DeleteCommentCommandHandlerTests
    {
        private const string CallerUserId = "user-1";

        [Fact]
        public async Task Handle_NotTheAuthor_ThrowsForbiddenAccessException_BeforeAnyWrite()
        {
            var commentRepo = new Mock<ICommentRepository>();
            commentRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Comment { Id = 1, CreatedBy = "someone-else", BodyHtml = "<p>original</p>" });

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new DeleteCommentCommandHandler(commentRepo.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            await Should.ThrowAsync<ForbiddenAccessException>(() =>
                handler.Handle(new DeleteCommentCommand { Id = 1 }, CancellationToken.None));

            commentRepo.Verify(r => r.UpdateAsync(It.IsAny<Comment>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Author_TombstonesComment_ClearsBodyAndMentions_KeepsRowAndIsDeletedTrue()
        {
            var comment = new Comment { Id = 1, CreatedBy = CallerUserId, BodyHtml = "<p>original</p>", IsDeleted = false };

            var commentRepo = new Mock<ICommentRepository>();
            commentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new DeleteCommentCommandHandler(commentRepo.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            await handler.Handle(new DeleteCommentCommand { Id = 1 }, CancellationToken.None);

            comment.IsDeleted.ShouldBeTrue();
            comment.BodyHtml.ShouldBeNull();
            commentRepo.Verify(r => r.UpdateAsync(comment), Times.Once);
            commentRepo.Verify(r => r.RemoveMentionsAsync(1), Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyDeleted_IsIdempotent_DoesNotWriteAgain()
        {
            var comment = new Comment { Id = 1, CreatedBy = CallerUserId, IsDeleted = true };

            var commentRepo = new Mock<ICommentRepository>();
            commentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

            var userService = new Mock<IUserService>();
            userService.Setup(u => u.UserId).Returns(CallerUserId);

            var handler = new DeleteCommentCommandHandler(commentRepo.Object, userService.Object, new Mock<IUnitOfWork>().Object);

            await handler.Handle(new DeleteCommentCommand { Id = 1 }, CancellationToken.None);

            commentRepo.Verify(r => r.UpdateAsync(It.IsAny<Comment>()), Times.Never);
        }
    }
}
