using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Infrastructure.FileAttachmentService;
using ClefCraft.Infrastructure.Logging;
using ClefCraft.Infrastructure.Services.Authorization;
using ClefCraft.Infrastructure.Services.Calendar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClefCraft.Infrastructure
{
    public static class InfrastructureServicesRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            services.AddScoped<IRecurringEventProjectionService, RecurringEventProjectionService>();
            services.AddScoped<IEventEnrichmentService, EventEnrichmentService>();
            services.AddScoped<IEventAnalyticsService, EventAnalyticsService>();
            services.AddScoped<IAttendancePredictionService, AttendancePredictionService>();
            services.AddScoped<IReminderSchedulerService, ReminderSchedulerService>();
            services.AddScoped<IBoardAccessService, BoardAccessService>();
            services.AddScoped<ICalendarAccessService, CalendarAccessService>();
            services.AddHostedService<NotificationBackgroundService>();

            // A relative RootPath (or none) is anchored at the content root, so the default works
            // unchanged on a dev machine and in the container; set AttachmentStorage:RootPath to override.
            services.AddOptions<AttachmentStorageOptions>()
                .Bind(configuration.GetSection(AttachmentStorageOptions.SectionName))
                .PostConfigure<IHostEnvironment>((options, env) =>
                    options.RootPath = Path.Combine(
                        env.ContentRootPath,
                        string.IsNullOrWhiteSpace(options.RootPath)
                            ? Path.Combine("App_Data", "calendar-attachments")
                            : options.RootPath));

            return services;
        }
    }
}