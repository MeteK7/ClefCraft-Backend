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
    public class BoardRepository: GenericRepository<Board>, IBoardRepository
    {
        public BoardRepository(ClefCraftDatabaseContext context) : base(context)
        {
        }

        public async Task<List<Board>> GetBoards(string userId)
        {
            var memberBoardIds = _context.BoardMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.BoardId);

            return await _context.Boards
                .Where(b => memberBoardIds.Contains(b.Id))
                .Select(b => new Board
            {
                Id = b.Id,
                Title = b.Title,
                OwnerUserId = b.OwnerUserId
            })
        .ToListAsync();
        }

        //public async Task<List<Board>> GetBoards()
        //{
        //    return await _context.Boards.Include(c => c.BoardColumns).ToListAsync();
        //}
    }
}
