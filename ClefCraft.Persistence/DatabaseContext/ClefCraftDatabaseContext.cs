using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Domain;
using ClefCraft.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.DatabaseContext
{
    public class ClefCraftDatabaseContext : DbContext
    {
        private readonly IUserService _userService;

        public ClefCraftDatabaseContext(DbContextOptions<ClefCraftDatabaseContext> options, IUserService userService) : base(options)
        {
            _userService = userService;
        }

        //I expect to have my ClefCraftDatabaseContext know about a table(DbSet) modelled from LeaveType class.
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveAllocation> LeaveAllocations { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardColumn> BoardColumns { get; set; }
        public DbSet<BoardItem> BoardItems { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClefCraftDatabaseContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in base.ChangeTracker.Entries<BaseEntity>()
                .Where(q => q.State == EntityState.Added || q.State == EntityState.Modified))
            {
                entry.Entity.DateModified = DateTime.UtcNow; // Use UTC
                entry.Entity.ModifiedBy = _userService.UserId;

                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DateCreated = DateTime.UtcNow; // Use UTC
                    entry.Entity.CreatedBy = _userService.UserId;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

    }
}