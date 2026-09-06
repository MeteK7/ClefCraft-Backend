using ClefCraft.Application.Common.Models;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Comments;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Exceptions;
using MediatR;

namespace ClefCraft.Application.Features.Comments.Queries.GetCommentsForEntity
{
    public class GetCommentsForEntityHandler : IRequestHandler<GetCommentsForEntityQuery, PagedResult<CommentDto>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IUserService _userService;

        public GetCommentsForEntityHandler(
            ICommentRepository commentRepository,
            IBoardAccessService boardAccessService,
            ICalendarAccessService calendarAccessService,
            IUserService userService)
        {
            _commentRepository = commentRepository;
            _boardAccessService = boardAccessService;
            _calendarAccessService = calendarAccessService;
            _userService = userService;
        }

        public async Task<PagedResult<CommentDto>> Handle(GetCommentsForEntityQuery request, CancellationToken cancellationToken)
        {
            var validator = new GetCommentsForEntityValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid comments request", validationResult);

            await CommentAccess.EnsureCanAccessAsync(
                request.EntityType, request.EntityId, _userService.UserId,
                _boardAccessService, _calendarAccessService);

            var skip = (request.PageNumber - 1) * request.PageSize;

            var comments = await _commentRepository.GetByEntityAsync(request.EntityType, request.EntityId, skip, request.PageSize);
            var totalCount = await _commentRepository.CountByEntityAsync(request.EntityType, request.EntityId);

            var commentIds = comments.Select(c => c.Id).ToList();
            var mentions = await _commentRepository.GetMentionsByCommentIdsAsync(commentIds);
            var mentionsByComment = mentions
                .GroupBy(m => m.CommentId)
                .ToDictionary(g => g.Key, g => g.Select(m => m.MentionedUserId).ToList());

            var authorIds = comments
                .Select(c => c.CreatedBy)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var authors = await _userService.GetUsersByIds(authorIds!);

            var items = comments.Select(c =>
            {
                var author = authors.FirstOrDefault(u => u.Id == c.CreatedBy);
                var mentionedUserIds = mentionsByComment.TryGetValue(c.Id, out var ids) ? ids : new List<string>();
                return CommentMapper.ToDto(c, author, mentionedUserIds);
            }).ToList();

            return new PagedResult<CommentDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
