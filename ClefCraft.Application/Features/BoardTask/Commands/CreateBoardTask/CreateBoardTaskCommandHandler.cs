using AutoMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClefCraft.Domain;
using ClefCraft.Application.Features.BoardTask.Queries.GetBoardTasks;
using ClefCraft.Application.Contracts.Persistence;

namespace ClefCraft.Application.Features.BoardTask.Commands.CreateBoardTask
{
    public class CreateBoardTaskCommandHandler : IRequestHandler<CreateBoardTaskCommand, BoardTaskDto>
    {
        private readonly IBoardTaskRepository _taskRepository;
        private readonly IMapper _mapper;

        public CreateBoardTaskCommandHandler(IBoardTaskRepository taskRepository, IMapper mapper)
        {
            _taskRepository = taskRepository;
            _mapper = mapper;
        }

        public async Task<BoardTaskDto> Handle(CreateBoardTaskCommand request, CancellationToken cancellationToken)
        {
            var boardTask = new Domain.BoardTask
            {
                Title = request.Title,
                Description = request.Description,
                BoardColumnId = request.BoardColumnId
            };



            await _taskRepository.AddBoardTask(boardTask);

            return _mapper.Map<BoardTaskDto>(boardTask);
        }
    }
}
