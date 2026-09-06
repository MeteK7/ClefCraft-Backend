using ClefCraft.Domain.Common;

namespace ClefCraft.Domain
{
    public class Comment : BaseEntity
    {
        public string EntityType { get; set; } // "BoardItem", "CalendarEvent"
        public int EntityId { get; set; }

        // Single-level reply-to only: a comment whose own ParentCommentId is
        // non-null can never be targeted by another reply (enforced in
        // CreateCommentCommandHandler, not just here).
        public int? ParentCommentId { get; set; }

        public string? BodyHtml { get; set; }

        // Soft-delete tombstone: the row (and its place among replies) is kept,
        // but BodyHtml is cleared on delete so deleted content isn't recoverable
        // via the API. First use of soft-delete in this codebase — everywhere
        // else uses a real EF delete — because a reply/mention can reference a
        // deleted comment.
        public bool IsDeleted { get; set; }
    }
}
