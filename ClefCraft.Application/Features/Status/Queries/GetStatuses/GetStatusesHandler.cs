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

namespace ClefCraft.Application.Features.Status.Queries.GetStatuses
{
    public class GetStatusesHandler
        : IRequestHandler<GetStatusesQuery, List<StatusDto>>
    {
        private readonly IStatusRepository _repo;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;

        public GetStatusesHandler(
            IStatusRepository repo,
            IBoardAccessService boardAccessService,
            IUserService userService)
        {
            _repo = repo;
            _boardAccessService = boardAccessService;
            _userService = userService;
        }

        public async Task<List<StatusDto>> Handle(GetStatusesQuery request, CancellationToken cancellationToken)
        {
            await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId, _userService.UserId);

            var statuses = await _repo.GetStatusesByBoardIdAsync(request.BoardId);
            return statuses.Select(BoardItemMapper.ToDto).ToList();
        }
    }

}
