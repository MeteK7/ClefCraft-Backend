using AutoMapper;
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
        private readonly IMapper _mapper;

        public GetStatusesHandler(IStatusRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<StatusDto>> Handle(GetStatusesQuery request, CancellationToken cancellationToken)
        {
            var statuses = await _repo.GetStatusesByBoardIdAsync(request.BoardId);
            return _mapper.Map<List<StatusDto>>(statuses);
        }
    }

}
