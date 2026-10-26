using System.Linq.Expressions;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;

namespace ClefCraft.Application.Features.BoardItemAsks
{
    /// <summary>Why an ask is someone's move.</summary>
    public enum AskTurnReason
    {
        /// <summary>The recipient decides: accept or decline (Pending).</summary>
        Respond = 0,

        /// <summary>The recipient delivers what they accepted: mark it done, or decline after all (Accepted).</summary>
        Deliver = 1,

        /// <summary>
        /// The requester finds someone else or waives the requirement: a required ask that was
        /// declined or cancelled still blocks completing the item.
        /// </summary>
        ResolveRequirement = 2
    }

    /// <summary>
    /// Whose move an ask is: the one person who has to act next, or nobody. Derived from the ask's
    /// state only, never from what someone could optionally do (edit, nudge, withdraw): replies,
    /// nudges and due dates never make an ask anyone's move. Nothing is anyone's move while the item
    /// is Done, since no ask can change then. Every move can be cleared by its owner:
    /// <list type="bullet">
    /// <item>Pending: the recipient accepts or declines.</item>
    /// <item>Accepted: the recipient marks it done or declines.</item>
    /// <item>Declined or Cancelled, required: the requester asks again with the requirement, or waives
    /// it (the requester is always an ask manager).</item>
    /// </list>
    /// A declined or cancelled ask that isn't required is nobody's move: there's nothing to clear it
    /// with (Cancel only applies to open asks), so it's news, not an obligation.
    /// </summary>
    public static class AskTurn
    {
        public readonly record struct Move(string UserId, AskTurnReason Reason);

        public static Move? Of(BoardItemAsk ask, bool itemIsDone)
        {
            if (itemIsDone)
                return null;

            return ask.State switch
            {
                BoardItemAskState.Pending => new Move(ask.RecipientUserId, AskTurnReason.Respond),
                BoardItemAskState.Accepted => new Move(ask.RecipientUserId, AskTurnReason.Deliver),
                BoardItemAskState.Declined or BoardItemAskState.Cancelled when ask.IsRequired =>
                    new Move(ask.RequestedByUserId, AskTurnReason.ResolveRequirement),
                _ => null
            };
        }

        /// <summary>The caller's move on the ask, if it is theirs.</summary>
        public static AskTurnReason? ReasonFor(BoardItemAsk ask, string callerId, bool itemIsDone) =>
            Of(ask, itemIsDone) is { } move && move.UserId == callerId ? move.Reason : null;

        /// <summary>
        /// The same rule as <see cref="Of"/>, as a query: asks that are the user's move on the given
        /// boards, on items that aren't Done. Kept next to Of so the two can't drift apart; the tests
        /// run both over the same cases.
        /// </summary>
        public static Expression<Func<BoardItemAsk, bool>> IsYourMove(
            string userId, IReadOnlyCollection<int> boardIds, IReadOnlyCollection<int> doneStatusIds) =>
            a => boardIds.Contains(a.BoardItem.BoardId)
                && (a.BoardItem.BoardItemStatus == null || !doneStatusIds.Contains(a.BoardItem.BoardItemStatus.StatusId))
                && ((a.RecipientUserId == userId
                        && (a.State == BoardItemAskState.Pending || a.State == BoardItemAskState.Accepted))
                    || (a.RequestedByUserId == userId
                        && a.IsRequired
                        && (a.State == BoardItemAskState.Declined || a.State == BoardItemAskState.Cancelled)));
    }
}
