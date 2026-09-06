using ClefCraft.Domain.Common;

namespace ClefCraft.Domain
{
    public class CommentMention : BaseEntity
    {
        public int CommentId { get; set; }
        public string MentionedUserId { get; set; }
    }
}
