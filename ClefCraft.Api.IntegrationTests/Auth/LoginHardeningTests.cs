using ClefCraft.Api.IntegrationTests.TestHelpers;
using ClefCraft.Api.RateLimiting;
using ClefCraft.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.Auth
{
    // Each test builds its own factory: the rate limiter's counters live in the host, so a shared
    // host would let one test's requests turn another test's expected 401 into a 429.
    public class LoginHardeningTests
    {
        private static HttpRequestMessage LoginRequest(string email, string password, string? forwardedFor = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/login")
            {
                Content = JsonContent.Create(new { email, password })
            };
            request.Headers.Add("Origin", ClefCraftApiFactory.AllowedOrigin);
            if (forwardedFor != null)
                request.Headers.Add("X-Forwarded-For", forwardedFor);
            return request;
        }

        [Fact]
        public async Task Login_EleventhAttemptInAWindow_Returns429WithCorsHeaders()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = factory.CreateClient();

            for (var attempt = 1; attempt <= AuthRateLimiting.PermitLimit; attempt++)
            {
                using var allowed = await client.SendAsync(LoginRequest("nobody@test.com", "Wr0ng!Pass"));
                allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"attempt {attempt}");
            }

            using var limited = await client.SendAsync(LoginRequest("nobody@test.com", "Wr0ng!Pass"));

            limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
            // Without CORS headers the browser hides the 429 and the SPA can't tell the user to wait.
            limited.Headers.GetValues("Access-Control-Allow-Origin").ShouldContain(ClefCraftApiFactory.AllowedOrigin);
        }

        [Fact]
        public async Task Login_UnknownEmailAndWrongPassword_ReturnTheSameResponse()
        {
            using var factory = new ClefCraftApiFactory();
            using (var scope = factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var created = await userManager.CreateAsync(
                    new ApplicationUser { UserName = "known", Email = "known@test.com", FirstName = "Known", LastName = "User" },
                    "R1ght!Pass");
                created.Succeeded.ShouldBeTrue();
            }
            using var client = factory.CreateClient();

            using var unknownEmail = await client.SendAsync(LoginRequest("unknown@test.com", "R1ght!Pass"));
            using var wrongPassword = await client.SendAsync(LoginRequest("known@test.com", "Wr0ng!Pass"));

            unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            (await ComparableBody(unknownEmail)).ShouldBe(await ComparableBody(wrongPassword));
        }

        [Fact]
        public async Task Login_RateLimitIsPerForwardedClientIp_AndIgnoresClientSuppliedEntries()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = factory.CreateClient();

            for (var attempt = 1; attempt <= AuthRateLimiting.PermitLimit; attempt++)
            {
                using var allowed = await client.SendAsync(LoginRequest("nobody@test.com", "Wr0ng!Pass", "203.0.113.1"));
                allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"attempt {attempt}");
            }

            // A client prepending its own entry doesn't escape: only the proxy-appended (right-most) one counts.
            using var spoofed = await client.SendAsync(LoginRequest("nobody@test.com", "Wr0ng!Pass", "198.51.100.9, 203.0.113.1"));
            spoofed.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

            // A different client behind the same proxy has its own bucket.
            using var otherClient = await client.SendAsync(LoginRequest("nobody@test.com", "Wr0ng!Pass", "203.0.113.2"));
            otherClient.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // ProblemDetails carries a per-request traceId; everything else must match exactly.
        private static async Task<string> ComparableBody(HttpResponseMessage response)
        {
            var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
            body.Remove("traceId");
            return body.ToJsonString();
        }
    }
}
