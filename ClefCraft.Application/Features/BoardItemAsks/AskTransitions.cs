using ClefCraft.Application.Exceptions;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    public enum BoardItemAskAction
    {
        Accept = 0,
        Decline = 1,
        Complete = 2,
        Reopen = 3,
        Cancel = 4,

        /// <summary>Change the note or due date. An Accepted ask goes back to Pending.</summary>
        Edit = 5,

        SetRequired = 6,

        /// <summary>Clear IsRequired, so the ask no longer blocks completing the item.</summary>
        WaiveRequirement = 7,

        Nudge = 8,

        /// <summary>Cancel an open ask and ask someone else instead.</summary>
        Reassign = 9,

        /// <summary>Ask again after a decline or cancellation (a new ask).</summary>
        AskAgain = 10,

        /// <summary>Mark an Accepted ask done while completing the item.</summary>
        CompleteOnBehalf = 11
    }

    /// <summary>
    /// How the caller relates to one ask. Ask managers are the requester, the item's current
    /// assignee and the board owner; being the assignee is evaluated now, so a recipient who later
    /// becomes the assignee also manages their own asks.
    /// </summary>
    public readonly record struct AskActorRoles(bool IsRecipient, bool IsManager)
    {
        public static AskActorRoles For(BoardItemAsk ask, string callerId, string? assigneeId, string? boardOwnerId) =>
            new(
                IsRecipient: ask.RecipientUserId == callerId,
                IsManager: ask.RequestedByUserId == callerId
                    || (!string.IsNullOrEmpty(assigneeId) && assigneeId == callerId)
                    || (!string.IsNullOrEmpty(boardOwnerId) && boardOwnerId == callerId));
    }

    // The ask lifecycle in one place: who may take each action, and from which states. Every
    // command checks it, and AllowedActions reports it to the client, so the UI offers exactly what
    // the server accepts.
    public static class AskTransitions
    {
        private enum Actor { Recipient, Manager, RecipientOrManager }

        private sealed record Rule(Actor Actor, BoardItemAskState[] From);

        private static readonly IReadOnlyDictionary<BoardItemAskAction, Rule> Rules =
            new Dictionary<BoardItemAskAction, Rule>
            {
                [BoardItemAskAction.Accept] = new(Actor.Recipient, [BoardItemAskState.Pending]),
                [BoardItemAskAction.Decline] = new(Actor.Recipient, [BoardItemAskState.Pending, BoardItemAskState.Accepted]),
                [BoardItemAskAction.Complete] = new(Actor.Recipient, [BoardItemAskState.Accepted]),
                [BoardItemAskAction.Reopen] = new(Actor.Recipient, [BoardItemAskState.Completed]),
                [BoardItemAskAction.Cancel] = new(Actor.Manager, [BoardItemAskState.Pending, BoardItemAskState.Accepted]),
                [BoardItemAskAction.Edit] = new(Actor.Manager, [BoardItemAskState.Pending, BoardItemAskState.Accepted]),
                [BoardItemAskAction.SetRequired] = new(Actor.Manager, [BoardItemAskState.Pending, BoardItemAskState.Accepted]),
                [BoardItemAskAction.WaiveRequirement] = new(Actor.Manager,
                    [BoardItemAskState.Pending, BoardItemAskState.Accepted, BoardItemAskState.Declined, BoardItemAskState.Cancelled]),
                [BoardItemAskAction.Nudge] = new(Actor.Manager, [BoardItemAskState.Pending]),
                [BoardItemAskAction.Reassign] = new(Actor.Manager, [BoardItemAskState.Pending, BoardItemAskState.Accepted]),
                [BoardItemAskAction.AskAgain] = new(Actor.Manager, [BoardItemAskState.Declined, BoardItemAskState.Cancelled]),
                [BoardItemAskAction.CompleteOnBehalf] = new(Actor.RecipientOrManager, [BoardItemAskState.Accepted])
            };

        public static bool IsOpen(BoardItemAskState state) =>
            state is BoardItemAskState.Pending or BoardItemAskState.Accepted;

        /// <summary>
        /// A required ask that isn't Completed, whatever its state: declining, withdrawing or the
        /// recipient leaving doesn't satisfy a requirement.
        /// </summary>
        public static bool IsUnmetRequirement(BoardItemAsk ask) =>
            ask.IsRequired && ask.State != BoardItemAskState.Completed;

        /// <summary>The caller's role allows the action (ignoring the ask's state).</summary>
        public static bool HasRole(BoardItemAskAction action, AskActorRoles roles) =>
            Rules[action].Actor switch
            {
                Actor.Recipient => roles.IsRecipient,
                Actor.Manager => roles.IsManager,
                _ => roles.IsRecipient || roles.IsManager
            };

        /// <summary>The ask's current state (and requirement) allows the action, whoever asks.</summary>
        public static bool IsAllowedFrom(BoardItemAskAction action, BoardItemAsk ask)
        {
            if (!Rules[action].From.Contains(ask.State))
                return false;

            return action switch
            {
                BoardItemAskAction.SetRequired => !ask.IsRequired,
                BoardItemAskAction.WaiveRequirement => ask.IsRequired,
                _ => true
            };
        }

        public static bool IsNudgeDue(BoardItemAsk ask, DateTime utcNow) =>
            ask.LastNudgedAt == null || utcNow - ask.LastNudgedAt.Value >= BoardItemAskLimits.NudgeInterval;

        /// <summary>
        /// Throws ForbiddenAccessException when the caller's role doesn't allow the action, and
        /// BadRequestException when the item is Done or the ask's state doesn't allow it.
        /// </summary>
        public static void EnsureAllowed(BoardItemAskAction action, BoardItemAsk ask, AskActorRoles roles, bool itemIsDone)
        {
            if (!HasRole(action, roles))
                throw new ForbiddenAccessException();

            if (itemIsDone)
                throw new BadRequestException("Asks can't change while the item is Done.");

            if (!IsAllowedFrom(action, ask))
                throw new BadRequestException($"{action} isn't possible on this ask in its current state ({ask.State}).");
        }

        /// <summary>The ask must still be the version the caller saw.</summary>
        public static void EnsureRevision(BoardItemAsk ask, int expectedRevision)
        {
            if (ask.Revision != expectedRevision)
                throw new BadRequestException("This ask changed since you loaded it; reload it and try again.");
        }

        /// <summary>Everything the caller may do with the ask right now. Empty while the item is Done.</summary>
        public static List<BoardItemAskAction> AllowedActions(BoardItemAsk ask, AskActorRoles roles, bool itemIsDone, DateTime utcNow)
        {
            if (itemIsDone)
                return new List<BoardItemAskAction>();

            return Rules.Keys
                .Where(action => HasRole(action, roles) && IsAllowedFrom(action, ask))
                .Where(action => action != BoardItemAskAction.Nudge || IsNudgeDue(ask, utcNow))
                .OrderBy(action => action)
                .ToList();
        }
    }
}
