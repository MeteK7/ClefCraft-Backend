using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.NudgeAsk;
using ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember;
using ClefCraft.Application.Models.Notifications;
using ClefCraft.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using Xunit.Abstractions;
using static ClefCraft.Persistence.IntegrationTests.TestHelpers.AskSqliteDatabase;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Two requests racing on the same ask or item, on a relational provider (SQLite) so the
    // concurrency predicates are real SQL: the losing request's context has loaded its data, then
    // the winning request runs and commits just before the loser's save. The loser's UPDATE then
    // matches no row, EF throws, and the handler turns that into a 400 with nothing written.
    public class BoardItemAskConcurrencyTests
    {
        private readonly ITestOutputHelper _output;

        public BoardItemAskConcurrencyTests(ITestOutputHelper output) => _output = output;

        // The statements a context executed that update the given table, for checking (and printing)
        // the WHERE predicates EF generated.
        private List<string> UpdatesOf(List<string> sql, string table)
        {
            var updates = sql.Where(s => s.Contains($"UPDATE \"{table}\"")).ToList();
            foreach (var statement in updates)
                _output.WriteLine(statement.Substring(statement.IndexOf("UPDATE", StringComparison.Ordinal)));
            return updates;
        }

        // ---- 1. Two nudges at once ------------------------------------------------------------

        [Fact]
        public async Task TwoSimultaneousNudges_OnlyOneCommits()
        {
            using var db = await CreateAsync();
            var ask = await db.AddAskAsync(BoardItemAskState.Pending);
            var sql = new List<string>();

            using var winner = db.Request(Assignee);
            using var loser = db.Request(Requester, sql, new CompetingWriteInterceptor(() => winner.NudgeAsk(ask)));

            var ex = await Should.ThrowAsync<BadRequestException>(() => loser.NudgeAsk(ask));

            ex.Message.ShouldBe(NudgeAskCommandHandler.ConflictMessage);
            (await db.ItemHistoryAsync(AskHistory.Nudged)).Count.ShouldBe(1);
            (await db.ItemHistoryAsync(AskHistory.Nudged)).Single().UserId.ShouldBe(Assignee);
            loser.ShouldHaveNotifiedNobody();
            winner.Hub.Verify(h => h.SendAskEventAsync(Recipient, It.IsAny<AskNotification>(), It.IsAny<CancellationToken>()), Times.Once);

            // Never nudged before, so the predicate is IS NULL on LastNudgedAt, plus the revision.
            var update = UpdatesOf(sql, "BoardItemAsks").ShouldHaveSingleItem();
            update.ShouldContain("\"LastNudgedAt\" IS NULL");
            update.ShouldContain("AND \"Revision\" = @p");
        }

        // ---- 2. Accept versus cancel ----------------------------------------------------------

        [Fact]
        public async Task AcceptVersusCancel_TheCancelWins_AndTheAcceptChangesNothing()
        {
            using var db = await CreateAsync();
            var ask = await db.AddAskAsync(BoardItemAskState.Pending);
            var sql = new List<string>();

            using var canceller = db.Request(Requester);
            using var accepter = db.Request(Recipient, sql, new CompetingWriteInterceptor(() => canceller.Cancel(ask)));

            await Should.ThrowAsync<BadRequestException>(() => accepter.Accept(ask));

            (await db.AskAsync(ask.Id)).State.ShouldBe(BoardItemAskState.Cancelled);
            (await db.ItemHistoryAsync(AskHistory.Accepted)).ShouldBeEmpty();
            accepter.ShouldHaveNotifiedNobody();

            UpdatesOf(sql, "BoardItemAsks").ShouldHaveSingleItem()
                .ShouldContain("AND \"LastNudgedAt\" IS NULL AND \"Revision\" = @");
        }

        // ---- 3. Item completion versus ask creation -----------------------------------------------

        [Fact]
        public async Task CompletionVersusAskCreation_TheNewAskWins_AndTheItemStaysOpen()
        {
            using var db = await CreateAsync();
            var sql = new List<string>();

            using var asker = db.Request(Requester);
            using var completer = db.Request(Assignee, sql, new CompetingWriteInterceptor(() => asker.CreateAskFor(Other)));

            var ex = await Should.ThrowAsync<BadRequestException>(() => completer.CompleteItem());

            ex.Message.ShouldBe(UpdateBoardItemCommandHandler.ConflictMessage);
            (await db.ItemAsync()).BoardItemStatus.StatusId.ShouldBe(db.InProgressStatusId);
            (await db.ItemHistoryAsync(AskHistory.Created)).Count.ShouldBe(1);
            completer.ShouldHaveNotifiedNobody();

            UpdatesOf(sql, "BoardItems").ShouldHaveSingleItem().ShouldContain("WHERE \"Id\" = @p");
            UpdatesOf(sql, "BoardItems").Single().ShouldContain("AND \"AskVersion\" = @p");
        }

        [Fact]
        public async Task AskCreationVersusCompletion_TheCompletionWins_AndNoAskIsLeftOpenOnTheDoneItem()
        {
            using var db = await CreateAsync();
            var sql = new List<string>();

            using var completer = db.Request(Assignee);
            using var asker = db.Request(Requester, sql, new CompetingWriteInterceptor(() => completer.CompleteItem()));

            await Should.ThrowAsync<BadRequestException>(() => asker.CreateAskFor(Other));

            (await db.ItemAsync()).BoardItemStatus.StatusId.ShouldBe(db.DoneStatusId);
            using (var context = db.NewContext("reader"))
                (await context.BoardItemAsks.CountAsync()).ShouldBe(0);
            (await db.ItemHistoryAsync(AskHistory.Created)).ShouldBeEmpty();
            asker.ShouldHaveNotifiedNobody();

            UpdatesOf(sql, "BoardItems").ShouldHaveSingleItem().ShouldContain("AND \"AskVersion\" = @p");
        }

        // ---- 4. Item completion versus the recipient reopening ------------------------------------

        [Fact]
        public async Task CompletionVersusReopen_TheReopenWins_AndTheItemStaysOpen()
        {
            using var db = await CreateAsync();
            var done = await db.AddAskAsync(BoardItemAskState.Completed);

            using var reopener = db.Request(Recipient);
            using var completer = db.Request(Assignee, interceptors: new CompetingWriteInterceptor(() => reopener.Reopen(done)));

            await Should.ThrowAsync<BadRequestException>(() => completer.CompleteItem());

            (await db.ItemAsync()).BoardItemStatus.StatusId.ShouldBe(db.InProgressStatusId);
            (await db.AskAsync(done.Id)).State.ShouldBe(BoardItemAskState.Accepted);
            completer.ShouldHaveNotifiedNobody();
        }

        [Fact]
        public async Task ReopenVersusCompletion_TheCompletionWins_AndTheAskStaysCompleted()
        {
            using var db = await CreateAsync();
            var done = await db.AddAskAsync(BoardItemAskState.Completed);

            using var completer = db.Request(Assignee);
            using var reopener = db.Request(Recipient, interceptors: new CompetingWriteInterceptor(() => completer.CompleteItem()));

            await Should.ThrowAsync<BadRequestException>(() => reopener.Reopen(done));

            (await db.ItemAsync()).BoardItemStatus.StatusId.ShouldBe(db.DoneStatusId);
            (await db.AskAsync(done.Id)).State.ShouldBe(BoardItemAskState.Completed);
            reopener.ShouldHaveNotifiedNobody();
        }

        // ---- 5. Item completion versus decline -------------------------------------------------

        [Fact]
        public async Task CompletionVersusDecline_AskChangedBetweenValidationAndSave_NothingFromTheCompletionIsWritten()
        {
            using var db = await CreateAsync();
            var accepted = await db.AddAskAsync(BoardItemAskState.Accepted);
            var sql = new List<string>();

            using var decliner = db.Request(Recipient);
            using var completer = db.Request(Assignee, sql, new CompetingWriteInterceptor(() => decliner.Decline(accepted)));

            // The snapshot is current when the completion validates it; the decline lands before its save.
            var ex = await Should.ThrowAsync<BadRequestException>(() => completer.CompleteItem(new[] { accepted }, title: "Release notes (final)"));

            ex.Message.ShouldBe(UpdateBoardItemCommandHandler.ConflictMessage);

            var item = await db.ItemAsync();
            item.BoardItemStatus.StatusId.ShouldBe(db.InProgressStatusId);
            item.Title.ShouldBe("Release notes");
            (await db.AskAsync(accepted.Id)).State.ShouldBe(BoardItemAskState.Declined);
            (await db.ItemHistoryAsync(AskHistory.Cancelled)).ShouldBeEmpty();
            (await db.ItemHistoryAsync("UPDATED")).ShouldBeEmpty();
            completer.ShouldHaveNotifiedNobody();

            UpdatesOf(sql, "BoardItemAsks").ShouldNotBeEmpty();
            sql.ShouldContain(s => s.Contains("UPDATE \"BoardItemAsks\"") && s.Contains("AND \"Revision\" = @p"));
        }

        // ---- 6. Member removal versus accept ------------------------------------------------------

        [Fact]
        public async Task RemovalVersusAccept_TheAcceptWins_AndTheMemberIsNotRemoved()
        {
            using var db = await CreateAsync();
            var pending = await db.AddAskAsync(BoardItemAskState.Pending);

            using var accepter = db.Request(Recipient);
            using var remover = db.Request(Owner, interceptors: new CompetingWriteInterceptor(() => accepter.Accept(pending)));

            var ex = await Should.ThrowAsync<BadRequestException>(() => remover.RemoveFromBoard(Recipient));

            ex.Message.ShouldBe(RemoveBoardMemberCommandHandler.ConflictMessage);
            (await db.IsMemberAsync(Recipient)).ShouldBeTrue();
            (await db.AskAsync(pending.Id)).State.ShouldBe(BoardItemAskState.Accepted);
            (await db.ItemHistoryAsync(AskHistory.Cancelled)).ShouldBeEmpty();
            remover.ShouldHaveNotifiedNobody();
        }

        [Fact]
        public async Task AcceptVersusRemoval_TheRemovalWins_AndTheAskIsCancelled()
        {
            using var db = await CreateAsync();
            var pending = await db.AddAskAsync(BoardItemAskState.Pending);

            using var remover = db.Request(Owner);
            using var accepter = db.Request(Recipient, interceptors: new CompetingWriteInterceptor(() => remover.RemoveFromBoard(Recipient)));

            await Should.ThrowAsync<BadRequestException>(() => accepter.Accept(pending));

            (await db.IsMemberAsync(Recipient)).ShouldBeFalse();
            var ask = await db.AskAsync(pending.Id);
            ask.State.ShouldBe(BoardItemAskState.Cancelled);
            ask.CancelReason.ShouldBe(BoardItemAskCancelReason.RecipientRemoved);
            accepter.ShouldHaveNotifiedNobody();
        }

        // ---- AskVersion: every path that opens an ask, and completion, is a conditional UPDATE -----

        [Theory]
        [InlineData("create")]
        [InlineData("ask again")]
        [InlineData("reassign")]
        [InlineData("reopen")]
        [InlineData("complete item")]
        public async Task EveryAskVersionPath_FailsWith400_WhenAnotherRequestChangedAskVersionFirst(string path)
        {
            using var db = await CreateAsync();
            var declined = await db.AddAskAsync(BoardItemAskState.Declined);
            var pending = await db.AddAskAsync(BoardItemAskState.Pending);
            var completed = await db.AddAskAsync(BoardItemAskState.Completed);
            var sql = new List<string>();

            async Task BumpAskVersion()
            {
                using var other = db.NewContext("someone-else");
                await other.Database.ExecuteSqlRawAsync("UPDATE \"BoardItems\" SET \"AskVersion\" = \"AskVersion\" + 1");
            }

            var caller = path == "reopen" ? Recipient : Requester;
            using var request = db.Request(caller, sql, new CompetingWriteInterceptor(BumpAskVersion));

            Task act = path switch
            {
                "create" => request.CreateAskFor(Other),
                "ask again" => request.CreateAskFor(Other, replacesAskId: declined.Id),
                "reassign" => request.ReassignTo(pending, Other),
                "reopen" => request.Reopen(completed),
                _ => request.CompleteItem(new[] { pending })
            };

            await Should.ThrowAsync<BadRequestException>(() => act);

            var itemUpdate = UpdatesOf(sql, "BoardItems").ShouldHaveSingleItem();
            itemUpdate.ShouldContain("\"AskVersion\" = @p");
            itemUpdate.ShouldContain("WHERE \"Id\" = @p");

            request.ShouldHaveNotifiedNobody();
            (await db.ItemAsync()).BoardItemStatus.StatusId.ShouldBe(db.InProgressStatusId);
            (await db.AskAsync(pending.Id)).State.ShouldBe(BoardItemAskState.Pending);
            (await db.AskAsync(completed.Id)).State.ShouldBe(BoardItemAskState.Completed);
            using var reader = db.NewContext("reader");
            (await reader.BoardItemAsks.CountAsync()).ShouldBe(3);
            (await db.ItemHistoryAsync()).Where(l => l.ActionType.StartsWith("ASK_")).ShouldBeEmpty();
        }
    }
}
