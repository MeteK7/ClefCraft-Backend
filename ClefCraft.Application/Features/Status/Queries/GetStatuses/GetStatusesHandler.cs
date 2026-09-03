using AutoMapper;
using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Persistence;
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
        private readonly IMapper _mapper;

        public GetStatusesHandler(
            IStatusRepository repo,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IMapper mapper)
        {
            _repo = repo;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<List<StatusDto>> Handle(GetStatusesQuery request, CancellationToken cancellationToken)
        {
            await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId, _userService.UserId);

            var statuses = await _repo.GetStatusesByBoardIdAsync(request.BoardId);
            return _mapper.Map<List<StatusDto>>(statuses);
        }
    }

}
