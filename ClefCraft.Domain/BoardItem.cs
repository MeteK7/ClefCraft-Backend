using ClefCraft.Domain.Common;
using System;
using System.Collections.Generic;

namespace ClefCraft.Domain
{
    public class BoardItem : BaseEntity
    {
        public int BoardId { get; set; }
        public Board Board { get; set; } = null!;

        public int BoardColumnId { get; set; }
        public BoardColumn BoardColumn { get; set; } = null!;

        // ONE status
        public BoardItemStatus BoardItemStatus { get; set; } = null!;

        // ONE priority
        public BoardItemPriority BoardItemPriority { get; set; } = null!;

        // MANY tags
        public ICollection<BoardItemTag> BoardItemTags { get; set; }
            = new List<BoardItemTag>();

        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? AssigneeId { get; set; }
        public DateTime? DueDate { get; set; }
        public double? EstimatedTime { get; set; }
        public double? TimeSpent { get; set; }

        public ICollection<BoardItemRelation> OutgoingRelations
    = new List<BoardItemRelation>();

        public ICollection<BoardItemRelation> IncomingRelations
            = new List<BoardItemRelation>();
    }
}
