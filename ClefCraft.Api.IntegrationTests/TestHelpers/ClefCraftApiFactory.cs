using ClefCraft.Application.Contracts.AI;
using ClefCraft.Identity.DbContext;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ClefCraft.Api.IntegrationTests.TestHelpers
{
    /// <summary>
    /// The real API pipeline (middleware order, rate limiting, CORS, controllers) over in-memory
    /// databases, with nothing that reaches outside the process. Rate-limiter state lives in the
    /// host, so any test that counts requests should create (and dispose) its own instance.
    /// </summary>
    public class ClefCraftApiFactory : WebApplicationFactory<Program>
    {
        public const string AllowedOrigin = "http://localhost:4200";

        private readonly string _databaseName = Guid.NewGuid().ToString();

        /// <summary>Per-factory attachment storage root, deleted when the factory is disposed.</summary>
        public string AttachmentRoot { get; } = Path.Combine(Path.GetTempPath(), "clefcraft-tests", Guid.NewGuid().ToString());

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Program.cs skips its startup migrations in this environment.
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Never used (the DbContexts are swapped for in-memory below), but startup
                    // validation requires the setting, as it does in every environment.
                    ["ConnectionStrings:ClefCraftDatabaseConnectionString"] = "Host=unused.invalid;Database=unused",
                    ["JwtSettings:Key"] = "integration-test-signing-key-needs-at-least-32-bytes",
                    ["AIService:BaseUrl"] = "http://ai.invalid",
                    ["AttachmentStorage:RootPath"] = AttachmentRoot
                }));

            builder.ConfigureServices(services =>
            {
                // Background services (e.g. NotificationBackgroundService) would poll the database.
                services.RemoveAll<IHostedService>();

                services.RemoveAll<IAIService>();
                services.AddSingleton<IAIService, StubAIService>();

                // AddDbContext keeps each configure callback (here UseNpgsql) as its own
                // registration, so it has to be removed too or both providers apply.
                services.RemoveAll<DbContextOptions<ClefCraftDatabaseContext>>();
                services.RemoveAll<DbContextOptions<ClefCraftIdentityDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ClefCraftDatabaseContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ClefCraftIdentityDbContext>>();
                services.AddDbContext<ClefCraftDatabaseContext>(o => o.UseInMemoryDatabase($"{_databaseName}-app"));
                services.AddDbContext<ClefCraftIdentityDbContext>(o => o.UseInMemoryDatabase($"{_databaseName}-identity"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && Directory.Exists(AttachmentRoot))
                Directory.Delete(AttachmentRoot, recursive: true);
        }

        private sealed class StubAIService : IAIService
        {
            public Task<double> PredictAttendanceAsync(AIEventDto ev) => Task.FromResult(0.5);

            public Task<List<double>> PredictBatchAsync(List<AIEventDto> ev) =>
                Task.FromResult(ev.Select(_ => 0.5).ToList());
        }
    }
}
