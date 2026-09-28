using ClefCraft.Api.IntegrationTests.TestHelpers;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.Users
{
    // GET /api/Users used to hand every signed-in user the id, name and email of every account.
    // Assignee pickers now use the board's member list, which is members-only and has no emails.
    public class UserDirectoryExposureTests
    {
        [Fact]
        public async Task UserDirectory_IsGone_EvenForASignedInUser()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("aliceuser");

            using var response = await client.GetAsync("/api/Users");

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task BoardMemberList_ContainsNamesButNoEmails()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("bobbyuser");

            using var created = await client.PostAsJsonAsync("/api/Boards", new { title = "Etudes" });
            created.EnsureSuccessStatusCode();
            var boardId = JsonNode.Parse(await created.Content.ReadAsStringAsync())!["id"]!.GetValue<int>();

            using var response = await client.GetAsync($"/api/Boards/{boardId}/Members");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            var member = JsonNode.Parse(body)!.AsArray().Single()!.AsObject();
            member["fullName"]!.GetValue<string>().ShouldBe("bobbyuser Tester");
            member.ContainsKey("email").ShouldBeFalse();
            body.ShouldNotContain("@test.example");
        }
    }
}
