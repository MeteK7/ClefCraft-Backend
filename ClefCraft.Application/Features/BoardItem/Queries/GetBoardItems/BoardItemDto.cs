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

        public string Title { get; set; }
        public string Description { get; set; }

        public int BoardId { get; set; }
        public int BoardColumnId { get; set; }

        public StatusDto Status { get; set; }
        public PriorityDto Priority { get; set; }

        public List<TagDto> Tags { get; set; } = new();
        public string AssigneeId { get; set; }
        public string AssigneeFirstName { get; set; }
        public string AssigneeLastName { get; set; }
        public DateTime? DueDate { get; set; }

        public double? EstimatedTime { get; set; }
        public double? TimeSpent { get; set; }

        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

        public string CreatedByFullName { get; set; }
        public string ModifiedByFullName { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
