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
        public int BoardId { get; set; }

        public int ExcludeItemId { get; set; }

        public string SearchTerm { get; set; } = "";

        public SearchBoardItemsQuery(
            int boardId,
            int excludeItemId,
            string searchTerm)
        {
            BoardId = boardId;
            ExcludeItemId = excludeItemId;
            SearchTerm = searchTerm;
        }
    }
}
