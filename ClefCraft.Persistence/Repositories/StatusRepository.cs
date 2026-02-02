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
    public class StatusRepository: GenericRepository<Status>, IStatusRepository
    {
        public StatusRepository(ClefCraftDatabaseContext dbContext) : base(dbContext)
        {

        }
        public async Task<List<Status>> GetStatusesAsync()
        {
            return await _context.Statuses
                .Select(s => new Status
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .ToListAsync();
        }

        public async Task<List<Status>> GetStatusesByBoardIdAsync(int boardId)
        {
            return await _context.BoardStatuses
                .Where(bs => bs.BoardId == boardId || bs.BoardId == null)
                .Select(bs => new Status
                {
                    Id = bs.Status.Id,
                    Name = bs.Status.Name
                })
                .Distinct()
                .ToListAsync();
        }
    }
}
