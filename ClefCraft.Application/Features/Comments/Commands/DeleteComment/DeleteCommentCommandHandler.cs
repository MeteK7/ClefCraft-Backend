using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using MediatR;

namespace ClefCraft.Application.Features.Comments.Commands.DeleteComment
{
    public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCommentCommandHandler(
            ICommentRepository commentRepository,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _commentRepository = commentRepository;
            _userService = userService;
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = await _commentRepository.GetByIdAsync(request.Id);
            if (comment == null)
                throw new NotFoundException(nameof(Domain.Comment), request.Id);

            if (comment.CreatedBy != _userService.UserId)
                throw new ForbiddenAccessException();

            if (comment.IsDeleted)
                return Unit.Value; // already tombstoned — idempotent

            // Tombstone: keep the row (and its place among replies), but clear the content so
            // it isn't recoverable via the API. Mentions are removed too since a deleted
            // comment's mentions are no longer meaningful.
            comment.IsDeleted = true;
            comment.BodyHtml = null;
            await _commentRepository.UpdateAsync(comment);
            await _commentRepository.RemoveMentionsAsync(comment.Id);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
