using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace ClefCraft.Persistence.Repositories
{
    public class BoardMemberRepository : GenericRepository<BoardMember>, IBoardMemberRepository
    {
        public BoardMemberRepository(ClefCraftDatabaseContext context) : base(context)
        {
        }

        public async Task<bool> IsMemberAsync(int boardId, string userId)
        {
            return await _context.BoardMembers
                .AnyAsync(m => m.BoardId == boardId && m.UserId == userId);
        }

        public async Task<List<BoardMember>> GetByBoardIdAsync(int boardId)
        {
            return await _context.BoardMembers
                .Where(m => m.BoardId == boardId)
                .ToListAsync();
        }

        public async Task<BoardMember?> GetByBoardAndUserAsync(int boardId, string userId)
        {
            return await _context.BoardMembers
                .FirstOrDefaultAsync(m => m.BoardId == boardId && m.UserId == userId);
        }
    }
}
