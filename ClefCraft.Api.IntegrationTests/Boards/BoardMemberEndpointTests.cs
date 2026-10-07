using ClefCraft.Api.IntegrationTests.TestHelpers;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.Boards
{
    // Adding a member goes through the real HTTP pipeline here (model binding and validation
    // included); the handler tests build the command directly and can't see binding problems.
    public class BoardMemberEndpointTests
    {
        private static async Task<int> CreateBoardAsync(HttpClient client, string title)
        {
            using var response = await client.PostAsJsonAsync("/api/Boards", new { title });
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<int>();
        }

        private static async Task<string> UserIdAsync(HttpClient client)
        {
            using var response = await client.GetAsync("/api/Auth/me");
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<string>();
        }

        private static async Task<List<string>> MemberIdsAsync(HttpClient client, int boardId)
        {
            using var response = await client.GetAsync($"/api/Boards/{boardId}/Members");
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray()
                .Select(m => m!["userId"]!.GetValue<string>())
                .ToList();
        }

        [Fact]
        public async Task Owner_AddsAnotherUser_WithJustTheirUserId()
        {
            using var factory = new ClefCraftApiFactory();
            using var owner = await factory.CreateSignedInClientAsync("boardowner");
            using var guest = await factory.CreateSignedInClientAsync("boardguest");
            var guestId = await UserIdAsync(guest);
            var boardId = await CreateBoardAsync(owner, "Duets");

            // The body the frontend and any client send: only the user being added.
            using var response = await owner.PostAsJsonAsync($"/api/Boards/{boardId}/Members", new { userId = guestId });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            JsonNode.Parse(await response.Content.ReadAsStringAsync())!["userId"]!.GetValue<string>().ShouldBe(guestId);
            (await MemberIdsAsync(owner, boardId)).ShouldContain(guestId);
        }

        [Fact]
        public async Task NonOwner_CannotAddMembers_EvenWhenNamingTheOwnerAsRequester()
        {
            using var factory = new ClefCraftApiFactory();
            using var owner = await factory.CreateSignedInClientAsync("boardowner");
            using var intruder = await factory.CreateSignedInClientAsync("intruder");
            var ownerId = await UserIdAsync(owner);
            var intruderId = await UserIdAsync(intruder);
            var boardId = await CreateBoardAsync(owner, "Private");

            // The caller always comes from the token: a requestingUserId in the body is ignored.
            using var response = await intruder.PostAsJsonAsync(
                $"/api/Boards/{boardId}/Members",
                new { userId = intruderId, requestingUserId = ownerId });

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await MemberIdsAsync(owner, boardId)).ShouldNotContain(intruderId);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"userId\":\"\"}")]
        public async Task AddingAMember_WithoutAUserId_Returns400(string body)
        {
            using var factory = new ClefCraftApiFactory();
            using var owner = await factory.CreateSignedInClientAsync("boardowner");
            var boardId = await CreateBoardAsync(owner, "Solo");

            using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            using var response = await owner.PostAsync($"/api/Boards/{boardId}/Members", content);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await MemberIdsAsync(owner, boardId)).Count.ShouldBe(1); // just the owner
        }
    }
}
