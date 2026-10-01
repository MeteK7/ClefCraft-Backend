using ClefCraft.Api.Hubs;
using ClefCraft.Api.Middleware;
using ClefCraft.Api.RateLimiting;
using ClefCraft.Api.Services;
using ClefCraft.Application;
using ClefCraft.Application.Contracts.AI;
using ClefCraft.Application.Contracts.Calendar;
using ClefCraft.Application.Contracts.FileAttachment;
using ClefCraft.Identity;
using ClefCraft.Identity.DbContext;
using ClefCraft.Identity.Seeding;
using ClefCraft.Infrastructure;
using ClefCraft.Infrastructure.FileAttachmentService;
using ClefCraft.Infrastructure.Services.AI;
using ClefCraft.Infrastructure.Services.Calendar;
using ClefCraft.Persistence;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.WriteTo.Console().ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddPersistenceServices(
    builder.Configuration,
    builder.Environment);
builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient",
        builder => builder
        .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.AddAuthRateLimiting();

var aiBaseUrl = builder.Configuration["AIService:BaseUrl"];

builder.Services.AddHttpClient<IAIService, AIService>(client =>
{
    client.BaseAddress = new Uri(aiBaseUrl!);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileAttachmentService, FileAttachmentService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificationHubService, NotificationHubService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API",
        Version = "v2",
        Description = "Your Api Description"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
});
var app = builder.Build();

app.Logger.LogInformation("ENVIRONMENT: {Env}", app.Environment.EnvironmentName);
app.Logger.LogInformation("IS DEVELOPMENT: {IsDev}", app.Environment.IsDevelopment());

// AUTO APPLY MIGRATIONS ON STARTUP
// Skipped under "Testing": ClefCraft.Api.IntegrationTests swaps in the EF in-memory provider, which has no migrations.
if (!app.Environment.IsEnvironment("Testing"))
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

    try
    {
        // -------------------------
        // Persistence DB
        // -------------------------
        var db = scope.ServiceProvider.GetRequiredService<ClefCraftDatabaseContext>();

        var pendingPersistence = db.Database.GetPendingMigrations();

        if (pendingPersistence.Any())
        {
            logger.LogInformation("Applying {Count} persistence migrations...", pendingPersistence.Count());

            db.Database.Migrate();

            logger.LogInformation("Persistence migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("No pending persistence migrations.");
        }

        // -------------------------
        // Identity DB
        // -------------------------
        var identityDb = scope.ServiceProvider.GetRequiredService<ClefCraftIdentityDbContext>();

        var pendingIdentity = identityDb.Database.GetPendingMigrations();

        if (pendingIdentity.Any())
        {
            logger.LogInformation("Applying {Count} identity migrations...",
                pendingIdentity.Count());

            identityDb.Database.Migrate();

            logger.LogInformation("Identity migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("No pending identity migrations.");
        }

        if (app.Environment.IsDevelopment())
        {
            await scope.ServiceProvider.GetRequiredService<DevelopmentUserSeeder>().SeedAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw; // fail fast on startup (important for production consistency)
    }
}

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

app.UseCors("AllowAngularClient");

// After UseCors so a 429 still carries the CORS headers and the browser can read its status.
app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// Lets ClefCraft.Api.IntegrationTests reference Program in WebApplicationFactory<Program>.
public partial class Program { }
