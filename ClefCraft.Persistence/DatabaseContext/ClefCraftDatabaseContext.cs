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
        public DbSet<CalendarEventException> CalendarEventExceptions { get; set; }
        public DbSet<CalendarEventExceptionHistory> CalendarEventExceptionHistories { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<CalendarEventAttachment> CalendarEventAttachments { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Priority> Priorities { get; set; }
        public DbSet<BoardTag> BoardTags { get; set; }
        public DbSet<BoardStatus> BoardStatuses { get; set; }
        public DbSet<BoardPriority> BoardPriorities { get; set; }
        public DbSet<BoardItemTag> BoardItemTags { get; set; }
        public DbSet<BoardItemStatus> BoardItemStatuses { get; set; }
        public DbSet<BoardItemPriority> BoardItemPriorities { get; set; }
        public DbSet<BoardColumnMapping> BoardColumnMappings { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<EntitySnapshot> EntitySnapshots { get; set; }
        public DbSet<UserInteractionSignal> UserInteractionSignals { get; set; }
        public DbSet<TaskLifecycle> TaskLifecycles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClefCraftDatabaseContext).Assembly);

            modelBuilder.Entity<CalendarEventException>()
                .HasIndex(x => new { x.CalendarEventId, x.OccurrenceDate })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var userId = _userService.UserId;

            // ✅ KEEP audit logic
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DateCreated = utcNow;
                    entry.Entity.CreatedBy = userId;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.DateModified = utcNow;
                    entry.Entity.ModifiedBy = userId;
                }
            }

            // ✅ Collect domain events BEFORE save
            var domainEvents = ChangeTracker
                .Entries<BaseEntity>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            var result = await base.SaveChangesAsync(cancellationToken);

            // ✅ Dispatch AFTER commit (important)
            foreach (var domainEvent in domainEvents)
            {
                // TODO: inject IMediator and publish
                // await _mediator.Publish(domainEvent);
            }

            // ✅ Clear events
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                entry.Entity.ClearDomainEvents();
            }

            return result;
        }
    }
}