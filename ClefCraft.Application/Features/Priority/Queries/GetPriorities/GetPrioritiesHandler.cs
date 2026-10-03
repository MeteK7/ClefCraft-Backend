using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardItem;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Priority.Queries.GetPriorities
{
    public class GetPrioritiesHandler
        : IRequestHandler<GetPrioritiesQuery, List<PriorityDto>>
    {
        private readonly IPriorityRepository _repo;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;

        public GetPrioritiesHandler(
            IPriorityRepository repo,
            IBoardAccessService boardAccessService,
            IUserService userService)
        {
            _repo = repo;
            _boardAccessService = boardAccessService;
            _userService = userService;
        }

        public async Task<List<PriorityDto>> Handle(
            GetPrioritiesQuery request,
            CancellationToken cancellationToken)
        {
            await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId, _userService.UserId);

            var priorities = await _repo.GetPrioritiesByBoardIdAsync(request.BoardId);
            return priorities.Select(BoardItemMapper.ToDto).ToList();
        }
    }
}
