using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Repositories
{
    public class BoardItemRelationRepository
        : IBoardItemRelationRepository
    {
        private readonly ClefCraftDatabaseContext _context;

        public BoardItemRelationRepository(
            ClefCraftDatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<BoardItemRelation>> GetForItemAsync(int itemId)
        {
            return await _context.BoardItemRelations
                .Include(x => x.SourceBoardItem)
                    .ThenInclude(i => i.BoardItemStatus)
                        .ThenInclude(s => s.Status)
                .Include(x => x.SourceBoardItem)
                    .ThenInclude(i => i.BoardItemPriority)
                        .ThenInclude(p => p.Priority)
                .Include(x => x.TargetBoardItem)
                    .ThenInclude(i => i.BoardItemStatus)
                        .ThenInclude(s => s.Status)
                .Include(x => x.TargetBoardItem)
                    .ThenInclude(i => i.BoardItemPriority)
                        .ThenInclude(p => p.Priority)
                .Where(x =>
                    x.SourceBoardItemId == itemId ||
                    x.TargetBoardItemId == itemId)
                .ToListAsync();
        }

        public async Task AddAsync(BoardItemRelation relation)
        {
            await _context.BoardItemRelations.AddAsync(relation);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.BoardItemRelations.FindAsync(id);

            if (entity != null)
                _context.BoardItemRelations.Remove(entity);
        }

        public async Task<bool> ExistsAsync(
            int sourceId,
            int targetId,
            int relationType)
        {
            return await _context.BoardItemRelations.AnyAsync(x =>
                x.SourceBoardItemId == sourceId &&
                x.TargetBoardItemId == targetId &&
                (int)x.RelationType == relationType);
        }

        public async Task<List<BoardItem>> SearchItemsAsync(
            string search,
            int excludeItemId)
        {
            search = search.ToLower();

            return await _context.BoardItems
                .Include(x => x.BoardItemStatus)
                    .ThenInclude(x => x.Status)
                .Include(x => x.BoardItemPriority)
                    .ThenInclude(x => x.Priority)
                .Where(x =>
                    x.Id != excludeItemId &&
                    (
                        x.Title.ToLower().Contains(search)
                        ||
                        x.Id.ToString().Contains(search)
                    ))
                .OrderBy(x => x.Title)
                .Take(20)
                .ToListAsync();
        }
    }
}