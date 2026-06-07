using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Persistence;
using Microsoft.AspNet.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        // Only inject the scope factory to prevent captive dependencies
        public NotificationBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
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

                        // Now securely saved inside the repository context boundary!
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reminder service error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}