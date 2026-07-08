using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelation.DTOs
{
    public class BoardItemRelationDto
    {
        public int Id { get; set; }

        public int RelationType { get; set; }

        public int ItemId { get; set; }

        public string Title { get; set; } = "";

        public string Status { get; set; } = "";

        public string Priority { get; set; } = "";
    }
}
