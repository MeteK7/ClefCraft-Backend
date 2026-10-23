using ClefCraft.Domain.Common;
using ClefCraft.Domain.Enums;

namespace ClefCraft.Domain
{
    // A request from one board member to another to help with an item, and the recipient's answer
    // to it. It exists from the moment someone is asked, so it can be pending, declined or
    // cancelled without any help having been given. DateCreated (from BaseEntity) is the ask's
    // age and never changes after the insert.
    public class BoardItemAsk : BaseEntity
    {
        public int BoardItemId { get; set; }
        public BoardItem BoardItem { get; set; } = null!;

        public string RequestedByUserId { get; set; } = null!;
        public string RecipientUserId { get; set; } = null!;

        public string Note { get; set; } = null!;
        public DateTime? DueDate { get; set; }

        // Completing the item needs this ask Completed, or the requirement explicitly moved,
        // waived or overridden. Declining or cancelling the ask doesn't satisfy it.
        public bool IsRequired { get; set; }

        // The ask can only be marked done once the recipient has replied (since the last reopen):
        // the reply is the deliverable, not just the status.
        public bool ExpectsAnswer { get; set; }

        public BoardItemAskState State { get; set; }

        /// <summary>The recipient's optional reason when declining.</summary>
        public string? ResponseNote { get; set; }

        /// <summary>When the recipient last accepted or declined.</summary>
        public DateTime? RespondedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        /// <summary>When the recipient last reopened the ask; replies before it don't answer it.</summary>
        public DateTime? LastReopenedAt { get; set; }

        // The reply that answered the ask, fixed when it was marked done: the reply sent with it,
        // or else the recipient's latest since the last reopen. Later replies are follow-ups, not
        // the answer. Null while the ask isn't done, or when it was done without a reply.
        public int? AnswerReplyId { get; set; }
        public BoardItemAskReply? AnswerReply { get; set; }

        // Who marked it done. The recipient always gets the credit; this differs from
        // RecipientUserId when someone completed the item and marked the ask done for them.
        public string? CompletedByUserId { get; set; }

        public BoardItemAskCancelReason? CancelReason { get; set; }

        public DateTime? LastNudgedAt { get; set; }

        // Concurrency token, incremented on every change except a nudge, so a nudge never makes
        // a recipient's answer look stale.
        public int Revision { get; set; }

        public ICollection<BoardItemAskReply> Replies { get; set; } = new List<BoardItemAskReply>();
    }
}
