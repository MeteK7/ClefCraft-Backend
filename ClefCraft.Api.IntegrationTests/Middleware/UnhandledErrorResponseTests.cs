using ClefCraft.Api.IntegrationTests.TestHelpers;
using ClefCraft.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.Middleware
{
    // End to end through the real pipeline, in the factory's non-Development "Testing" environment.
    public class UnhandledErrorResponseTests
    {
        [Fact]
        public async Task UnexpectedServerError_DoesNotSendTheExceptionMessageToTheClient()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("deleteduser");

            // A deleted account whose access token is still valid makes /me throw a
            // NullReferenceException inside UserService.GetUser (a known bug, tracked separately) —
            // a genuine unexpected exception from production code.
            using (var scope = factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByNameAsync("deleteduser");
                (await userManager.DeleteAsync(user!)).Succeeded.ShouldBeTrue();
            }

            using var response = await client.GetAsync("/api/Auth/me");

            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            var raw = await response.Content.ReadAsStringAsync();
            raw.ShouldNotContain("Object reference");
            raw.ShouldNotContain("NullReference");

            var body = JsonNode.Parse(raw)!;
            body["title"]!.GetValue<string>().ShouldBe("An unexpected error occurred.");
            body["detail"].ShouldBeNull();
            body["traceId"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
        }
    }
}
