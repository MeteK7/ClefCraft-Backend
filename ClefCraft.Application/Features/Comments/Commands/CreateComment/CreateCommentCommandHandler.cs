using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using MediatR;

namespace ClefCraft.Application.Features.Comments.Commands.CreateComment
{
    public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IUserService _userService;
        private readonly INotificationHubService _notificationHubService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCommentCommandHandler(
            ICommentRepository commentRepository,
            IBoardAccessService boardAccessService,
            ICalendarAccessService calendarAccessService,
            IBoardItemRepository boardItemRepository,
            IUserService userService,
            INotificationHubService notificationHubService,
            IUnitOfWork unitOfWork)
        {
            _commentRepository = commentRepository;
            _boardAccessService = boardAccessService;
            _calendarAccessService = calendarAccessService;
            _boardItemRepository = boardItemRepository;
            _userService = userService;
            _notificationHubService = notificationHubService;
            _unitOfWork = unitOfWork;
        }

        public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            var validator = new CreateCommentCommandValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid comment request", validationResult);

            var userId = _userService.UserId;

            await CommentAccess.EnsureCanAccessAsync(
                request.EntityType, request.EntityId, userId,
                _boardAccessService, _calendarAccessService);

            if (request.ParentCommentId.HasValue)
            {
                var parent = await _commentRepository.GetByIdReadOnlyAsync(request.ParentCommentId.Value);

                if (parent == null || parent.EntityType != request.EntityType || parent.EntityId != request.EntityId)
                    throw new NotFoundException(nameof(Domain.Comment), request.ParentCommentId.Value);

                // Single-level replies only: a reply can never itself be replied to.
                if (parent.ParentCommentId.HasValue)
                    throw new BadRequestException("Cannot reply to a reply — only top-level comments can be replied to.");
            }

            var comment = new Domain.Comment
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                ParentCommentId = request.ParentCommentId,
                BodyHtml = request.BodyHtml,
                CreatedBy = userId
            };

            await _commentRepository.CreateAsync(comment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var mentionedUserIds = request.MentionedUserIds
                .Where(id => !string.IsNullOrWhiteSpace(id) && id != userId)
                .Distinct()
                .ToList();

            if (mentionedUserIds.Any())
            {
                await _commentRepository.AddMentionsAsync(
                    mentionedUserIds.Select(id => new Domain.CommentMention { CommentId = comment.Id, MentionedUserId = id }));

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var author = await _userService.GetUser(userId);

            if (mentionedUserIds.Any())
            {
                var authorFullName = author != null ? $"{author.Firstname} {author.Lastname}" : "Unknown";
                var excerpt = ExcerptUtils.PlainTextExcerpt(request.BodyHtml, 140);

                // BoardId is only resolved for the notification payload — it lets the frontend's
                // mention toast deep-link straight to the right board, since /board?openItemId=
                // otherwise defaults to the viewer's first board.
                int? boardId = null;
                if (comment.EntityType == "BoardItem")
                {
                    var item = await _boardItemRepository.GetByIdReadOnlyAsync(comment.EntityId);
                    boardId = item?.BoardId;
                }

                foreach (var mentionedUserId in mentionedUserIds)
                {
                    await _notificationHubService.SendCommentMentionAsync(
                        mentionedUserId, comment.EntityType, comment.EntityId, comment.Id, authorFullName, excerpt, boardId, cancellationToken);
                }
            }

            return CommentMapper.ToDto(comment, author, mentionedUserIds);
        }
    }
}
