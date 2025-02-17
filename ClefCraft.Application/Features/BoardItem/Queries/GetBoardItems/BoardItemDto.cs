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
        public string Status { get; set; }
        public string Priority { get; set; } // Low, Medium, High
        public List<string> Tags { get; set; } = new List<string>();
        public string Assignee { get; set; }
        public DateTime DueDate { get; set; }
        public double EstimatedTime { get; set; } // In hours
        public double TimeSpent { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public string CreatedByFullName { get; set; }
        public string ModifiedByFullName { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
