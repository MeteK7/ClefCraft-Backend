using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem;
using MediatR;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks
{
    public class GetMyAskCountQueryHandler : IRequestHandler<GetMyAskCountQuery, int>
    {
        private readonly IBoardItemAskRepository _askRepository;
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IStatusRepository _statusRepository;
        private readonly IUserService _userService;

        public GetMyAskCountQueryHandler(
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

        public async Task<int> Handle(GetMyAskCountQuery request, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(request.Direction))
                throw new BadRequestException("Direction must be Received, Sent or Waiting");

            var callerId = _userService.UserId;

            var boardIds = await _boardMemberRepository.GetMemberBoardIdsAsync(callerId);
            if (boardIds.Count == 0)
                return 0;

            var completedStatusIds = await CompletedStatus.GetIdsAsync(_statusRepository);

            return await _askRepository.CountAsync(
                MyAskFilters.For(request.Direction, callerId, boardIds, DateTime.UtcNow, completedStatusIds));
        }
    }
}
