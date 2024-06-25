using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardTask.Queries.GetBoardTasks
{
    public class GetBoardTasksQuery : IRequest<List<BoardColumnDto>> { }
}
