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
    public class BoardItemRepository : GenericRepository<BoardItem>, IBoardItemRepository
    {
        public BoardItemRepository(ClefCraftDatabaseContext context) : base(context)
        {
        }

        public async Task<List<BoardColumn>> GetAllBoardColumnsWithItems()
        {
            var columnMappings = await _context.BoardColumnMappings
                                               .Include(m => m.BoardColumn)
                                               .ThenInclude(c => c.BoardItems)
                                               .ToListAsync();

            return columnMappings.Select(m => m.BoardColumn).ToList();
        }

        public async Task<List<BoardColumn>> GetBoardColumnsWithBoardItems(int boardId)
        {
            var columnMappings = await _context.BoardColumnMappings
                                               .Where(m => m.BoardId == boardId)
                                               .Include(m => m.BoardColumn)
                                               .ThenInclude(c => c.BoardItems
                                                                  .Where(i => i.BoardId == boardId)) // 🔹 FILTER ITEMS BY BOARD
                                               .ToListAsync();

            return columnMappings.Select(m => m.BoardColumn).ToList();
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
