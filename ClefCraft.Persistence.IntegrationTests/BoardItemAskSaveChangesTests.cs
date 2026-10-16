using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using ClefCraft.Persistence.Repositories;
using ClefCraft.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit.Abstractions;
using static ClefCraft.Persistence.IntegrationTests.TestHelpers.AskSqliteDatabase;

namespace ClefCraft.Persistence.IntegrationTests
{
    // How asks go through ClefCraftDatabaseContext.SaveChangesAsync on a relational provider: the
    // item's last-modified stamp, the automatic audit rows and the explicit ASK_* history.
    public class BoardItemAskSaveChangesTests
    {
        private readonly ITestOutputHelper _output;

        public BoardItemAskSaveChangesTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task AskOnlyChange_UpdatesAskVersion_WithoutStampingTheItem()
        {
            using var db = await CreateAsync();
            var before = await db.ItemAsync();
            var sql = new List<string>();

            using (var request = db.Request(Requester, sql))
                await request.CreateAskFor(Other);

            var after = await db.ItemAsync();
            after.AskVersion.ShouldBe(before.AskVersion + 1);
            after.DateModified.ShouldBe(before.DateModified);
            after.ModifiedBy.ShouldBe(before.ModifiedBy);

            // Still a conditional update, with only AskVersion set.
            var itemUpdate = sql.Single(s => s.Contains("UPDATE \"BoardItems\""));
            _output.WriteLine(itemUpdate);
            itemUpdate.ShouldContain("SET \"AskVersion\" = @p0");
            itemUpdate.ShouldNotContain("\"DateModified\" = @");
            itemUpdate.ShouldContain("AND \"AskVersion\" = @p");

            (await db.ItemHistoryAsync(AskHistory.Created)).Count.ShouldBe(1);
        }

        [Fact]
        public async Task MixedChange_StampsTheItem_WritesItsHistory_AndKeepsTheConcurrencyCheck()
        {
            using var db = await CreateAsync();

            using (var context = db.NewContext(Requester))
            {
                var item = await context.BoardItems.SingleAsync(i => i.Id == db.ItemId);
                item.Title = "Release notes (final)";
                item.AskVersion++;
                await new ActivityLogger(context, DatabaseContextFactory.CreateUserServiceMock(Requester).Object)
                    .LogAsync("BoardItem", item.Id, AskHistory.Created, new Dictionary<string, object> { ["Note"] = "x" });

                await context.SaveChangesAsync();
            }

            var after = await db.ItemAsync();
            after.Title.ShouldBe("Release notes (final)");
            after.ModifiedBy.ShouldBe(Requester);
            (await db.ItemHistoryAsync(AskHistory.Created)).Count.ShouldBe(1);

            var updated = (await db.ItemHistoryAsync("UPDATED")).ShouldHaveSingleItem();
            var metadata = updated.MetadataJson.ShouldNotBeNull();
            metadata.ShouldContain("Title");
            metadata.ShouldNotContain("AskVersion");
        }

        [Fact]
        public async Task MixedChange_WhenAskVersionChangedMeanwhile_WritesNothing()
        {
            using var db = await CreateAsync();

            using var context = db.NewContext(Requester, interceptors: new CompetingWriteInterceptor(async () =>
            {
                using var other = db.NewContext("someone-else");
                await other.Database.ExecuteSqlRawAsync("UPDATE \"BoardItems\" SET \"AskVersion\" = \"AskVersion\" + 1");
            }));
            var item = await context.BoardItems.SingleAsync(i => i.Id == db.ItemId);
            item.Title = "Lost edit";
            item.AskVersion++;
            await new ActivityLogger(context, DatabaseContextFactory.CreateUserServiceMock(Requester).Object)
                .LogAsync("BoardItem", item.Id, AskHistory.Created, new Dictionary<string, object> { ["Note"] = "x" });

            await Should.ThrowAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());

            (await db.ItemAsync()).Title.ShouldBe("Release notes");
            (await db.ItemHistoryAsync(AskHistory.Created)).ShouldBeEmpty();
        }

        [Fact]
        public async Task CompletingTheItem_StillStampsIt()
        {
            using var db = await CreateAsync();

            using (var request = db.Request(Assignee))
                await request.CompleteItem();

            var after = await db.ItemAsync();
            after.BoardItemStatus.StatusId.ShouldBe(db.DoneStatusId);
            after.ModifiedBy.ShouldBe(Assignee);
        }

        [Fact]
        public async Task ItemHistory_ShowsTheAskEntries_NotTheAsksOwnAuditRows()
        {
            using var db = await CreateAsync();

            using (var request = db.Request(Requester))
                await request.CreateAskFor(Other);

            using var context = db.NewContext("reader");
            var itemHistory = await new ActivityLogRepository(context).GetByEntityAsync("BoardItem", db.ItemId, 0, 50);

            itemHistory.Select(l => l.ActionType).ShouldContain(AskHistory.Created);
            itemHistory.ShouldAllBe(l => l.EntityType == "BoardItem");
            itemHistory.ShouldNotContain(l => l.MetadataJson != null && l.MetadataJson.Contains("AskVersion"));

            // The automatic audit row exists, under the ask's own type, where the History never looks.
            (await context.ActivityLogs.CountAsync(l => l.EntityType == nameof(BoardItemAsk) && l.ActionType == "CREATED")).ShouldBe(1);
        }

        [Fact]
        public async Task Completion_WritesTheOpenAsks_AndLeavesTerminalRequirementsUnwritten()
        {
            // Documents which snapshot asks the completion save itself protects: the asks it changes
            // get a Revision-checked UPDATE; a terminal unmet requirement it only overrides isn't
            // written, so it relies on the check against the acknowledged revision before the save.
            using var db = await CreateAsync();
            var open = await db.AddAskAsync(BoardItemAskState.Pending);
            var declinedRequired = await db.AddAskAsync(BoardItemAskState.Declined, isRequired: true);
            var sql = new List<string>();

            using (var request = db.Request(Assignee, sql))
                await request.CompleteItem(new[] { open, declinedRequired }, overrides: new[] { declinedRequired.Id });

            var askUpdates = sql.Where(s => s.Contains("UPDATE \"BoardItemAsks\"")).ToList();
            askUpdates.ForEach(_output.WriteLine);
            askUpdates.Count.ShouldBe(1);
            askUpdates.Single().ShouldContain("AND \"Revision\" = @p");

            (await db.AskAsync(open.Id)).State.ShouldBe(BoardItemAskState.Cancelled);
            (await db.AskAsync(declinedRequired.Id)).Revision.ShouldBe(1);
            (await db.ItemHistoryAsync(AskHistory.RequirementsOverridden)).Single().MetadataJson!
                .ShouldContain($"[{declinedRequired.Id}]");
        }
    }
}
