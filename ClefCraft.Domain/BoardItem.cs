using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;

namespace ClefCraft.Domain
{
    public class BoardItem : BaseEntity
    {
        public string Title { get; set; }
        public string? Description { get; set; }

        public int StatusId { get; set; }
        public Status Status { get; set; }

        public int PriorityId { get; set; }
        public Priority Priority { get; set; }

        public ICollection<BoardItemTag> BoardItemTags { get; set; } = new List<BoardItemTag>();

        public string? Assignee { get; set; }
        public DateTime? DueDate { get; set; }
        public double? EstimatedTime { get; set; } // In hours
        public double? TimeSpent { get; set; }

        public int BoardColumnId { get; set; }
        public BoardColumn BoardColumn { get; set; }

        public int BoardId { get; set; }
        public Board Board { get; set; }
    }
}
