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

        /// <summary>Board of the related item, so the client can link to it.</summary>
        public int BoardId { get; set; }

        /// <summary>
        /// True when the item the relations were requested for is the relation's source
        /// ("this item Blocks ItemId"); false when it is the target ("ItemId Blocks this item").
        /// </summary>
        public bool IsOutgoing { get; set; }

        public string Title { get; set; } = "";

        public string Status { get; set; } = "";

        public string Priority { get; set; } = "";

        public string? AssigneeId { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
