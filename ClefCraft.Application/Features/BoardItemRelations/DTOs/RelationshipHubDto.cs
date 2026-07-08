using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.DTOs
{
    public class RelationshipHubDto
    {
        public List<RelationshipGroupDto> Groups { get; set; }
            = new();

        public int ParentCount { get; set; }

        public int BlockCount { get; set; }

        public int RelatedCount { get; set; }

        public int DependencyCount { get; set; }
    }
}
