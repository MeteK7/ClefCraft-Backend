using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using System.Linq.Expressions;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks
{
    // The lists' rules, as expressions: the repository runs them in the database, and they can be
    // checked against in-memory asks. They rely on BoardItem being loaded.
    public static class MyAskFilters
    {
        /// <summary>The rule for one list; Waiting is AskTurn's, shared with the board cards.</summary>
        public static Expression<Func<BoardItemAsk, bool>> For(
            AskDirection direction, string userId, List<int> boardIds, DateTime utcNow, IReadOnlyCollection<int> doneStatusIds) =>
            direction switch
            {
                AskDirection.Received => Received(userId, boardIds),
                AskDirection.Sent => Sent(userId, boardIds, utcNow - BoardItemAskLimits.SentDeclinedWindow),
                _ => AskTurn.IsYourMove(userId, boardIds, doneStatusIds)
            };

        public static Expression<Func<BoardItemAsk, bool>> Received(string userId, List<int> boardIds) =>
            a => a.RecipientUserId == userId
                && boardIds.Contains(a.BoardItem.BoardId)
                && (a.State == BoardItemAskState.Pending || a.State == BoardItemAskState.Accepted);

        public static Expression<Func<BoardItemAsk, bool>> Sent(string userId, List<int> boardIds, DateTime declinedSince) =>
            a => a.RequestedByUserId == userId
                && boardIds.Contains(a.BoardItem.BoardId)
                && (a.State == BoardItemAskState.Pending
                    || a.State == BoardItemAskState.Accepted
                    || (a.State == BoardItemAskState.Declined && a.RespondedAt >= declinedSince));
    }
}
