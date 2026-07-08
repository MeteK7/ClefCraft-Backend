using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.DTOs
{
    public class RelationshipGroupDto
    {
        public int RelationType { get; set; }

        public string Name { get; set; } = "";

        public List<RelationshipCardDto> Items { get; set; }
            = new();
    }
}
