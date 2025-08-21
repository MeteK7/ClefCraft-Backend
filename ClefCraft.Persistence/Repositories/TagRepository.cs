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
    public class TagRepository:GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(ClefCraftDatabaseContext dbContext) : base(dbContext)
        {
        }
        public async Task<List<Tag>> GetTags()
        {
            return await _context.Tags.Select(b => new Tag
            {
                Id = b.Id,
                Name = b.Name
            })
        .ToListAsync();
        }

        public async Task<List<Tag>> GetTagsByIdsAsync(List<int> tagIds)
        {
            return await _context.Tags
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync();
        }
    }
}
