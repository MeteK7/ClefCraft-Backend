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
    public class PriorityRepository:GenericRepository<Priority>, IPriorityRepository
    {
        public PriorityRepository(ClefCraftDatabaseContext dbContext) : base(dbContext)
        {

        }
        public async Task<List<Priority>> GetPrioritiesAsync()
        {
            return await _context.Priorities
                .Select(p => new Priority
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .ToListAsync();
        }
    }
}
