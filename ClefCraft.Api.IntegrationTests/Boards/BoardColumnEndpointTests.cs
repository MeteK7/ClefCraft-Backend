using ClefCraft.Api.IntegrationTests.TestHelpers;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.Boards
{
    public class BoardColumnEndpointTests
    {
        private static async Task<int> CreateBoardAsync(HttpClient client, string title)
        {
            using var response = await client.PostAsJsonAsync("/api/Boards", new { title });
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<int>();
        }

        private static async Task<JsonArray> ColumnsAsync(HttpClient client, int boardId)
        {
            using var response = await client.GetAsync($"/api/BoardItems/GetBoardItemsByBoardId/{boardId}");
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();
        }

        private static Task<HttpResponseMessage> CreateItemAsync(HttpClient client, int boardId, int columnId) =>
            client.PostAsJsonAsync("/api/BoardItems/Create", new
            {
                title = "Etude",
                description = "",
                boardId,
                boardColumnId = columnId,
                statusId = 1,
                priorityId = 1
            });

        [Fact]
        public async Task NewBoard_HasTheFiveDefaultColumns_InLaneOrder()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("boardmaker");

            var boardId = await CreateBoardAsync(client, "Scales");
            var columns = await ColumnsAsync(client, boardId);

            columns.Select(c => c!["title"]!.GetValue<string>())
                .ShouldBe(new[] { "Backlog", "To Do", "In Progress", "In Review", "Done" });
        }

        [Fact]
        public async Task CreatingAnItem_InAnotherBoardsColumn_Returns400()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("boardmaker");
            var boardA = await CreateBoardAsync(client, "A");
            var boardB = await CreateBoardAsync(client, "B");
            var columnOfA = (await ColumnsAsync(client, boardA))[0]!["id"]!.GetValue<int>();
            var columnOfB = (await ColumnsAsync(client, boardB))[0]!["id"]!.GetValue<int>();

            using var rejected = await CreateItemAsync(client, boardB, columnOfA);
            using var accepted = await CreateItemAsync(client, boardB, columnOfB);

            rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            JsonNode.Parse(await rejected.Content.ReadAsStringAsync())!["title"]!.GetValue<string>()
                .ShouldBe("The column does not belong to this board.");
            accepted.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}
