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

namespace ClefCraft.Application.Features.Priority.Queries.GetPriorities
{
    public class GetPrioritiesHandler
        : IRequestHandler<GetPrioritiesQuery, List<PriorityDto>>
    {
        private readonly IPriorityRepository _repo;
        private readonly IBoardAccessService _boardAccessService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public GetPrioritiesHandler(
            IPriorityRepository repo,
            IBoardAccessService boardAccessService,
            IUserService userService,
            IMapper mapper)
        {
            _repo = repo;
            _boardAccessService = boardAccessService;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<List<PriorityDto>> Handle(
            GetPrioritiesQuery request,
            CancellationToken cancellationToken)
        {
            await _boardAccessService.EnsureBoardOwnedByUserAsync(request.BoardId, _userService.UserId);

            var priorities = await _repo.GetPrioritiesByBoardIdAsync(request.BoardId);
            return _mapper.Map<List<PriorityDto>>(priorities);
        }
    }
}
