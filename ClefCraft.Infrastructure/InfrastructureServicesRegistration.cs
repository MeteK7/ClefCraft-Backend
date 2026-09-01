using ClefCraft.Application.Contracts.Authorization;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Email;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Models.Email;
using ClefCraft.Infrastructure.EmailService;
using ClefCraft.Infrastructure.Logging;
using ClefCraft.Infrastructure.Services.Authorization;
using ClefCraft.Infrastructure.Services.Calendar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClefCraft.Infrastructure
{
    public static class InfrastructureServicesRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddScoped(typeof(IAppLogger<>), typeof(LoggerAdapter<>));
            services.AddScoped<IRecurringEventProjectionService, RecurringEventProjectionService>();
            services.AddScoped<IEventEnrichmentService, EventEnrichmentService>();
            services.AddScoped<IEventAnalyticsService, EventAnalyticsService>();
            services.AddScoped<IAttendancePredictionService, AttendancePredictionService>();
            services.AddScoped<IReminderSchedulerService, ReminderSchedulerService>();
            services.AddScoped<IBoardAccessService, BoardAccessService>();
            services.AddScoped<ICalendarAccessService, CalendarAccessService>();
            services.AddHostedService<NotificationBackgroundService>();

            return services;
        }
    }
}