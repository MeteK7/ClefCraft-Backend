using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
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
        private readonly IBoardAccessService _boardAccessService;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCommentCommandHandler(
            ICommentRepository commentRepository,
            IBoardAccessService boardAccessService,
            ICalendarAccessService calendarAccessService,
            IUserService userService,
            IUnitOfWork unitOfWork)
        {
            _commentRepository = commentRepository;
            _boardAccessService = boardAccessService;
            _calendarAccessService = calendarAccessService;
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

            // The author must still have access to the underlying board/event — if they were
            // since removed (e.g. taken off the board), old comments there are frozen for them.
            await CommentAccess.EnsureCanAccessAsync(
                comment.EntityType, comment.EntityId, _userService.UserId,
                _boardAccessService, _calendarAccessService);

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
