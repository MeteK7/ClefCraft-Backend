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
    public class RecurrenceSeriesRepository
        : GenericRepository<RecurrenceSeries>,
          IRecurrenceSeriesRepository
    {
        public RecurrenceSeriesRepository(
            ClefCraftDatabaseContext context)
            : base(context)
        {
        }

        public async Task<RecurrenceSeries?> GetBySeriesUidAsync(
            string seriesUid)
        {
            return await _context.Set<RecurrenceSeries>()
                .Include(x => x.Segments)
                .FirstOrDefaultAsync(x =>
                    x.SeriesUid == seriesUid);
        }

        public async Task<List<RecurrenceSeries>> GetByUserIdAsync(
            string userId)
        {
            return await _context.Set<RecurrenceSeries>()
                .Include(x => x.Segments)
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }
    }
}
