using ClefCraft.Application.Contracts.Calendar;
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

                    // Resolve BOTH repositories and services safely inside the scope
                    var queueRepo = scope.ServiceProvider.GetRequiredService<INotificationQueueRepository>();
                    var notificationHubService = scope.ServiceProvider.GetRequiredService<INotificationHubService>();

                    var pending = await queueRepo.GetPendingAsync(DateTimeOffset.UtcNow);

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
                }
                catch (Exception ex)
                {
                    // Ensure you use Serilog or Console explicitly so you see errors if this drops
                    Console.WriteLine($"Reminder service error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}