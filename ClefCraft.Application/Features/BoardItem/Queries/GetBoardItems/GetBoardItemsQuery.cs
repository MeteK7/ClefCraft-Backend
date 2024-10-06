using ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems
{
    public class GetBoardItemsQuery : IRequest<List<BoardColumnDto>> {
        public int? BoardId { get; set; }  // Make it nullable

        public GetBoardItemsQuery(int? boardId = null)  // Default to null
        {
            BoardId = boardId;
        }
    }
}
