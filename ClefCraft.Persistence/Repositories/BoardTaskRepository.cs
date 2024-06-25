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
    public class BoardTaskRepository : IBoardTaskRepository
    {
        private readonly ClefCraftDatabaseContext _context;

        public BoardTaskRepository(ClefCraftDatabaseContext context)
        {
            _context = context;
        }
        public async Task<List<BoardColumn>> GetBoardColumnsWithBoardTasks()
        {
            return await _context.BoardColumns.Include(c => c.BoardTasks).ToListAsync();
        }
        public async Task AddBoardTask(BoardTask boardTask)
        {
            await _context.BoardTasks.AddAsync(boardTask);
            await _context.SaveChangesAsync();
        }
    }
}
