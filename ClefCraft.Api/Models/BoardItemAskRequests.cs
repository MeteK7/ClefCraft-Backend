using System.ComponentModel.DataAnnotations;

namespace ClefCraft.Api.Models
{
    // Bodies of the BoardItemAsks endpoints. The item or ask comes from the route and the caller
    // from the token, so neither can be supplied (or spoofed) here. Required members are checked by
    // [ApiController] model validation (400 with field errors); the commands' own validators check
    // the rest.

    /// <summary>Body of POST api/BoardItems/{itemId}/asks.</summary>
    public class CreateAskRequest
    {
        [Required]
        public string RecipientUserId { get; set; } = string.Empty;

        [Required]
        public string Note { get; set; } = string.Empty;

        public DateTime? DueDate { get; set; }

        public bool IsRequired { get; set; }

        /// <summary>The ask can only be marked done once the recipient has replied.</summary>
        public bool ExpectsAnswer { get; set; }

        /// <summary>"Ask again": the Declined or Cancelled ask on the same item this one follows up.</summary>
        public int? ReplacesAskId { get; set; }
    }

    /// <summary>Body of PUT api/BoardItemAsks/{id}: the ask's full terms, current values for what isn't changing.</summary>
    public class UpdateAskRequest
    {
        [Required]
        public int? ExpectedRevision { get; set; }

        [Required]
        public string Note { get; set; } = string.Empty;

        public DateTime? DueDate { get; set; }

        public bool IsRequired { get; set; }

        public string? WaiveReason { get; set; }
    }

    /// <summary>Body of POST api/BoardItemAsks/{id}/accept, decline, complete, reopen and cancel.</summary>
    public class AskStateChangeRequest
    {
        [Required]
        public int? ExpectedRevision { get; set; }

        /// <summary>The recipient's reason, used when declining.</summary>
        public string? Reason { get; set; }

        /// <summary>Complete only: a reply to post with it ("Send & mark done").</summary>
        public string? ReplyBody { get; set; }
    }

    /// <summary>Body of POST api/BoardItemAsks/{id}/replies and PUT api/BoardItemAskReplies/{replyId}.</summary>
    public class AskReplyRequest
    {
        [Required]
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>Body of POST api/BoardItemAsks/{id}/expects-answer.</summary>
    public class SetAskExpectsAnswerRequest
    {
        [Required]
        public int? ExpectedRevision { get; set; }

        [Required]
        public bool? ExpectsAnswer { get; set; }
    }

    /// <summary>Body of POST api/BoardItemAsks/{id}/reassign.</summary>
    public class ReassignAskRequest
    {
        [Required]
        public int? ExpectedRevision { get; set; }

        [Required]
        public string NewRecipientUserId { get; set; } = string.Empty;

        /// <summary>Null keeps the current note.</summary>
        public string? Note { get; set; }

        /// <summary>Null keeps the current due date.</summary>
        public DateTime? DueDate { get; set; }
    }
}
