using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Board.Queries.GetBoards
{
    public class GetBoardsQuery:IRequest<List<BoardDto>>
    {
        public string UserId { get; set; }
    }
}
