using ClefCraft.Api.IntegrationTests.TestHelpers;
using ClefCraft.Domain;
using ClefCraft.Persistence.DatabaseContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ClefCraft.Api.IntegrationTests.Boards
{
    // The asks endpoints through the real pipeline: routing, model binding, the handlers and the
    // exception middleware. Enums are serialized as numbers (State: 0 Pending, 1 Accepted,
    // 2 Declined, 3 Completed, 4 Cancelled).
    public class BoardItemAskEndpointTests
    {
        private const int Pending = 0;
        private const int Accepted = 1;
        private const int Cancelled = 4;

        /// <summary>A board owned by "owner" with "helper" and "second" as members, and one item in progress.</summary>
        private sealed class AskBoard : IDisposable
        {
            public HttpClient Owner { get; init; } = null!;
            public HttpClient Helper { get; init; } = null!;
            public HttpClient Second { get; init; } = null!;
            public string OwnerId { get; init; } = null!;
            public string HelperId { get; init; } = null!;
            public string SecondId { get; init; } = null!;
            public int BoardId { get; init; }
            public int ItemId { get; init; }
            public int ColumnId { get; init; }
            public int DoneStatusId { get; init; }

            public void Dispose()
            {
                Owner.Dispose();
                Helper.Dispose();
                Second.Dispose();
            }
        }

        private static async Task<string> UserIdAsync(HttpClient client)
        {
            using var response = await client.GetAsync("/api/Auth/me");
            response.EnsureSuccessStatusCode();
            return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<string>();
        }

        private static async Task<JsonNode> ReadAsync(HttpResponseMessage response) =>
            JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

        private static async Task<AskBoard> CreateAskBoardAsync(ClefCraftApiFactory factory)
        {
            // The in-memory database has no reference data; the item needs a status, and "Done" must exist.
            int inProgressId, doneId;
            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ClefCraftDatabaseContext>();
                var inProgress = new Status { Name = "In Progress" };
                var done = new Status { Name = "Done" };
                context.Statuses.AddRange(inProgress, done);
                await context.SaveChangesAsync();
                (inProgressId, doneId) = (inProgress.Id, done.Id);
            }

            var owner = await factory.CreateSignedInClientAsync("askowner");
            var helper = await factory.CreateSignedInClientAsync("askhelper");
            var second = await factory.CreateSignedInClientAsync("asksecond");
            var helperId = await UserIdAsync(helper);
            var secondId = await UserIdAsync(second);

            using var board = await owner.PostAsJsonAsync("/api/Boards", new { title = "Release" });
            var boardId = (await ReadAsync(board))["id"]!.GetValue<int>();
            (await owner.PostAsJsonAsync($"/api/Boards/{boardId}/Members", new { userId = helperId })).EnsureSuccessStatusCode();
            (await owner.PostAsJsonAsync($"/api/Boards/{boardId}/Members", new { userId = secondId })).EnsureSuccessStatusCode();

            using var columns = await owner.GetAsync($"/api/BoardItems/GetBoardItemsByBoardId/{boardId}");
            var columnId = (await ReadAsync(columns)).AsArray()[0]!["id"]!.GetValue<int>();

            using var item = await owner.PostAsJsonAsync("/api/BoardItems/Create", new
            {
                title = "Release notes",
                description = "",
                boardId,
                boardColumnId = columnId,
                statusId = inProgressId,
                priorityId = 1
            });
            item.EnsureSuccessStatusCode();

            return new AskBoard
            {
                Owner = owner,
                Helper = helper,
                Second = second,
                OwnerId = await UserIdAsync(owner),
                HelperId = helperId,
                SecondId = secondId,
                BoardId = boardId,
                ItemId = (await ReadAsync(item))["id"]!.GetValue<int>(),
                ColumnId = columnId,
                DoneStatusId = doneId
            };
        }

        private static Task<HttpResponseMessage> AskAsync(HttpClient client, int itemId, string recipientUserId, bool isRequired = false) =>
            client.PostAsJsonAsync($"/api/BoardItems/{itemId}/asks", new { recipientUserId, note = "Review the migration", isRequired });

        private static async Task<JsonNode> AskOkAsync(HttpClient client, int itemId, string recipientUserId, bool isRequired = false)
        {
            using var response = await AskAsync(client, itemId, recipientUserId, isRequired);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            return await ReadAsync(response);
        }

        private static Task<HttpResponseMessage> ActAsync(HttpClient client, JsonNode ask, string action, int? revision = null) =>
            client.PostAsJsonAsync($"/api/BoardItemAsks/{ask["id"]!.GetValue<int>()}/{action}",
                new { expectedRevision = revision ?? ask["revision"]!.GetValue<int>() });

        private static async Task<JsonArray> AsksOfItemAsync(HttpClient client, int itemId)
        {
            using var response = await client.GetAsync($"/api/BoardItems/{itemId}/asks");
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            return (await ReadAsync(response)).AsArray();
        }

        private static object Snapshot(JsonArray asks) =>
            asks.Where(a => a!["isOpen"]!.GetValue<bool>() || a["isUnmetRequirement"]!.GetValue<bool>())
                .Select(a => new { askId = a!["id"]!.GetValue<int>(), revision = a["revision"]!.GetValue<int>() })
                .ToList();

        private static Task<HttpResponseMessage> CompleteItemAsync(HttpClient client, AskBoard board, object? askClosure, string route = "put") =>
            route == "put"
                ? client.PutAsJsonAsync($"/api/BoardItems/{board.ItemId}", new { id = board.ItemId, boardColumnId = board.ColumnId, statusId = board.DoneStatusId, askClosure })
                : client.PostAsJsonAsync("/api/BoardItems/SwitchColumn", new { id = board.ItemId, boardColumnId = board.ColumnId, statusId = board.DoneStatusId, askClosure });

        private static async Task<string> TitleOfAsync(HttpResponseMessage response) =>
            (await ReadAsync(response))["title"]!.GetValue<string>();

        // ---- Success codes and the basic loop ------------------------------------------------------

        [Fact]
        public async Task AskAcceptAndMine_Return200_WithTheAsk()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);

            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);
            ask["state"]!.GetValue<int>().ShouldBe(Pending);
            ask["requestedByUserId"]!.GetValue<string>().ShouldBe(board.OwnerId);

            using (var received = await board.Helper.GetAsync("/api/BoardItemAsks/mine?direction=received"))
            {
                received.StatusCode.ShouldBe(HttpStatusCode.OK);
                (await ReadAsync(received)).AsArray().Single()!["id"]!.GetValue<int>().ShouldBe(ask["id"]!.GetValue<int>());
            }

            using var accept = await ActAsync(board.Helper, ask, "accept");
            accept.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadAsync(accept))["state"]!.GetValue<int>().ShouldBe(Accepted);

            using var sent = await board.Owner.GetAsync("/api/BoardItemAsks/mine?direction=Sent");
            sent.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadAsync(sent)).AsArray().Single()!["state"]!.GetValue<int>().ShouldBe(Accepted);
        }

        [Fact]
        public async Task Mine_IgnoresAUserIdInTheQuery()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            await AskOkAsync(board.Owner, board.ItemId, board.HelperId);

            using var response = await board.Second.GetAsync($"/api/BoardItemAsks/mine?direction=received&userId={board.HelperId}");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadAsync(response)).AsArray().ShouldBeEmpty();
        }

        // ---- Error codes and shapes ---------------------------------------------------------------

        [Fact]
        public async Task WithoutAToken_Returns401()
        {
            using var factory = new ClefCraftApiFactory();
            using var anonymous = factory.CreateClient();

            using var response = await anonymous.GetAsync("/api/BoardItemAsks/mine?direction=received");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UnknownAskOrItem_Returns404()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);

            using var unknownAsk = await board.Owner.PostAsJsonAsync("/api/BoardItemAsks/987654/accept", new { expectedRevision = 0 });
            using var unknownItem = await board.Owner.GetAsync("/api/BoardItems/987654/asks");

            unknownAsk.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            unknownItem.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task NonMemberAndWrongActor_Return403()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            using var outsider = await factory.CreateSignedInClientAsync("askoutsider");
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);

            using var nonMember = await outsider.GetAsync($"/api/BoardItems/{board.ItemId}/asks");
            using var notTheRecipient = await ActAsync(board.Second, ask, "accept");
            using var notAManager = await board.Second.PostAsync($"/api/BoardItemAsks/{ask["id"]}/nudge", null);

            nonMember.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            notTheRecipient.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            notAManager.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task DomainRuleViolations_Return400_WithTheReasonAsTitle()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);

            using var askYourself = await AskAsync(board.Owner, board.ItemId, board.OwnerId);
            using var stale = await ActAsync(board.Helper, ask, "accept", revision: 99);
            using var wrongState = await ActAsync(board.Helper, ask, "complete");
            using var firstNudge = await board.Owner.PostAsync($"/api/BoardItemAsks/{ask["id"]}/nudge", null);
            using var secondNudge = await board.Owner.PostAsync($"/api/BoardItemAsks/{ask["id"]}/nudge", null);

            askYourself.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(askYourself)).ShouldBe("You can't ask yourself.");
            stale.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(stale)).ShouldContain("changed since you loaded it");
            wrongState.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            firstNudge.StatusCode.ShouldBe(HttpStatusCode.OK);
            secondNudge.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(secondNudge)).ShouldBe("This ask was already nudged in the last 24 hours.");
        }

        [Fact]
        public async Task UnknownDirection_Returns400_AsAValidationProblem()
        {
            using var factory = new ClefCraftApiFactory();
            using var client = await factory.CreateSignedInClientAsync("asksomeone");

            using var bogus = await client.GetAsync("/api/BoardItemAsks/mine?direction=bogus");
            using var missing = await client.GetAsync("/api/BoardItemAsks/mine");
            using var outOfRange = await client.GetAsync("/api/BoardItemAsks/mine?direction=7");

            // Model binding rejects it before the handler: the framework's validation problem, with
            // the field error under "errors", which the client already reads (attachment uploads,
            // registration) before falling back to "title".
            bogus.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            bogus.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
            var problem = await ReadAsync(bogus);
            problem["status"]!.GetValue<int>().ShouldBe(400);
            problem["title"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();
            problem["errors"]!.AsObject().ContainsKey("direction").ShouldBeTrue();

            missing.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadAsync(missing))["errors"]!.AsObject().ContainsKey("direction").ShouldBeTrue();

            // An undefined number is rejected by model binding too, in the same shape.
            outOfRange.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadAsync(outOfRange))["errors"]!.AsObject().ContainsKey("direction").ShouldBeTrue();
        }

        [Fact]
        public async Task MissingExpectedRevision_Returns400_AsAValidationProblem()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);

            using var response = await board.Helper.PostAsJsonAsync($"/api/BoardItemAsks/{ask["id"]}/accept", new { });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadAsync(response))["errors"]!.AsObject().ContainsKey("ExpectedRevision").ShouldBeTrue();
        }

        // ---- Timestamps -----------------------------------------------------------------------------

        // Every ask timestamp must reach the client as an unambiguous instant: UTC with a "Z", the
        // instant the server recorded. (On InMemory the stored Kind is kept; that Npgsql reads
        // timestamptz back as UTC is the provider's documented behaviour.)
        [Fact]
        public async Task AskTimestamps_AreSerializedAsUtcInstants()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var before = DateTimeOffset.UtcNow.AddSeconds(-1);

            using var created = await board.Owner.PostAsJsonAsync($"/api/BoardItems/{board.ItemId}/asks", new
            {
                recipientUserId = board.HelperId,
                note = "Review the migration",
                isRequired = false,
                dueDate = "2026-11-02T00:00:00Z"
            });
            created.StatusCode.ShouldBe(HttpStatusCode.OK);
            var accepted = await ReadAsync(created);
            (await ActAsync(board.Helper, accepted, "accept")).EnsureSuccessStatusCode();

            var nudged = await AskOkAsync(board.Owner, board.ItemId, board.SecondId);
            (await board.Owner.PostAsync($"/api/BoardItemAsks/{nudged["id"]}/nudge", null)).EnsureSuccessStatusCode();
            var after = DateTimeOffset.UtcNow.AddSeconds(1);

            var asks = await AsksOfItemAsync(board.Owner, board.ItemId);
            var first = asks.Single(a => a!["id"]!.GetValue<int>() == accepted["id"]!.GetValue<int>())!;
            var second = asks.Single(a => a!["id"]!.GetValue<int>() == nudged["id"]!.GetValue<int>())!;

            static DateTimeOffset Instant(JsonNode ask, string field)
            {
                var raw = ask[field]!.GetValue<string>();
                raw.ShouldEndWith("Z", customMessage: $"{field} was \"{raw}\"");
                var parsed = DateTimeOffset.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
                parsed.Offset.ShouldBe(TimeSpan.Zero);
                return parsed;
            }

            Instant(first, "dateCreated").ShouldBeInRange(before, after);
            Instant(first, "respondedAt").ShouldBeInRange(before, after);
            Instant(first, "dueDate").ShouldBe(new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero));

            var lastNudgedAt = Instant(second, "lastNudgedAt");
            lastNudgedAt.ShouldBeInRange(before, after);
            (Instant(second, "nextNudgeAt") - lastNudgedAt).ShouldBe(TimeSpan.FromHours(24));
            second["allowedActions"]!.AsArray().Select(a => a!.GetValue<int>()).ShouldNotContain(8); // Nudge
        }

        // ---- Reassign -----------------------------------------------------------------------------

        [Fact]
        public async Task Reassign_CancelsTheAsk_AndAsksTheNewPerson_MovingTheRequirement()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId, isRequired: true);

            using var response = await board.Owner.PostAsJsonAsync($"/api/BoardItemAsks/{ask["id"]}/reassign",
                new { expectedRevision = ask["revision"]!.GetValue<int>(), newRecipientUserId = board.SecondId });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var replacement = await ReadAsync(response);
            replacement["recipientUserId"]!.GetValue<string>().ShouldBe(board.SecondId);
            replacement["isRequired"]!.GetValue<bool>().ShouldBeTrue();
            replacement["state"]!.GetValue<int>().ShouldBe(Pending);

            var old = (await AsksOfItemAsync(board.Owner, board.ItemId)).Single(a => a!["id"]!.GetValue<int>() == ask["id"]!.GetValue<int>())!;
            old["state"]!.GetValue<int>().ShouldBe(Cancelled);
            old["isRequired"]!.GetValue<bool>().ShouldBeFalse();
        }

        [Fact]
        public async Task Reassign_ByTheRecipient_Returns403_AndToANonMember_Returns400()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            using var outsider = await factory.CreateSignedInClientAsync("askoutsider");
            var outsiderId = await UserIdAsync(outsider);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);
            var revision = ask["revision"]!.GetValue<int>();

            using var byRecipient = await board.Helper.PostAsJsonAsync($"/api/BoardItemAsks/{ask["id"]}/reassign",
                new { expectedRevision = revision, newRecipientUserId = board.SecondId });
            using var toOutsider = await board.Owner.PostAsJsonAsync($"/api/BoardItemAsks/{ask["id"]}/reassign",
                new { expectedRevision = revision, newRecipientUserId = outsiderId });

            byRecipient.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            toOutsider.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(toOutsider)).ShouldBe("The person you ask must be a member of this board.");
        }

        // ---- Completing the item ------------------------------------------------------------------

        [Theory]
        [InlineData("put")]
        [InlineData("switch")]
        public async Task Completion_WithAnUnmetRequirement_IsBlocked_OnBothRoutes(string route)
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId, isRequired: true);
            var snapshot = Snapshot(await AsksOfItemAsync(board.Owner, board.ItemId));

            using var noClosure = await CompleteItemAsync(board.Owner, board, null, route);
            using var noOverride = await CompleteItemAsync(board.Owner, board,
                new { acknowledgedAsks = snapshot, completeAskIds = Array.Empty<int>(), overrideAskIds = Array.Empty<int>() }, route);

            noClosure.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(noClosure)).ShouldBe("This item has open or required asks; review them before completing it.");
            noOverride.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(noOverride)).ShouldBe("The item's asks changed; review them again.");
            (await AsksOfItemAsync(board.Owner, board.ItemId)).Single()!["state"]!.GetValue<int>().ShouldBe(Pending);
            ask["id"].ShouldNotBeNull();
        }

        [Fact]
        public async Task Completion_WithTheExactSnapshotAndOverride_Succeeds()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var required = await AskOkAsync(board.Owner, board.ItemId, board.HelperId, isRequired: true);
            var snapshot = Snapshot(await AsksOfItemAsync(board.Owner, board.ItemId));

            using var response = await CompleteItemAsync(board.Owner, board, new
            {
                acknowledgedAsks = snapshot,
                completeAskIds = Array.Empty<int>(),
                overrideAskIds = new[] { required["id"]!.GetValue<int>() }
            });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var asks = await AsksOfItemAsync(board.Owner, board.ItemId);
            asks.Single()!["state"]!.GetValue<int>().ShouldBe(Cancelled);
            asks.Single()!["allowedActions"]!.AsArray().ShouldBeEmpty();
        }

        [Fact]
        public async Task Completion_AfterAPendingAskWasAccepted_IsRejectedAsStale()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);
            var snapshot = Snapshot(await AsksOfItemAsync(board.Owner, board.ItemId));

            (await ActAsync(board.Helper, ask, "accept")).EnsureSuccessStatusCode();

            using var response = await CompleteItemAsync(board.Owner, board,
                new { acknowledgedAsks = snapshot, completeAskIds = Array.Empty<int>(), overrideAskIds = Array.Empty<int>() });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(response)).ShouldBe("The item's asks changed; review them again.");
        }

        [Fact]
        public async Task Completion_AfterAnOptionalAskWasMadeRequired_IsRejectedAsStale()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);
            var snapshot = Snapshot(await AsksOfItemAsync(board.Owner, board.ItemId));

            (await board.Owner.PutAsJsonAsync($"/api/BoardItemAsks/{ask["id"]}", new
            {
                expectedRevision = ask["revision"]!.GetValue<int>(),
                note = ask["note"]!.GetValue<string>(),
                isRequired = true
            })).EnsureSuccessStatusCode();

            using var response = await CompleteItemAsync(board.Owner, board,
                new { acknowledgedAsks = snapshot, completeAskIds = Array.Empty<int>(), overrideAskIds = Array.Empty<int>() });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(response)).ShouldBe("The item's asks changed; review them again.");
        }

        // ---- A board owner without a membership row -----------------------------------------------

        [Fact]
        public async Task OwnerWithoutAMembershipRow_IsTreatedLikeAnyNonMember()
        {
            // Access to a board is membership-based everywhere (BoardAccessService); the owner only
            // gets manager rights over asks once they are a member. Without the row they can't see
            // the board's items either, so asks follow the same rule.
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Helper, board.ItemId, board.SecondId);

            using (var scope = factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ClefCraftDatabaseContext>();
                context.BoardMembers.Remove(await context.BoardMembers.SingleAsync(m => m.BoardId == board.BoardId && m.UserId == board.OwnerId));
                await context.SaveChangesAsync();
            }

            using var items = await board.Owner.GetAsync($"/api/BoardItems/GetBoardItemsByBoardId/{board.BoardId}");
            using var asks = await board.Owner.GetAsync($"/api/BoardItems/{board.ItemId}/asks");
            using var cancel = await ActAsync(board.Owner, ask, "cancel");
            using var mine = await board.Owner.GetAsync("/api/BoardItemAsks/mine?direction=sent");

            items.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            asks.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            cancel.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            mine.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadAsync(mine)).AsArray().ShouldBeEmpty();
        }

        // ---- A concurrency conflict at save time --------------------------------------------------

        private sealed class ConflictOnNextSave : SaveChangesInterceptor
        {
            public bool Armed { get; set; }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                if (Armed)
                {
                    Armed = false;
                    throw new DbUpdateConcurrencyException("Simulated: the row changed since it was read.");
                }

                return ValueTask.FromResult(result);
            }
        }

        private sealed class ConflictingSaveApiFactory : ClefCraftApiFactory
        {
            public ConflictOnNextSave Conflict { get; } = new();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureServices(services =>
                    services.ConfigureDbContext<ClefCraftDatabaseContext>(options => options.AddInterceptors(Conflict)));
            }
        }

        [Fact]
        public async Task ConcurrencyConflictOnSave_Returns400_NotA500_AndChangesNothing()
        {
            using var factory = new ConflictingSaveApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);

            factory.Conflict.Armed = true;
            using var response = await ActAsync(board.Helper, ask, "accept");

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await TitleOfAsync(response)).ShouldBe("This ask or its item changed while you were saving; reload and try again.");
            (await AsksOfItemAsync(board.Owner, board.ItemId)).Single()!["state"]!.GetValue<int>().ShouldBe(Pending);
        }

        // ---- Replies and "Expects an answer" ------------------------------------------------------

        private const int Completed = 3;

        private static async Task<JsonNode> AskExpectingAnAnswerAsync(AskBoard board)
        {
            using var response = await board.Owner.PostAsJsonAsync($"/api/BoardItems/{board.ItemId}/asks",
                new { recipientUserId = board.HelperId, note = "Share the dummy-data SQL", expectsAnswer = true });
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            return await ReadAsync(response);
        }

        private static Task<HttpResponseMessage> ReplyAsync(HttpClient client, JsonNode ask, string body) =>
            client.PostAsJsonAsync($"/api/BoardItemAsks/{ask["id"]!.GetValue<int>()}/replies", new { body });

        [Fact]
        public async Task ExpectedAnswer_BlocksMarkingDone_WithACode_UntilTheRecipientReplies()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskExpectingAnAnswerAsync(board);
            ask["expectsAnswer"]!.GetValue<bool>().ShouldBeTrue();
            ask["isAnswered"]!.GetValue<bool>().ShouldBeFalse();

            using (var accept = await ActAsync(board.Helper, ask, "accept"))
                ask = await ReadAsync(accept);

            using (var blocked = await ActAsync(board.Helper, ask, "complete"))
            {
                blocked.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                var problem = await ReadAsync(blocked);
                problem["code"]!.GetValue<string>().ShouldBe("ASK_ANSWER_REQUIRED");
                problem["title"]!.GetValue<string>().ShouldBe("This ask expects an answer. Reply with the answer before marking it done.");
            }

            // Every other 400 keeps its old shape: no code.
            using (var stale = await ActAsync(board.Helper, ask, "complete", revision: 99))
            {
                stale.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
                (await ReadAsync(stale)).AsObject().ContainsKey("code").ShouldBeFalse();
            }

            using (var reply = await ReplyAsync(board.Helper, ask, "INSERT INTO items (title) VALUES ('a');"))
            {
                reply.StatusCode.ShouldBe(HttpStatusCode.OK);
                var dto = await ReadAsync(reply);
                dto["isAnswered"]!.GetValue<bool>().ShouldBeTrue();
                dto["revision"]!.GetValue<int>().ShouldBe(ask["revision"]!.GetValue<int>());
                var saved = dto["replies"]!.AsArray().Single()!;
                saved["isMine"]!.GetValue<bool>().ShouldBeTrue();
                saved["authorUserId"]!.GetValue<string>().ShouldBe(board.HelperId);
                saved["dateCreated"]!.GetValue<string>().ShouldEndWith("Z");
            }

            using var done = await ActAsync(board.Helper, ask, "complete");
            done.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await ReadAsync(done))["state"]!.GetValue<int>().ShouldBe(Completed);

            var seenByRequester = (await AsksOfItemAsync(board.Owner, board.ItemId)).Single()!;
            seenByRequester["replies"]!.AsArray().Single()!["isMine"]!.GetValue<bool>().ShouldBeFalse();
            seenByRequester["canReply"]!.GetValue<bool>().ShouldBeTrue();
        }

        [Fact]
        public async Task SendAndMarkDone_FromPending_IsOneRequest()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskExpectingAnAnswerAsync(board);

            using var response = await board.Helper.PostAsJsonAsync($"/api/BoardItemAsks/{ask["id"]!.GetValue<int>()}/complete",
                new { expectedRevision = ask["revision"]!.GetValue<int>(), replyBody = "Here is the SQL." });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var dto = await ReadAsync(response);
            dto["state"]!.GetValue<int>().ShouldBe(Completed);
            dto["respondedAt"].ShouldNotBeNull();
            dto["replies"]!.AsArray().Single()!["body"]!.GetValue<string>().ShouldBe("Here is the SQL.");
        }

        [Fact]
        public async Task Replies_AreForTheRequesterAndRecipientOnly_AndOnlyTheAuthorEdits()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);

            using (var outsider = await ReplyAsync(board.Second, ask, "Me too"))
                outsider.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            using (var empty = await ReplyAsync(board.Helper, ask, "   "))
                empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            int replyId;
            using (var reply = await ReplyAsync(board.Owner, ask, "Which table?"))
            {
                reply.StatusCode.ShouldBe(HttpStatusCode.OK);
                replyId = (await ReadAsync(reply))["replies"]!.AsArray().Single()!["id"]!.GetValue<int>();
            }

            using (var notAuthor = await board.Helper.PutAsJsonAsync($"/api/BoardItemAskReplies/{replyId}", new { body = "Changed" }))
                notAuthor.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            using var edit = await board.Owner.PutAsJsonAsync($"/api/BoardItemAskReplies/{replyId}", new { body = "Which table, items?" });
            edit.StatusCode.ShouldBe(HttpStatusCode.OK);
            var edited = (await ReadAsync(edit))["replies"]!.AsArray().Single()!;
            edited["body"]!.GetValue<string>().ShouldBe("Which table, items?");
            edited["dateEdited"].ShouldNotBeNull();
        }

        [Fact]
        public async Task ExpectsAnswerToggle_IsForAskManagers_AndBumpsTheRevision()
        {
            using var factory = new ClefCraftApiFactory();
            using var board = await CreateAskBoardAsync(factory);
            var ask = await AskOkAsync(board.Owner, board.ItemId, board.HelperId);
            var route = $"/api/BoardItemAsks/{ask["id"]!.GetValue<int>()}/expects-answer";
            var revision = ask["revision"]!.GetValue<int>();

            using (var recipient = await board.Helper.PostAsJsonAsync(route, new { expectedRevision = revision, expectsAnswer = true }))
                recipient.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

            using var set = await board.Owner.PostAsJsonAsync(route, new { expectedRevision = revision, expectsAnswer = true });
            set.StatusCode.ShouldBe(HttpStatusCode.OK);
            var dto = await ReadAsync(set);
            dto["expectsAnswer"]!.GetValue<bool>().ShouldBeTrue();
            dto["revision"]!.GetValue<int>().ShouldBe(revision + 1);
        }
    }
}
