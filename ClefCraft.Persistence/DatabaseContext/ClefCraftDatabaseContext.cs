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

            var entries = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            var pendingLogs = new List<(ActivityLog Log, BaseEntity? NewEntity)>();

            foreach (var entry in entries)
            {
                // 🔹 Maintain your existing audit fields
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DateCreated = utcNow;
                    entry.Entity.CreatedBy = userId;
                }

                if (entry.State == EntityState.Modified || entry.State == EntityState.Added)
                {
                    entry.Entity.DateModified = utcNow;
                    entry.Entity.ModifiedBy = userId;
                }

                // 🔹 Skip logging for ActivityLog itself (avoid infinite loop)
                if (entry.Entity is ActivityLog)
                    continue;

                // 🔹 Build Activity Log
                var entityType = entry.Entity.GetType().Name;

                string actionType = entry.State switch
                {
                    EntityState.Added => "CREATED",
                    EntityState.Modified => "UPDATED",
                    EntityState.Deleted => "DELETED",
                    _ => "UNKNOWN"
                };

                Dictionary<string, object>? changes = null;

                // Only track property changes for updates
                if (entry.State == EntityState.Modified)
                {
                    changes = new Dictionary<string, object>();

                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.IsModified)
                            continue;

                        var original = prop.OriginalValue;
                        var current = prop.CurrentValue;

                        if (Equals(original, current))
                            continue;

                        // Avoid noisy/system fields
                        if (prop.Metadata.Name is "DateModified" or "ModifiedBy")
                            continue;

                        changes[prop.Metadata.Name] = new
                        {
                            Old = original,
                            New = current
                        };
                    }

                    // If nothing meaningful changed → skip log
                    if (!changes.Any())
                        continue;
                }

                var log = new ActivityLog
                {
                    UserId = userId,
                    EntityType = entityType,
                    EntityId = entry.Entity.Id, // may be 0 for Added — fixed after save
                    ActionType = actionType,
                    MetadataJson = changes != null
                        ? System.Text.Json.JsonSerializer.Serialize(changes)
                        : null,
                    Timestamp = utcNow
                };

                pendingLogs.Add((log, entry.State == EntityState.Added ? entry.Entity : null));
            }

            // STEP 2: Save main entities FIRST
            var result = await base.SaveChangesAsync(cancellationToken);

            // STEP 3: Fix IDs for newly created entities
            foreach (var (log, newEntity) in pendingLogs)
            {
                if (newEntity != null && log.EntityId == 0)
                    log.EntityId = newEntity.Id; // EF has populated the real Id by now
            }

            // STEP 4: Persist the audit logs in a second pass
            if (pendingLogs.Any())
            {
                await ActivityLogs.AddRangeAsync(
                    pendingLogs.Select(p => p.Log),
                    cancellationToken);
                await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
    }
}