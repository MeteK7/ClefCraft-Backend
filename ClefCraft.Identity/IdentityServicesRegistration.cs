using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Identity.DbContext;
using ClefCraft.Identity.Models;
using ClefCraft.Identity.Providers;
using ClefCraft.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClefCraft.Identity
{
    public static class IdentityServicesRegistration
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            var cs = configuration.GetConnectionString("ClefCraftDatabaseConnectionString");

            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            services.AddDbContext<ClefCraftIdentityDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("ClefCraftDatabaseConnectionString");

                options.UseNpgsql(connectionString);
            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ClefCraftIdentityDbContext>
                ().AddDefaultTokenProviders();

            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IUserService, UserService>();

            services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]))
                };

                o.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs/notifications"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}