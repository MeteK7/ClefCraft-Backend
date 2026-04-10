using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Services
{
    public class ActivityLogger : IActivityLogger
    {
        private readonly ClefCraftDatabaseContext _context;
        private readonly IUserService _userService;

        public ActivityLogger(
            ClefCraftDatabaseContext context,
            IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task LogAsync(
            string entityType,
            int entityId,
            string actionType,
            object? metadata = null)
        {
            var log = new ActivityLog
            {
                UserId = _userService.UserId,
                EntityType = entityType,
                EntityId = entityId,
                ActionType = actionType,
                MetadataJson = metadata != null
                    ? System.Text.Json.JsonSerializer.Serialize(metadata)
                    : null,
                Timestamp = DateTime.UtcNow
            };

            await _context.ActivityLogs.AddAsync(log);
        }
    }
}
