using System.Threading.RateLimiting;

namespace ClefCraft.Api.RateLimiting
{
    /// <summary>
    /// Per-client-IP limit for the credential endpoints (login, register), applied with
    /// [EnableRateLimiting(AuthRateLimiting.PolicyName)]. Account lockout (IdentityOptions.Lockout)
    /// stops guessing against one account; this slows spraying across many accounts from one client.
    /// The client IP is only meaningful behind a proxy once UseForwardedHeaders has run.
    /// </summary>
    public static class AuthRateLimiting
    {
        public const string PolicyName = "auth";

        public const int PermitLimit = 10;
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

        // RemoteIpAddress can be null (e.g. in-process test servers); share one bucket then rather than throw.
        private const string UnknownClientKey = "unknown-client";

        public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
        {
            return services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(PolicyName, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClientKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = PermitLimit,
                            Window = Window,
                            QueueLimit = 0
                        }));
            });
        }
    }
}
