using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;

namespace ClefCraft.Domain
{
    public class BoardItem : BaseEntity
    {
        public int BoardId { get; set; }
        public Board Board { get; set; }

        public int BoardColumnId { get; set; }
        public BoardColumn BoardColumn { get; set; }

        // ONE status
        public BoardItemStatus BoardItemStatus { get; set; }

        // ONE priority
        public BoardItemPriority BoardItemPriority { get; set; }

        // MANY tags
        public ICollection<BoardItemTag> BoardItemTags { get; set; }
            = new List<BoardItemTag>();

        public string Title { get; set; }
        public string? Description { get; set; }
        public string? AssigneeId { get; set; }
        public DateTime? DueDate { get; set; }
        public double? EstimatedTime { get; set; }
        public double? TimeSpent { get; set; }
    }
}
