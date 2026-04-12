using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Models.Analytics;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;

namespace ClefCraft.Persistence.Services
{
    public class UserInteractionService : IUserInteractionService
    {
        private readonly ClefCraftDatabaseContext _context;
        private readonly IUserService _userService;

        public UserInteractionService(
            ClefCraftDatabaseContext context,
            IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task TrackAsync(
            string signalType,
            string entityType,
            int entityId,
            double value = 1)
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

            await _context.SaveChangesAsync();
        }

        public async Task TrackBatchAsync(IEnumerable<Interaction> interactions)
        {
            var utcNow = DateTime.UtcNow;
            var userId = _userService.UserId;

            var signals = interactions.Select(i => new UserInteractionSignal
            {
                UserId = userId,
                SignalType = i.SignalType,
                EntityType = i.EntityType,
                EntityId = i.EntityId,
                Value = i.Value,
                Timestamp = utcNow
            });

            await _context.UserInteractionSignals.AddRangeAsync(signals);
            await _context.SaveChangesAsync();
        }
    }
}