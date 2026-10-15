namespace ClefCraft.Application.Models.Notifications
{
    /// <summary>The "ReceiveAsk" SignalR payload: something happened to an ask the receiver is part of.</summary>
    public class AskNotification
    {
        public int AskId { get; set; }
        public int BoardItemId { get; set; }

        /// <summary>Lets the client deep-link to the right board, as the mention toast does.</summary>
        public int BoardId { get; set; }

        public string ItemTitle { get; set; } = null!;

        /// <summary>Requested, Changed, Nudged, Accepted, Declined, Completed, Reopened or Cancelled.</summary>
        public string Event { get; set; } = null!;

        public string ActorFullName { get; set; } = null!;
        public string Note { get; set; } = null!;

        /// <summary>The decline reason, or the cancel reason (Withdrawn, ItemCompleted, RecipientRemoved).</summary>
        public string? Reason { get; set; }

        /// <summary>True when someone other than the recipient marked the ask done.</summary>
        public bool ActedOnBehalf { get; set; }
    }
}
