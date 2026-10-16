using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks.DTOs
{
    public class BoardItemAskDto
    {
        public int Id { get; set; }

        public int BoardItemId { get; set; }
        public string BoardItemTitle { get; set; } = null!;
        public int BoardId { get; set; }

        public string RequestedByUserId { get; set; } = null!;
        public string RequestedByFullName { get; set; } = null!;

        public string RecipientUserId { get; set; } = null!;
        public string RecipientFullName { get; set; } = null!;

        /// <summary>False when the recipient has since left the board.</summary>
        public bool RecipientIsBoardMember { get; set; }

        public string Note { get; set; } = null!;
        public DateTime? DueDate { get; set; }
        public bool IsRequired { get; set; }

        public BoardItemAskState State { get; set; }
        public string? ResponseNote { get; set; }
        public DateTime? RespondedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
        public string? CompletedByUserId { get; set; }
        public string? CompletedByFullName { get; set; }

        public BoardItemAskCancelReason? CancelReason { get; set; }

        /// <summary>When the ask was made: its age. Never reset by edits or nudges.</summary>
        public DateTime DateCreated { get; set; }

        public DateTime? LastNudgedAt { get; set; }

        /// <summary>Send back as ExpectedRevision with any change.</summary>
        public int Revision { get; set; }

        /// <summary>Pending or Accepted.</summary>
        public bool IsOpen { get; set; }

        /// <summary>Required and not Completed: blocks completing the item until it is resolved.</summary>
        public bool IsUnmetRequirement { get; set; }

        /// <summary>What the current user may do with this ask right now.</summary>
        public List<BoardItemAskAction> AllowedActions { get; set; } = new();
    }
}
