using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
using ClefCraft.Persistence.Services;
using ClefCraft.Persistence.UnitOfWork;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace ClefCraft.Persistence
{
    public static class PersistenceServiceRegistration
    {
        //public static IServiceCollection AddPersistenceServices(
        //    this IServiceCollection services,
        //    IConfiguration configuration,
        //    IWebHostEnvironment environment)
        //{
        //    var connectionString =
        //        configuration.GetConnectionString("ClefCraftDatabaseConnectionString");

        //    services.AddDbContext<ClefCraftDatabaseContext>(options =>
        //    {
        //        var connectionString = configuration.GetConnectionString("ClefCraftDatabaseConnectionString");

        //        if (connectionString != null &&
        //            (connectionString.StartsWith("postgres") || connectionString.Contains("Host=")))
        //        {
        //            options.UseNpgsql(connectionString);
        //        }
        //        else
        //        {
        //            options.UseSqlServer(connectionString);
        //        }
        //    });

        //    // repositories stay unchanged
        //    services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        //    services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        //    services.AddScoped<ILeaveAllocationRepository, LeaveAllocationRepository>();
        //    services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        //    services.AddScoped<IBoardRepository, BoardRepository>();
        //    services.AddScoped<IBoardItemRepository, BoardItemRepository>();
        //    services.AddScoped<IRecurrenceSeriesRepository, RecurrenceSeriesRepository>();
        //    services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
        //    services.AddScoped<ICalendarEventSegmentRepository, CalendarEventSegmentRepository>();
        //    services.AddScoped<ICalendarEventExceptionRepository, CalendarEventExceptionRepository>();
        //    services.AddScoped<ICalendarEventAttachmentRepository, CalendarEventAttachmentRepository>();
        //    services.AddScoped<ITagRepository, TagRepository>();
        //    services.AddScoped<IStatusRepository, StatusRepository>();
        //    services.AddScoped<IPriorityRepository, PriorityRepository>();
        //    services.AddScoped<IEventTypeRepository, EventTypeRepository>();
        //    services.AddScoped<IAIDataRepository, AIDataRepository>();
        //    services.AddScoped<IActivityLogger, ActivityLogger>();
        //    services.AddScoped<IUserInteractionService, UserInteractionService>();
        //    services.AddScoped<ITaskLifecycleService, TaskLifecycleService>();
        //    services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        //    services.AddScoped<INotificationQueueRepository, NotificationQueueRepository>();
        //    services.AddScoped<ICalendarReminderRepository, CalendarReminderRepository>();

        //    return services;
        //}

        public static IServiceCollection AddPersistenceServices(
    this IServiceCollection services,
    IConfiguration configuration,
    IWebHostEnvironment environment)
        {
            var cs = configuration.GetConnectionString("ClefCraftDatabaseConnectionString");
            Console.WriteLine($"[Persistence DB] CONNECTION STRING EXISTS: {!string.IsNullOrEmpty(cs)}");
            Console.WriteLine($"[Persistence DB] VALUE: {cs}");

            services.AddDbContext<ClefCraftDatabaseContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("ClefCraftDatabaseConnectionString");

                if (connectionString != null &&
                    (connectionString.StartsWith("postgres") || connectionString.Contains("Host=")))
                {
                    options.UseNpgsql(connectionString);
                }
                else
                {
                    options.UseSqlServer(connectionString);
                }
            });


            // repositories stay unchanged
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
            services.AddScoped<ILeaveAllocationRepository, LeaveAllocationRepository>();
            services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
            services.AddScoped<IBoardRepository, BoardRepository>();
            services.AddScoped<IBoardItemRepository, BoardItemRepository>();
            services.AddScoped<IRecurrenceSeriesRepository, RecurrenceSeriesRepository>();
            services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
            services.AddScoped<ICalendarEventSegmentRepository, CalendarEventSegmentRepository>();
            services.AddScoped<ICalendarEventExceptionRepository, CalendarEventExceptionRepository>();
            services.AddScoped<ICalendarEventAttachmentRepository, CalendarEventAttachmentRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IStatusRepository, StatusRepository>();
            services.AddScoped<IPriorityRepository, PriorityRepository>();
            services.AddScoped<IEventTypeRepository, EventTypeRepository>();
            services.AddScoped<IAIDataRepository, AIDataRepository>();
            services.AddScoped<IActivityLogger, ActivityLogger>();
            services.AddScoped<IUserInteractionService, UserInteractionService>();
            services.AddScoped<ITaskLifecycleService, TaskLifecycleService>();
            services.AddScoped<IUnitOfWork, EfUnitOfWork>();

            services.AddScoped<INotificationQueueRepository, NotificationQueueRepository>();
            services.AddScoped<ICalendarReminderRepository, CalendarReminderRepository>();

            return services;
        }
    }
}
