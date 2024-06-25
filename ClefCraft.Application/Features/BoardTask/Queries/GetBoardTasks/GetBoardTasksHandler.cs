using AutoMapper;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardTask.Queries.GetBoardTasks
{
    public class GetBoardTasksHandler : IRequestHandler<GetBoardTasksQuery, List<BoardColumnDto>>
    {
        private readonly IBoardTaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public GetBoardTasksHandler(IBoardTaskRepository taskRepository, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
        }
        public async Task<List<BoardColumnDto>> Handle(GetBoardTasksQuery request, CancellationToken cancellationToken)
        {
            var columns = await _taskRepository.GetBoardColumnsWithBoardTasks();
            return _mapper.Map<List<BoardColumnDto>>(columns);
        }
    }
}
