using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationBackgroundService> _logger;

        // Only inject the scope factory to prevent captive dependencies
        public NotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var queueRepo = scope.ServiceProvider.GetRequiredService<INotificationQueueRepository>();
                    var notificationHubService = scope.ServiceProvider.GetRequiredService<INotificationHubService>();
                    // 1. Resolve the Unit of Work to commit changes safely
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var pending = await queueRepo.GetPendingAsync(DateTimeOffset.UtcNow);

                    if (pending != null && pending.Any())
                    {
                        foreach (var item in pending)
                        {
                            // Scoped per item so one failed send doesn't abort the loop before
                            // SaveChangesAsync runs - otherwise every item already marked
                            // processed earlier in this batch, including successfully sent
                            // ones, would never get persisted and would be resent next tick.
                            try
                            {
                                if (!string.IsNullOrEmpty(item.UserId))
                                {
                                    await notificationHubService.SendReminderToUserAsync(
                                        item.UserId,
                                        item.CalendarEventId,
                                        item.Message,
                                        stoppingToken);
                                }

                                item.IsProcessed = true;
                                item.ProcessedAt = DateTimeOffset.UtcNow;
                                await queueRepo.UpdateAsync(item);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                    ex,
                                    "Failed to send reminder {NotificationQueueId} to user {UserId} for calendar event {CalendarEventId}",
                                    item.Id,
                                    item.UserId,
                                    item.CalendarEventId);
                            }
                        }

                        // Now securely saved inside the repository context boundary!
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reminder background service tick failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}