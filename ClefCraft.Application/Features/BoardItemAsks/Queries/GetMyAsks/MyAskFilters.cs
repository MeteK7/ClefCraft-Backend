using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using System.Linq.Expressions;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks
{
    // The two lists' rules, as expressions: the repository runs them in the database, and they can
    // be checked against in-memory asks. Both rely on BoardItem being loaded.
    public static class MyAskFilters
    {
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
