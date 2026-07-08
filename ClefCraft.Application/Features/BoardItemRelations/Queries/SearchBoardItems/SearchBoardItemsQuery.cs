using ClefCraft.Application.Features.BoardItemRelations.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.Queries.SearchBoardItems
{
    public class SearchBoardItemsQuery
        : IRequest<List<BoardItemSearchDto>>
    {
        public int ItemId { get; set; }

        public string Search { get; set; } = "";
    }
}
