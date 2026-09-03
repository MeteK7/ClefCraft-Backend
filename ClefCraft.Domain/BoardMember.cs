using ClefCraft.Domain.Common;

namespace ClefCraft.Domain
{
    public class BoardMember : BaseEntity
    {
        public int BoardId { get; set; }

        public Board Board { get; set; } = null!;

        public string UserId { get; set; } = null!;
    }
}
