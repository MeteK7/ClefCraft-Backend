using ClefCraft.Domain.Common;

namespace ClefCraft.Domain
{
    // A plain-text message on an ask from its requester or recipient: the answer itself, or the
    // conversation around it. Replies are never deleted; DateCreated (from BaseEntity) is when it
    // was posted.
    public class BoardItemAskReply : BaseEntity
    {
        public int BoardItemAskId { get; set; }
        public BoardItemAsk BoardItemAsk { get; set; } = null!;

        public string AuthorUserId { get; set; } = null!;

        public string Body { get; set; } = null!;

        /// <summary>When the author last edited the body; null if it was never edited.</summary>
        public DateTime? DateEdited { get; set; }
    }
}
