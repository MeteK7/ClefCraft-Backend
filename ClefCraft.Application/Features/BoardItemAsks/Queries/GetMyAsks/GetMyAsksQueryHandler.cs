using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain.Enums;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks
{
    public class GetMyAsksQueryHandler : IRequestHandler<GetMyAsksQuery, List<BoardItemAskDto>>
    {
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IUserService _userService;

        public GetMyAsksQueryHandler(
            IBoardItemAskRepository askRepository,
            IBoardMemberRepository boardMemberRepository,
            IStatusRepository statusRepository,
            IUserService userService)
        {
            _askRepository = askRepository;
            _boardMemberRepository = boardMemberRepository;
            _statusRepository = statusRepository;
            _userService = userService;
        }

        public async Task<List<BoardItemAskDto>> Handle(GetMyAsksQuery request, CancellationToken cancellationToken)
        {
            var validationResult = await new GetMyAsksQueryValidator().ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                throw new BadRequestException("Invalid asks request", validationResult);

            var callerId = _userService.UserId;
            var utcNow = DateTime.UtcNow;

            // Only boards the caller is still a member of: an ask on a board they left can't be
            // opened or acted on, so it isn't theirs to see.
            var boardIds = await _boardMemberRepository.GetMemberBoardIdsAsync(callerId);
            if (boardIds.Count == 0)
                return new List<BoardItemAskDto>();

            var completedStatusIds = await CompletedStatus.GetIdsAsync(_statusRepository);

            var filter = MyAskFilters.For(request.Direction, callerId, boardIds, utcNow, completedStatusIds);

            var asks = await _askRepository.GetWithItemReadOnlyAsync(filter);
            if (asks.Count == 0)
                return new List<BoardItemAskDto>();

            var askBoardIds = asks.Select(a => a.BoardItem.BoardId).Distinct().ToList();
            var members = (await _boardMemberRepository.GetByBoardIdsAsync(askBoardIds))
                .Select(m => (m.BoardId, m.UserId))
                .ToHashSet();

            var userIds = asks.SelectMany(BoardItemAskMapper.UserIdsOf).Distinct().ToList();
            var fullNames = (await _userService.GetUsersByIds(userIds))
                .ToDictionary(u => u.Id, u => $"{u.Firstname} {u.Lastname}");

            var replies = await _askRepository.GetRepliesByAskIdsReadOnlyAsync(asks.Select(a => a.Id));

            var ordered = request.Direction switch
            {
                // Waiting for an answer first, oldest first; then accepted work by due date.
                AskDirection.Received => asks.OrderBy(a => a.State == BoardItemAskState.Pending ? 0 : 1)
                    .ThenBy(a => a.State == BoardItemAskState.Pending ? (DateTime?)null : a.DueDate ?? DateTime.MaxValue)
                    .ThenBy(a => a.DateCreated),
                // Open asks first, then the recently declined ones, newest decline first.
                AskDirection.Sent => asks.OrderBy(a => a.State switch
                    {
                        BoardItemAskState.Pending => 0,
                        BoardItemAskState.Accepted => 1,
                        _ => 2
                    })
                    .ThenByDescending(a => a.State == BoardItemAskState.Declined ? a.RespondedAt : null)
                    .ThenBy(a => a.DateCreated),
                // Grouped by board; within a board, decisions first, then work, then requirements to
                // resolve; earliest due first, then oldest.
                _ => asks.OrderBy(a => a.BoardItem.Board?.Title, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(a => a.BoardItem.BoardId)
                    .ThenBy(a => AskTurn.Of(a, false)?.Reason)
                    .ThenBy(a => a.DueDate ?? DateTime.MaxValue)
                    .ThenBy(a => a.DateCreated)
            };

            return ordered
                .Select(ask =>
                {
                    var item = ask.BoardItem;
                    var roles = AskActorRoles.For(ask, callerId, item.AssigneeId, item.Board?.OwnerUserId);
                    var itemIsDone = CompletedStatus.IsCompleted(item, completedStatusIds);

                    return BoardItemAskMapper.ToDto(
                        ask,
                        item,
                        fullNames,
                        members.Contains((item.BoardId, ask.RecipientUserId)),
                        callerId,
                        roles,
                        itemIsDone,
                        utcNow,
                        replies);
                })
                .ToList();
        }
    }
}
