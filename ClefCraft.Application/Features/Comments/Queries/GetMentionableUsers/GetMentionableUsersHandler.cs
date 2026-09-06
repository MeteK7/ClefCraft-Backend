using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using MediatR;

namespace ClefCraft.Application.Features.Comments.Queries.GetMentionableUsers
{
    public class GetMentionableUsersHandler : IRequestHandler<GetMentionableUsersQuery, List<MentionableUserDto>>
    {
        private readonly IBoardAccessService _boardAccessService;
        private readonly ICalendarAccessService _calendarAccessService;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IUserService _userService;

        public GetMentionableUsersHandler(
            IBoardAccessService boardAccessService,
            ICalendarAccessService calendarAccessService,
            IBoardItemRepository boardItemRepository,
            ICalendarEventRepository calendarEventRepository,
            IBoardMemberRepository boardMemberRepository,
            IUserService userService)
        {
            _boardAccessService = boardAccessService;
            _calendarAccessService = calendarAccessService;
            _boardItemRepository = boardItemRepository;
            _calendarEventRepository = calendarEventRepository;
            _boardMemberRepository = boardMemberRepository;
            _userService = userService;
        }

        public async Task<List<MentionableUserDto>> Handle(GetMentionableUsersQuery request, CancellationToken cancellationToken)
        {
            if (!AllowedEntityTypes.Values.Contains(request.EntityType))
                throw new BadRequestException($"{request.EntityType} is not a supported entity type");

            var callerId = _userService.UserId;

            await CommentAccess.EnsureCanAccessAsync(
                request.EntityType, request.EntityId, callerId,
                _boardAccessService, _calendarAccessService);

            var candidateIds = new HashSet<string>();

            if (request.EntityType == "BoardItem")
            {
                var item = await _boardItemRepository.GetByIdReadOnlyAsync(request.EntityId);
                if (item == null) throw new NotFoundException(nameof(Domain.BoardItem), request.EntityId);

                var members = await _boardMemberRepository.GetByBoardIdAsync(item.BoardId);
                foreach (var m in members) candidateIds.Add(m.UserId);
            }
            else // CalendarEvent
            {
                var calendarEvent = await _calendarEventRepository.GetByIdReadOnlyAsync(request.EntityId);
                if (calendarEvent == null) throw new NotFoundException(nameof(Domain.CalendarEvent), request.EntityId);

                candidateIds.Add(calendarEvent.UserId);

                var ownerBoardIds = await _boardMemberRepository.GetMemberBoardIdsAsync(calendarEvent.UserId);
                foreach (var boardId in ownerBoardIds)
                {
                    var members = await _boardMemberRepository.GetByBoardIdAsync(boardId);
                    foreach (var m in members) candidateIds.Add(m.UserId);
                }
            }

            candidateIds.Remove(callerId);

            var users = await _userService.GetUsersByIds(candidateIds.ToList());

            return users
                .Select(u => new MentionableUserDto { UserId = u.Id, FullName = $"{u.Firstname} {u.Lastname}" })
                .OrderBy(u => u.FullName)
                .ToList();
        }
    }
}
