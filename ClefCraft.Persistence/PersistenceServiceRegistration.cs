using ClefCraft.Application.Contracts.Persistence;
using ClefCraft.Persistence.DatabaseContext;
using ClefCraft.Persistence.Repositories;
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
            //services.AddScoped<IBoardColumnRepository, BoardColumnRepository>();
            services.AddScoped<IBoardItemRepository, BoardItemRepository>();

            return services;
        }
    }
}
