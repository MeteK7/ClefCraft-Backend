using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Repositories
{
    public class BoardItemRepository : IBoardItemRepository
    {
        private readonly ClefCraftDatabaseContext _context;

        public BoardItemRepository(ClefCraftDatabaseContext context)
        {
            _context = context;
        }
        public async Task<List<BoardColumn>> GetBoardColumnsWithBoardItems()
        {
            return await _context.BoardColumns.Include(c => c.BoardItems).ToListAsync();
        }
        public async Task AddBoardItem(BoardItem boardItem)
        {
            await _context.BoardItems.AddAsync(boardItem);
            await _context.SaveChangesAsync();
        }
    }
}
