using ClefCraft.Application.Features.Priority.Queries.GetPriorities;
using ClefCraft.Application.Features.Status.Queries.GetStatuses;
using ClefCraft.Application.Features.Tag.Queries.GetTags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Queries.GetBoardItems
{
    public class BoardItemDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;

        public int BoardId { get; set; }
        public int BoardColumnId { get; set; }

        public StatusDto Status { get; set; } = null!;
        public PriorityDto Priority { get; set; } = null!;

        public List<TagDto> Tags { get; set; } = new();
        public string AssigneeId { get; set; } = null!;
        public string AssigneeFirstName { get; set; } = null!;
        public string AssigneeLastName { get; set; } = null!;
        public DateTime? DueDate { get; set; }

        public double? EstimatedTime { get; set; }
        public double? TimeSpent { get; set; }

        public string CreatedBy { get; set; } = null!;
        public string ModifiedBy { get; set; } = null!;

        public string CreatedByFullName { get; set; } = null!;
        public string ModifiedByFullName { get; set; } = null!;

        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
