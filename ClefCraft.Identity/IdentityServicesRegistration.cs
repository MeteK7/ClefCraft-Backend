using ClefCraft.Application.Contracts.Identity;
using ClefCraft.Application.Models.Identity;
using ClefCraft.Identity.DbContext;
using ClefCraft.Identity.Models;
using ClefCraft.Identity.Providers;
using ClefCraft.Identity.Seeding;
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
            // Validated at startup (Program.cs runs the startup validators before anything else),
            // so a bad config fails with a clear message rather than at the first sign-in.
            services.AddOptions<JwtSettings>()
                .Bind(configuration.GetSection("JwtSettings"))
                .Validate(s => Encoding.UTF8.GetByteCount(s.Key ?? string.Empty) >= 32,
                    "JwtSettings:Key must be at least 32 bytes (UTF-8).")
                .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer), "JwtSettings:Issuer is required.")
                .Validate(s => !string.IsNullOrWhiteSpace(s.Audience), "JwtSettings:Audience is required.")
                .ValidateOnStart();

            services.AddDbContext<ClefCraftIdentityDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("ClefCraftDatabaseConnectionString");

                options.UseNpgsql(connectionString);
            });

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // Enforced by AuthService.Login (CheckPasswordSignInAsync with lockoutOnFailure).
                    // Trade-off: anyone who knows an email can lock that account for 15 minutes.
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<ClefCraftIdentityDbContext>
                ().AddDefaultTokenProviders();

            services.AddScoped<DevelopmentUserSeeder>();

            services.AddTransient<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

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