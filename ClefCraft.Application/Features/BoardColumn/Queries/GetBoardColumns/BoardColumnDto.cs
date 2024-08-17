using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems;

namespace ClefCraft.Application.Features.BoardColumn.Queries.GetBoardColumns
{
    public class BoardColumnDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public List<BoardItemDto> BoardItems { get; set; }
    }
}
