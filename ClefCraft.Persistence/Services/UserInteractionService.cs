using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Persistence.Services
{
    public class UserInteractionService : IUserInteractionService
    {
        private readonly ClefCraftDatabaseContext _context;
        private readonly IUserService _userService;

        public UserInteractionService(ClefCraftDatabaseContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task TrackAsync(string signalType, string entityType, int entityId, double value = 1)
        {
            await _context.UserInteractionSignals.AddAsync(new UserInteractionSignal
            {
                UserId = _userService.UserId,
                SignalType = signalType,
                EntityType = entityType,
                EntityId = entityId,
                Value = value,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
