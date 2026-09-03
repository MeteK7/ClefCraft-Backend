using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardMembers.DTOs;
using MediatR;

namespace ClefCraft.Application.Features.BoardMembers.Queries.GetMembers
{
    public class GetBoardMembersQueryHandler : IRequestHandler<GetBoardMembersQuery, List<BoardMemberDto>>
    {
        private readonly IBoardMemberRepository _boardMemberRepository;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;

        public GetBoardMembersQueryHandler(
            IBoardMemberRepository boardMemberRepository,
            IBoardAccessService boardAccessService,
            IUserService userService)
        {
            _boardMemberRepository = boardMemberRepository;
            _boardAccessService = boardAccessService;
            _userService = userService;
        }

        public async Task<List<BoardMemberDto>> Handle(GetBoardMembersQuery request, CancellationToken cancellationToken)
        {
            // Any current member may view the member list, not just the owner.
            await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId, request.UserId);

            var members = await _boardMemberRepository.GetByBoardIdAsync(request.BoardId);

            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var users = await _userService.GetUsersByIds(userIds);
            var userDictionary = users.ToDictionary(u => u.Id);

            return members.Select(m => new BoardMemberDto
            {
                Id = m.Id,
                BoardId = m.BoardId,
                UserId = m.UserId,
                FullName = userDictionary.TryGetValue(m.UserId, out var user)
                    ? $"{user.Firstname} {user.Lastname}"
                    : "Unknown"
            }).ToList();
        }
    }
}
