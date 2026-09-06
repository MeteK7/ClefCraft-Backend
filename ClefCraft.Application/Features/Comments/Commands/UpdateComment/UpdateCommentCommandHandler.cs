using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using MediatR;

namespace ClefCraft.Application.Features.Comments.Commands.UpdateComment
{
    public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IUserService _userService;
        private readonly INotificationHubService _notificationHubService;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCommentCommandHandler(
            ICommentRepository commentRepository,
            IBoardItemRepository boardItemRepository,
            IUserService userService,
            INotificationHubService notificationHubService,
            IUnitOfWork unitOfWork)
        {
            _commentRepository = commentRepository;
            _boardItemRepository = boardItemRepository;
            _userService = userService;
            _notificationHubService = notificationHubService;
            _unitOfWork = unitOfWork;
        }

        public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
        {
            var validator = new UpdateCommentCommandValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid comment request", validationResult);

            var userId = _userService.UserId;

            var comment = await _commentRepository.GetByIdAsync(request.Id);
            if (comment == null || comment.IsDeleted)
                throw new NotFoundException(nameof(Domain.Comment), request.Id);

            // Edit/delete is author-only — there is no board-owner/moderator override in v1.
            if (comment.CreatedBy != userId)
                throw new ForbiddenAccessException();

            comment.BodyHtml = request.BodyHtml;
            await _commentRepository.UpdateAsync(comment);

            var existingMentions = await _commentRepository.GetMentionsByCommentIdsAsync(new[] { comment.Id });
            var existingMentionIds = existingMentions.Select(m => m.MentionedUserId).ToHashSet();

            var newMentionIds = request.MentionedUserIds
                .Where(id => !string.IsNullOrWhiteSpace(id) && id != userId)
                .Distinct()
                .ToList();

            await _commentRepository.RemoveMentionsAsync(comment.Id);
            if (newMentionIds.Any())
            {
                await _commentRepository.AddMentionsAsync(
                    newMentionIds.Select(id => new Domain.CommentMention { CommentId = comment.Id, MentionedUserId = id }));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Only people newly added to the mention list on this edit get notified —
            // re-notifying everyone already mentioned on every subsequent edit would be noise.
            var addedMentionIds = newMentionIds.Where(id => !existingMentionIds.Contains(id)).ToList();

            var author = await _userService.GetUser(userId);

            if (addedMentionIds.Any())
            {
                var authorFullName = author != null ? $"{author.Firstname} {author.Lastname}" : "Unknown";
                var excerpt = ExcerptUtils.PlainTextExcerpt(request.BodyHtml, 140);

                int? boardId = null;
                if (comment.EntityType == "BoardItem")
                {
                    var item = await _boardItemRepository.GetByIdReadOnlyAsync(comment.EntityId);
                    boardId = item?.BoardId;
                }

                foreach (var mentionedUserId in addedMentionIds)
                {
                    await _notificationHubService.SendCommentMentionAsync(
                        mentionedUserId, comment.EntityType, comment.EntityId, comment.Id, authorFullName, excerpt, boardId, cancellationToken);
                }
            }

            return CommentMapper.ToDto(comment, author, newMentionIds);
        }
    }
}
