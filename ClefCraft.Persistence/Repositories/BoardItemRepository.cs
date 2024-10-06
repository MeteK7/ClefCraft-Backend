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
        public async Task<List<BoardColumn>> GetAllBoardColumnsWithItems()
        {
            return await _context.BoardColumns
                                 .Include(c => c.BoardItems)
                                 .ToListAsync();
        }

        public async Task<List<BoardColumn>> GetBoardColumnsWithBoardItems(int boardId)
        {
            return await _context.BoardColumns
                                 .Where(c => c.BoardId == boardId)  // Filter by boardId
                                 .Include(c => c.BoardItems)
                                 .ToListAsync();
        }
        public async Task<BoardItem> GetBoardItemById(int id)
        {
            return await _context.BoardItems.FirstOrDefaultAsync(bi => bi.Id == id);
        }

        public async Task AddBoardItem(BoardItem boardItem)
        {
            await _context.BoardItems.AddAsync(boardItem);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception or inspect ex.InnerException for more details
                throw new ApplicationException("An error occurred while saving the BoardItem", ex);
            }
        }

        public async Task UpdateBoardItem(BoardItem boardItem)
        {
            _context.BoardItems.Update(boardItem); // Mark the entity as modified

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new ApplicationException("An error occurred while updating the BoardItem", ex);
            }
        }
    }
}
