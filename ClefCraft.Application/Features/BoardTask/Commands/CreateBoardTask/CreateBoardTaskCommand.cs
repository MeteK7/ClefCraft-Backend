using ClefCraft.Application.Features.BoardTask.Queries.GetBoardTasks;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardTask.Commands.CreateBoardTask
{
    public class CreateBoardTaskCommand : IRequest<BoardTaskDto>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int BoardColumnId { get; set; }
    }
}
