using ClefCraft.Api.Models;
using ClefCraft.Application.Exceptions;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net;

namespace ClefCraft.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext, IHostEnvironment env)
        {
            try
            {
                await _next(httpContext);
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                // The client went away; there is nobody to send a response to and nothing went wrong.
                _logger.LogInformation("Request aborted by client: {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
            }
            catch (Exception ex)
            {
                if (httpContext.Response.HasStarted)
                {
                    // Status and headers are already sent, so an error body can't be written;
                    // trying would throw again and hide this exception.
                    _logger.LogError(ex, "Unhandled exception after the response started for {Method} {Path} (trace {TraceId})",
                        httpContext.Request.Method, httpContext.Request.Path, TraceIdOf(httpContext));
                    throw;
                }

                await HandleExceptionAsync(httpContext, ex, env);
            }
        }

        // Same id [ApiController] puts in its own problem responses, so one id ties a response to its log entry.
        private static string TraceIdOf(HttpContext httpContext) =>
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        private async Task HandleExceptionAsync(HttpContext httpContext, Exception ex, IHostEnvironment env)
        {
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            CustomProblemDetails problem = new();

            switch (ex)
            {
                case BadRequestException badRequestException:
                    statusCode = HttpStatusCode.BadRequest;
                    problem = new CustomProblemDetails
                    {
                        Title = badRequestException.Message,
                        Status = (int)statusCode,
                        Detail = badRequestException.InnerException?.Message,
                        Type = nameof(BadRequestException),
                        Errors = badRequestException.ValidationErrors
                    };
                    break;
                case NotFoundException NotFound:
                    statusCode = HttpStatusCode.NotFound;
                    problem = new CustomProblemDetails
                    {
                        Title = NotFound.Message,
                        Status = (int)statusCode,
                        Type = nameof(NotFoundException),
                        Detail = NotFound.InnerException?.Message,
                    };
                    break;
                case ForbiddenAccessException forbidden:
                    statusCode = HttpStatusCode.Forbidden;
                    problem = new CustomProblemDetails
                    {
                        Title = forbidden.Message,
                        Status = (int)statusCode,
                        Type = nameof(ForbiddenAccessException),
                        Detail = forbidden.InnerException?.Message,
                    };
                    break;
                case System.ComponentModel.DataAnnotations.ValidationException validation:
                    statusCode = HttpStatusCode.BadRequest;
                    problem = new CustomProblemDetails
                    {
                        Title = validation.Message,
                        Status = (int)statusCode,
                        Type = nameof(System.ComponentModel.DataAnnotations.ValidationException),
                        Detail = validation.InnerException?.Message,
                    };
                    break;
                case BadHttpRequestException badHttpRequest:
                    // Raised by the server itself for a malformed or oversized request (e.g. 413 when
                    // a body exceeds the endpoint's RequestSizeLimit) — a client error, not a crash.
                    statusCode = (HttpStatusCode)badHttpRequest.StatusCode;
                    problem = new CustomProblemDetails
                    {
                        Title = statusCode == HttpStatusCode.RequestEntityTooLarge
                            ? "The request is too large."
                            : "The request could not be read.",
                        Status = (int)statusCode,
                        Type = nameof(BadHttpRequestException),
                    };
                    break;
                default:
                    // An unexpected exception's message is internal (SQL, EF, null references...):
                    // it goes to the log, never to the client outside Development.
                    problem = new CustomProblemDetails
                    {
                        Title = "An unexpected error occurred.",
                        Status = (int)statusCode,
                        Type = nameof(HttpStatusCode.InternalServerError),
                        Detail = env.IsDevelopment() ? $"{ex.Message}{Environment.NewLine}{ex.StackTrace}" : null,
                    };
                    break;
            }

            var traceId = TraceIdOf(httpContext);
            problem.Extensions["traceId"] = traceId;

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path} (trace {TraceId})",
                    httpContext.Request.Method, httpContext.Request.Path, traceId);
            }
            else
            {
                _logger.LogWarning("{ExceptionType} ({StatusCode}) for {Method} {Path}: {Title} (trace {TraceId})",
                    ex.GetType().Name, (int)statusCode, httpContext.Request.Method, httpContext.Request.Path, problem.Title, traceId);
            }

            httpContext.Response.StatusCode = (int)statusCode;
            await httpContext.Response.WriteAsJsonAsync(problem);
        }
    }
}