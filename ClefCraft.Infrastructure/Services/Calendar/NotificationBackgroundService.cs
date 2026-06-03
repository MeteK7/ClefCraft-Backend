using ClefCraft.Application.Contracts.Calendar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Infrastructure.Services.Calendar
{
    public class NotificationBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationBackgroundService(
            IServiceScopeFactory scopeFactory)
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

                    var queueRepo =
                        scope.ServiceProvider.GetRequiredService<INotificationQueueRepository>();

                    var pending =
                        await queueRepo.GetPendingAsync(DateTimeOffset.UtcNow);

                    foreach (var item in pending)
                    {
                        Console.WriteLine($"REMINDER: {item.Message}");

                        item.IsProcessed = true;
                        item.ProcessedAt = DateTimeOffset.UtcNow;

                        await queueRepo.UpdateAsync(item);
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
