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
            return await _context.Boards
                .Where(b => b.OwnerUserId == userId)
                .Select(b => new Board
            {
                Id = b.Id,
                Title = b.Title
            })
        .ToListAsync();
        }

        //public async Task<List<Board>> GetBoards()
        //{
        //    return await _context.Boards.Include(c => c.BoardColumns).ToListAsync();
        //}
    }
}
