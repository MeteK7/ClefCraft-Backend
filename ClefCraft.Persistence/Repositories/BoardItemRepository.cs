using Azure.Core;
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
            return await _context.BoardColumns
                .Where(bc =>
                    _context.BoardColumnMappings
                        .Any(m => m.BoardId == boardId && m.BoardColumnId == bc.Id))
                .Include(bc => bc.BoardItems
                    .Where(bi => bi.BoardId == boardId))
                    .ThenInclude(bi => bi.BoardItemStatus)
                        .ThenInclude(s => s.Status)
                .Include(bc => bc.BoardItems
                    .Where(bi => bi.BoardId == boardId))
                    .ThenInclude(bi => bi.BoardItemPriority)
                        .ThenInclude(p => p.Priority)
                .Include(bc => bc.BoardItems
                    .Where(bi => bi.BoardId == boardId))
                    .ThenInclude(bi => bi.BoardItemTags)
                        .ThenInclude(t => t.Tag)
                .ToListAsync();
        }

        public async Task<BoardItemStatus?> GetBoardItemStatusByBoardItemId(int boardItemId)
        {
            return await _context.BoardItemStatuses
                .Include(x => x.Status)
                .FirstOrDefaultAsync(x => x.BoardItemId == boardItemId);
        }

        public async Task<BoardItemPriority?> GetBoardItemPriorityByBoardItemId(int boardItemId)
        {
            return await _context.BoardItemPriorities
                .Include(x => x.Priority)
                .FirstOrDefaultAsync(x => x.BoardItemId == boardItemId);
        }

        public async Task<List<BoardItemTag>> GetBoardItemTagsByBoardItemId(int boardItemId)
        {
            return await _context.BoardItemTags
                                 .Where(bit => bit.BoardItemId == boardItemId)
                                 .Include(bit => bit.Tag) // Include related Tag
                                 .ToListAsync(); // Get the list of BoardItemTags
        }


        public async Task<BoardItem?> GetBoardItemById(int id)
        {
            return await _context.BoardItems
                .Include(b => b.BoardItemStatus)
                    .ThenInclude(s => s.Status)
                .Include(b => b.BoardItemPriority)
                    .ThenInclude(p => p.Priority)
                .Include(b => b.BoardItemTags)
                    .ThenInclude(t => t.Tag)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<List<BoardItem>> GetByIdsAsync(List<int> ids)
        {
            return await _context.BoardItems
                .Where(bi => ids.Contains(bi.Id))
                .ToListAsync();
        }

        public async Task AddBoardItem(BoardItem boardItem)
        {
            await _context.BoardItems.AddAsync(boardItem);
        }

        public async Task UpdateBoardItem(BoardItem boardItem)
        {
            _context.BoardItems.Update(boardItem);
        }
    }
}
