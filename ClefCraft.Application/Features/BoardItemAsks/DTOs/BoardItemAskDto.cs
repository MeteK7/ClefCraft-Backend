using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks.DTOs
{
    public class BoardItemAskDto
    {
        public int Id { get; set; }

        public int BoardItemId { get; set; }
        public string BoardItemTitle { get; set; } = null!;
        public int BoardId { get; set; }

        /// <summary>The board's title, when the board was loaded with the item (the caller's asks); null otherwise.</summary>
        public string? BoardTitle { get; set; }

        public string RequestedByUserId { get; set; } = null!;
        public string RequestedByFullName { get; set; } = null!;

        public string RecipientUserId { get; set; } = null!;
        public string RecipientFullName { get; set; } = null!;

        /// <summary>False when the recipient has since left the board.</summary>
        public bool RecipientIsBoardMember { get; set; }

        public string Note { get; set; } = null!;
        public DateTime? DueDate { get; set; }
        public bool IsRequired { get; set; }

        /// <summary>The ask can't be marked done until the recipient replies (see IsAnswered).</summary>
        public bool ExpectsAnswer { get; set; }

        /// <summary>The recipient has replied since the ask was last reopened.</summary>
        public bool IsAnswered { get; set; }

        public BoardItemAskState State { get; set; }
        public string? ResponseNote { get; set; }
        public DateTime? RespondedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
        public string? CompletedByUserId { get; set; }
        public string? CompletedByFullName { get; set; }

        public BoardItemAskCancelReason? CancelReason { get; set; }

        /// <summary>When it was cancelled; null while it isn't, or for a cancel with no record of it.</summary>
        public DateTime? CancelledAt { get; set; }
        public string? CancelledByUserId { get; set; }
        public string? CancelledByFullName { get; set; }

        /// <summary>When the ask was made: its age. Never reset by edits or nudges.</summary>
        public DateTime DateCreated { get; set; }

        public DateTime? LastNudgedAt { get; set; }

        /// <summary>
        /// Set only for a viewer who may nudge this ask once the cooldown ends: when that is.
        /// Null when they may nudge now (Nudge is in AllowedActions) or not at all.
        /// </summary>
        public DateTime? NextNudgeAt { get; set; }

        /// <summary>Send back as ExpectedRevision with any change.</summary>
        public int Revision { get; set; }

        /// <summary>Pending or Accepted.</summary>
        public bool IsOpen { get; set; }

        /// <summary>Required and not Completed: blocks completing the item until it is resolved.</summary>
        public bool IsUnmetRequirement { get; set; }

        /// <summary>What the current user may do with this ask right now.</summary>
        public List<BoardItemAskAction> AllowedActions { get; set; } = new();

        /// <summary>Why the ask is the current user's move (see AskTurn); null when it isn't theirs.</summary>
        public AskTurnReason? YourMove { get; set; }

        /// <summary>
        /// The current user may post a reply: they are the requester or the recipient and the ask
        /// isn't cancelled. Not one of AllowedActions, since replying is open while the item is Done.
        /// </summary>
        public bool CanReply { get; set; }

        /// <summary>The replies on this ask, oldest first.</summary>
        public List<AskReplyDto> Replies { get; set; } = new();

        /// <summary>
        /// The reply that answered the ask, fixed when it was marked done (one of Replies). Null
        /// while it isn't done, or when it was done without a reply.
        /// </summary>
        public int? AnswerReplyId { get; set; }
    }

    public class AskReplyDto
    {
        public int Id { get; set; }
        public string AuthorUserId { get; set; } = null!;
        public string AuthorFullName { get; set; } = null!;
        public string Body { get; set; } = null!;
        public DateTime DateCreated { get; set; }

        /// <summary>When the author last edited it; null when never edited.</summary>
        public DateTime? DateEdited { get; set; }

        /// <summary>Written by the current user, who may edit it.</summary>
        public bool IsMine { get; set; }
    }
}
