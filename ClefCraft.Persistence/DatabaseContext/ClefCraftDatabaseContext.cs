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

        public ClefCraftDatabaseContext(
            DbContextOptions<ClefCraftDatabaseContext> options,
            IUserService userService) : base(options)
        {
            _userService = userService;
        }

        // =========================
        // DbSets
        // =========================

        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveAllocation> LeaveAllocations { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardColumn> BoardColumns { get; set; }
        public DbSet<BoardItem> BoardItems { get; set; }

        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<CalendarEventException> CalendarEventExceptions { get; set; }
        public DbSet<CalendarEventExceptionHistory> CalendarEventExceptionHistories { get; set; }
        public DbSet<CalendarEventAttachment> CalendarEventAttachments { get; set; }

        public DbSet<EventType> EventTypes { get; set; }

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

        // =========================
        // Model Configuration
        // =========================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ClefCraftDatabaseContext).Assembly);

            modelBuilder.Entity<CalendarEventException>()
                .HasIndex(x => new { x.SeriesUid, x.OccurrenceDate })
                .IsUnique();

            modelBuilder.Entity<CalendarEvent>()
                .HasIndex(x => x.SeriesUid);

            base.OnModelCreating(modelBuilder);
        }

        // =========================
        // SaveChanges (Audit System)
        // =========================

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var userId = _userService.UserId;

            var entries = ChangeTracker.Entries<BaseEntity>()
                .Where(e =>
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified ||
                    e.State == EntityState.Deleted)
                .ToList();

            var pendingLogs = new List<ActivityLog>();

            foreach (var entry in entries)
            {
                // Skip audit logs themselves
                if (entry.Entity is ActivityLog)
                    continue;

                // Audit fields
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DateCreated = utcNow;
                    entry.Entity.CreatedBy = userId;
                }

                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Entity.DateModified = utcNow;
                    entry.Entity.ModifiedBy = userId;
                }

                var entityType = entry.Entity.GetType().Name;

                var actionType = entry.State switch
                {
                    EntityState.Added => "CREATED",
                    EntityState.Modified => "UPDATED",
                    EntityState.Deleted => "DELETED",
                    _ => "UNKNOWN"
                };

                Dictionary<string, object>? changes = null;

                if (entry.State == EntityState.Modified)
                {
                    changes = new Dictionary<string, object>();

                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.IsModified)
                            continue;

                        if (Equals(prop.OriginalValue, prop.CurrentValue))
                            continue;

                        if (prop.Metadata.Name is "DateModified" or "ModifiedBy")
                            continue;

                        changes[prop.Metadata.Name] = new
                        {
                            Old = prop.OriginalValue,
                            New = prop.CurrentValue
                        };
                    }

                    if (changes.Count == 0)
                        continue;
                }

                pendingLogs.Add(new ActivityLog
                {
                    UserId = userId,
                    EntityType = entityType,
                    EntityId = entry.State == EntityState.Added ? 0 : entry.Entity.Id,
                    ActionType = actionType,
                    MetadataJson = changes != null
                        ? System.Text.Json.JsonSerializer.Serialize(changes)
                        : null,
                    Timestamp = utcNow
                });
            }

            // STEP 1: Save main entities
            var result = await base.SaveChangesAsync(cancellationToken);

            // STEP 2: Fix IDs for added entities
            foreach (var log in pendingLogs)
            {
                if (log.EntityId == 0 && log.ActionType == "CREATED")
                {
                    var tracked = entries.FirstOrDefault(e =>
                        e.State == EntityState.Added &&
                        e.Entity.GetType().Name == log.EntityType);

                    if (tracked != null)
                        log.EntityId = tracked.Entity.Id;
                }
            }

            // STEP 3: Save logs
            if (pendingLogs.Any())
            {
                await ActivityLogs.AddRangeAsync(pendingLogs, cancellationToken);
                await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
    }
}