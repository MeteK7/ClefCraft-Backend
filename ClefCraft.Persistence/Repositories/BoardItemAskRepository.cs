using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ClefCraft.Persistence.Repositories
{
    public class BoardItemAskRepository : GenericRepository<BoardItemAsk>, IBoardItemAskRepository
    {
        public BoardItemAskRepository(ClefCraftDatabaseContext context) : base(context)
        {
        }

        public async Task<List<BoardItemAsk>> GetByItemIdAsync(int boardItemId)
        {
            return await _context.BoardItemAsks
                .Where(a => a.BoardItemId == boardItemId)
                .OrderBy(a => a.Id)
                .ToListAsync();
        }

        public async Task<List<BoardItemAsk>> GetByItemIdsReadOnlyAsync(IEnumerable<int> boardItemIds)
        {
            var ids = boardItemIds as ICollection<int> ?? boardItemIds.ToList();
            if (ids.Count == 0) return new List<BoardItemAsk>();

            return await _context.BoardItemAsks
                .Where(a => ids.Contains(a.BoardItemId))
                .OrderBy(a => a.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<BoardItemAsk>> GetOpenByRecipientOnBoardAsync(int boardId, string recipientUserId)
        {
            return await _context.BoardItemAsks
                .Include(a => a.BoardItem)
                .Where(a => a.BoardItem.BoardId == boardId
                    && a.RecipientUserId == recipientUserId
                    && (a.State == BoardItemAskState.Pending || a.State == BoardItemAskState.Accepted))
                .OrderBy(a => a.Id)
                .ToListAsync();
        }

        public async Task<List<BoardItemAsk>> GetWithItemReadOnlyAsync(Expression<Func<BoardItemAsk, bool>> filter)
        {
            return await _context.BoardItemAsks
                .Include(a => a.BoardItem)
                    .ThenInclude(i => i.Board)
                .Include(a => a.BoardItem)
                    .ThenInclude(i => i.BoardItemStatus)
                        .ThenInclude(s => s.Status)
                .Where(filter)
                .OrderBy(a => a.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<BoardItemAskReply>> GetRepliesByAskIdsReadOnlyAsync(IEnumerable<int> askIds)
        {
            var ids = askIds as ICollection<int> ?? askIds.ToList();
            if (ids.Count == 0) return new List<BoardItemAskReply>();

            return await _context.BoardItemAskReplies
                .Where(r => ids.Contains(r.BoardItemAskId))
                .OrderBy(r => r.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<BoardItemAskReply?> GetReplyByIdAsync(int replyId)
        {
            return await _context.BoardItemAskReplies.FirstOrDefaultAsync(r => r.Id == replyId);
        }

        public async Task AddReplyAsync(BoardItemAskReply reply)
        {
            await _context.BoardItemAskReplies.AddAsync(reply);
        }
    }
}
