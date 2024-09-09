using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById
{
    public class GetBoardItemByIdQuery : IRequest<BoardItemByIdDto>
    {
        public int Id { get; set; }

        public GetBoardItemByIdQuery(int id)
        {
            Id = id;
        }
    }
}
