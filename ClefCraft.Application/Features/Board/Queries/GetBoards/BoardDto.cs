using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.Board.Queries.GetBoards
{
    public class BoardDto
    {
        public int Id { get; set; }
        public string Title { get; set; }

        /// <summary>Creator metadata only — does not gate access. See IBoardAccessService.</summary>
        public string OwnerUserId { get; set; }
    }
}
