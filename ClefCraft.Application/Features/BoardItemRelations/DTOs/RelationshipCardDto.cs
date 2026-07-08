using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItemRelations.DTOs
{
    public class RelationshipCardDto
    {
        public int RelationId { get; set; }

        public int ItemId { get; set; }

        public string Title { get; set; } = "";

        public string Status { get; set; } = "";

        public string Priority { get; set; } = "";

        public string? AssigneeId { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
