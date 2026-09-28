using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.TestHelpers
{
    public static class AuthenticatedClient
    {
        /// <summary>
        /// Registers a fresh user through the real /api/Auth endpoints and returns a client that sends
        /// their access token. Uses two of the factory's auth rate-limit permits (register + login).
        /// </summary>
        public static async Task<HttpClient> CreateSignedInClientAsync(this ClefCraftApiFactory factory, string userName)
        {
            var client = factory.CreateClient();
            var email = $"{userName}@test.example";
            const string password = "Integr4tion!Pass";

            using var register = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                firstName = userName,
                lastName = "Tester",
                email,
                userName,
                password
            });
            register.EnsureSuccessStatusCode();

            using var login = await client.PostAsJsonAsync("/api/Auth/login", new { email, password });
            login.EnsureSuccessStatusCode();

            var token = JsonNode.Parse(await login.Content.ReadAsStringAsync())!["token"]!.GetValue<string>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}
