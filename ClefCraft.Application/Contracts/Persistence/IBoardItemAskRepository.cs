using ClefCraft.Domain;
using System.Linq.Expressions;

namespace ClefCraft.Application.Contracts.Persistence
{
    public interface IBoardItemAskRepository : IGenericRepository<BoardItemAsk>
    {
        /// <summary>Every ask on the item, tracked, oldest first.</summary>
        Task<List<BoardItemAsk>> GetByItemIdAsync(int boardItemId);

        /// <summary>Every ask on the given items, read-only, oldest first. One query.</summary>
        Task<List<BoardItemAsk>> GetByItemIdsReadOnlyAsync(IEnumerable<int> boardItemIds);

        /// <summary>The recipient's Pending and Accepted asks on items of the board, tracked, with their item.</summary>
        Task<List<BoardItemAsk>> GetOpenByRecipientOnBoardAsync(int boardId, string recipientUserId);

        /// <summary>
        /// Asks matching the filter, read-only, with their item, the item's board and its status
        /// loaded. The filter can rely on BoardItem being loaded.
        /// </summary>
        Task<List<BoardItemAsk>> GetWithItemReadOnlyAsync(Expression<Func<BoardItemAsk, bool>> filter);

        /// <summary>How many asks match the filter: one COUNT query. The filter can rely on BoardItem as above.</summary>
        Task<int> CountAsync(Expression<Func<BoardItemAsk, bool>> filter);

        /// <summary>The replies on the given asks, read-only, oldest first. One query.</summary>
        Task<List<BoardItemAskReply>> GetRepliesByAskIdsReadOnlyAsync(IEnumerable<int> askIds);

        /// <summary>One reply, tracked; null when it doesn't exist.</summary>
        Task<BoardItemAskReply?> GetReplyByIdAsync(int replyId);

        /// <summary>Adds a reply; the next save inserts it.</summary>
        Task AddReplyAsync(BoardItemAskReply reply);
    }
}
