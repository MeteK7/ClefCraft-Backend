using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Commands.CreateBoardItem
{
    public class CreateBoardItemCommand : IRequest<BoardItemDto>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int BoardColumnId { get; set; }
        public int BoardId { get; set; }
        public int StatusId { get; set; }
        public int PriorityId { get; set; }
    }
}
