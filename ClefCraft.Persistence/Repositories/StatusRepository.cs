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
    }
}
