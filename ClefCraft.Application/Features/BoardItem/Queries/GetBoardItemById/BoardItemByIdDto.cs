using ClefCraft.Application.Features.Priority.Queries.GetPriorities;
using ClefCraft.Application.Features.Status.Queries.GetStatuses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Application.Features.BoardItem.Queries.GetBoardItemById
{
    public class BoardItemByIdDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public StatusDto? Status { get; set; }
        public PriorityDto? Priority { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string AssigneeId { get; set; } = null!;
        public string AssigneeFirstName { get; set; } = null!;
        public string AssigneeLastName { get; set; } = null!;
        public DateTime DueDate { get; set; }
        public double EstimatedTime { get; set; } // In hours
        public double TimeSpent { get; set; }
    }
}
