using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetAsksForItem
{
    public class GetAsksForItemQueryHandler : IRequestHandler<GetAsksForItemQuery, List<BoardItemAskDto>>
    {
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IBoardItemRepository _boardItemRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;

        public GetAsksForItemQueryHandler(
            IBoardItemAskRepository askRepository,
            IBoardItemRepository boardItemRepository,
            IBoardRepository boardRepository,
            IStatusRepository statusRepository,
            IBoardMemberRepository boardMemberRepository,
            IBoardAccessService boardAccessService,
            IUserService userService)
        {
            _askRepository = askRepository;
            _boardItemRepository = boardItemRepository;
            _boardRepository = boardRepository;
            _statusRepository = statusRepository;
            _boardMemberRepository = boardMemberRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
        }

        public async Task<List<BoardItemAskDto>> Handle(GetAsksForItemQuery request, CancellationToken cancellationToken)
        {
            var callerId = _userService.UserId;

            var context = await AskCommandSupport.LoadItemAsync(
                request.BoardItemId, callerId, _boardAccessService, _boardItemRepository, _boardRepository, _statusRepository);

            var asks = await _askRepository.GetByItemIdsReadOnlyAsync(new[] { request.BoardItemId });
            if (asks.Count == 0)
                return new List<BoardItemAskDto>();

            var memberIds = (await _boardMemberRepository.GetByBoardIdAsync(context.Item.BoardId))
                .Select(m => m.UserId)
                .ToHashSet();

            var userIds = asks.SelectMany(BoardItemAskMapper.UserIdsOf).Distinct().ToList();
            var fullNames = (await _userService.GetUsersByIds(userIds))
                .ToDictionary(u => u.Id, u => $"{u.Firstname} {u.Lastname}");

            var utcNow = DateTime.UtcNow;

            return asks
                .Select(ask => BoardItemAskMapper.ToDto(
                    ask,
                    context.Item,
                    fullNames,
                    memberIds.Contains(ask.RecipientUserId),
                    context.RolesFor(ask, callerId),
                    context.ItemIsDone,
                    utcNow))
                .ToList();
        }
    }
}
