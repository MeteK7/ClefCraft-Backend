using ClefCraft.Domain.Common;
using ClefCraft.Domain.Enums;

namespace ClefCraft.Domain
{
    public class BoardItemRelation : BaseEntity
    {
        public int SourceBoardItemId { get; set; }

        public BoardItem SourceBoardItem { get; set; } = null!;

        public int TargetBoardItemId { get; set; }

        public BoardItem TargetBoardItem { get; set; } = null!;

        public BoardItemRelationType RelationType { get; set; }
    }
}