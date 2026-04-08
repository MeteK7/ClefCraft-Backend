using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Services
{
    public class TaskLifecycleService : ITaskLifecycleService
    {
        private readonly ClefCraftDatabaseContext _context;

        public TaskLifecycleService(ClefCraftDatabaseContext context)
        {
            _context = context;
        }

        public async Task EnsureCreatedAsync(int boardItemId)
        {
            var exists = await _context.TaskLifecycles
                .AnyAsync(t => t.BoardItemId == boardItemId);

            if (!exists)
            {
                await _context.TaskLifecycles.AddAsync(new TaskLifecycle
                {
                    BoardItemId = boardItemId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RecordFirstWorkAsync(int boardItemId)
        {
            var lc = await Get(boardItemId);
            if (lc == null || lc.FirstWorkedAt.HasValue) return;

            lc.FirstWorkedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RecordCompletionAsync(int boardItemId)
        {
            var lc = await Get(boardItemId);
            if (lc == null) return;

            lc.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task RecordReopenAsync(int boardItemId)
        {
            var lc = await Get(boardItemId);
            if (lc == null) return;

            lc.ReopenCount++;
            lc.CompletedAt = null; // it's open again
            await _context.SaveChangesAsync();
        }

        public async Task RecordStatusChangeAsync(int boardItemId)
        {
            var lc = await Get(boardItemId);
            if (lc == null) return;

            lc.StatusChangeCount++;
            await _context.SaveChangesAsync();
        }

        public async Task RecordAssigneeChangeAsync(int boardItemId)
        {
            var lc = await Get(boardItemId);
            if (lc == null) return;

            lc.AssigneeChangeCount++;
            await _context.SaveChangesAsync();
        }

        private async Task<TaskLifecycle?> Get(int boardItemId) =>
            await _context.TaskLifecycles
                .FirstOrDefaultAsync(t => t.BoardItemId == boardItemId);
    }
}
