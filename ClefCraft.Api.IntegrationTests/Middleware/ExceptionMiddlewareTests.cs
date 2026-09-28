using ClefCraft.Api.Middleware;
using ClefCraft.Application.Exceptions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.Middleware
{
    public class ExceptionMiddlewareTests
    {
        private const string SensitiveMessage = "23505: duplicate key value violates unique constraint \"IX_BoardMembers_BoardId_UserId\"";

        private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

        private sealed class CapturingLogger : ILogger<ExceptionMiddleware>
        {
            public List<LogEntry> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }

        private sealed class FakeEnvironment : IHostEnvironment
        {
            public FakeEnvironment(string name) => EnvironmentName = name;
            public string EnvironmentName { get; set; }
            public string ApplicationName { get; set; } = "ClefCraft.Api";
            public string ContentRootPath { get; set; } = "";
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }

        private sealed class StartedResponseFeature : HttpResponseFeature
        {
            public override bool HasStarted => true;
        }

        private static DefaultHttpContext NewContext()
        {
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.Path = "/api/things";
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static async Task<(DefaultHttpContext Context, CapturingLogger Logger)> Run(
            Exception thrown, string environment = "Production", Action<DefaultHttpContext>? arrange = null)
        {
            var context = NewContext();
            arrange?.Invoke(context);
            var logger = new CapturingLogger();
            var middleware = new ExceptionMiddleware(_ => throw thrown, logger);

            await middleware.InvokeAsync(context, new FakeEnvironment(environment));
            return (context, logger);
        }

        private static string BodyOf(HttpContext context)
        {
            context.Response.Body.Position = 0;
            return new StreamReader(context.Response.Body).ReadToEnd();
        }

        [Fact]
        public async Task UnexpectedException_OutsideDevelopment_ReturnsGenericBody_AndLogsTheException()
        {
            var thrown = new InvalidOperationException(SensitiveMessage);

            var (context, logger) = await Run(thrown);

            context.Response.StatusCode.ShouldBe(500);
            var raw = BodyOf(context);
            raw.ShouldNotContain("IX_BoardMembers");
            raw.ShouldNotContain("duplicate key");

            var body = JsonNode.Parse(raw)!;
            body["title"]!.GetValue<string>().ShouldBe("An unexpected error occurred.");
            body["detail"].ShouldBeNull();
            var traceId = body["traceId"]!.GetValue<string>();
            traceId.ShouldNotBeNullOrWhiteSpace();

            var error = logger.Entries.Single(e => e.Level == LogLevel.Error);
            error.Exception.ShouldBeSameAs(thrown);
            error.Message.ShouldContain(traceId);
        }

        [Fact]
        public async Task UnexpectedException_InDevelopment_IncludesMessageAndStackTrace()
        {
            Exception thrown;
            try { throw new InvalidOperationException(SensitiveMessage); }
            catch (InvalidOperationException ex) { thrown = ex; }

            var (context, _) = await Run(thrown, environment: "Development");

            var detail = JsonNode.Parse(BodyOf(context))!["detail"]!.GetValue<string>();
            detail.ShouldContain(SensitiveMessage);
            thrown.StackTrace.ShouldNotBeNullOrWhiteSpace();
            detail.ShouldContain(thrown.StackTrace!);
        }

        [Fact]
        public async Task BadRequestException_KeepsItsTitleAndErrors_AndLogsAWarningWithoutTheException()
        {
            var validation = new ValidationResult(new[] { new ValidationFailure("Title", "Title is required.") });

            var (context, logger) = await Run(new BadRequestException("Invalid board item", validation));

            context.Response.StatusCode.ShouldBe(400);
            var body = JsonNode.Parse(BodyOf(context))!;
            body["title"]!.GetValue<string>().ShouldBe("Invalid board item");
            body["errors"]!["Title"]![0]!.GetValue<string>().ShouldBe("Title is required.");
            body["traceId"].ShouldNotBeNull();

            logger.Entries.ShouldNotContain(e => e.Level == LogLevel.Error);
            var warning = logger.Entries.Single(e => e.Level == LogLevel.Warning);
            warning.Exception.ShouldBeNull();
        }

        [Fact]
        public async Task NotFoundException_Returns404WithItsTitle()
        {
            var (context, _) = await Run(new NotFoundException("CalendarEvent", 5));

            context.Response.StatusCode.ShouldBe(404);
            JsonNode.Parse(BodyOf(context))!["title"]!.GetValue<string>().ShouldBe("CalendarEvent(5) was not found");
        }

        [Fact]
        public async Task ForbiddenAccessException_Returns403WithItsTitle()
        {
            var (context, _) = await Run(new ForbiddenAccessException());

            context.Response.StatusCode.ShouldBe(403);
            JsonNode.Parse(BodyOf(context))!["title"]!.GetValue<string>().ShouldBe("You do not have access to this resource.");
        }

        [Fact]
        public async Task ResponseAlreadyStarted_RethrowsAndWritesNoBody()
        {
            var context = NewContext();
            context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());
            context.Response.Body = new MemoryStream();
            var logger = new CapturingLogger();
            var thrown = new InvalidOperationException(SensitiveMessage);
            var middleware = new ExceptionMiddleware(_ => throw thrown, logger);

            var rethrown = await Should.ThrowAsync<InvalidOperationException>(() =>
                middleware.InvokeAsync(context, new FakeEnvironment("Production")));

            rethrown.ShouldBeSameAs(thrown);
            context.Response.Body.Length.ShouldBe(0);
            logger.Entries.Single(e => e.Level == LogLevel.Error).Exception.ShouldBeSameAs(thrown);
        }

        [Fact]
        public async Task ClientDisconnected_LogsInformationOnly_AndWritesNoBody()
        {
            using var aborted = new CancellationTokenSource();
            aborted.Cancel();

            var (context, logger) = await Run(
                new OperationCanceledException(aborted.Token),
                arrange: c => c.RequestAborted = aborted.Token);

            context.Response.Body.Length.ShouldBe(0);
            logger.Entries.ShouldNotContain(e => e.Level >= LogLevel.Warning);
            var info = logger.Entries.Single();
            info.Level.ShouldBe(LogLevel.Information);
            info.Exception.ShouldBeNull();
        }

        [Fact]
        public async Task OperationCanceled_WithoutClientDisconnect_IsAnOrdinary500()
        {
            // e.g. an internal timeout: the client is still waiting and deserves a response.
            var (context, logger) = await Run(new TaskCanceledException("A task was canceled."));

            context.Response.StatusCode.ShouldBe(500);
            JsonNode.Parse(BodyOf(context))!["title"]!.GetValue<string>().ShouldBe("An unexpected error occurred.");
            logger.Entries.ShouldContain(e => e.Level == LogLevel.Error);
        }
    }
}
