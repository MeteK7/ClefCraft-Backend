using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.Logging;
using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
using ClefCraft.Persistence.Services;
using ClefCraft.Persistence.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClefCraft.Persistence
{
    public static class PersistenceServiceRegistration
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ClefCraftDatabaseContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("ClefCraftDatabaseConnectionString"));
            });

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
            services.AddScoped<ILeaveAllocationRepository, LeaveAllocationRepository>();
            services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
            services.AddScoped<IBoardRepository, BoardRepository>();
            //services.AddScoped<IBoardColumnRepository, BoardColumnRepository>();
            services.AddScoped<IBoardItemRepository, BoardItemRepository>();
            services.AddScoped<ICalendarEventRepository, CalendarEventRepository>();
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

            return services;
        }
    }
}
