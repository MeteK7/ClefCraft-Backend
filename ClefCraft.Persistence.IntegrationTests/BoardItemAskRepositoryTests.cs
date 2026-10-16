using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using ClefCraft.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static ClefCraft.Persistence.IntegrationTests.TestHelpers.AskSqliteDatabase;

namespace ClefCraft.Persistence.IntegrationTests
{
    // The ask queries as SQL (SQLite), including the GetMyAsks filters the handler builds.
    public class BoardItemAskRepositoryTests
    {
        // A second board, where the test users are not members, with one item.
        private static async Task<BoardItem> OtherBoardItemAsync(AskSqliteDatabase db)
        {
            using var context = db.NewContext("seed-user");
            var board = new Board { Title = "Other", OwnerUserId = "someone-else" };
            context.Boards.Add(board);
            await context.SaveChangesAsync();

            var column = new BoardColumn { Title = "To Do", BoardItems = new List<BoardItem>() };
            context.BoardColumnMappings.Add(new BoardColumnMapping { BoardId = board.Id, BoardColumn = column });
            await context.SaveChangesAsync();

            var item = new BoardItem { BoardId = board.Id, BoardColumnId = column.Id, Title = "Elsewhere" };
            context.BoardItems.Add(item);
            await context.SaveChangesAsync();
            return item;
        }

        private static async Task<BoardItemAsk> AddAskOnAsync(AskSqliteDatabase db, BoardItem item, BoardItemAskState state)
        {
            using var context = db.NewContext(Requester);
            var ask = new BoardItemAsk
            {
                BoardItemId = item.Id,
                RequestedByUserId = Requester,
                RecipientUserId = Recipient,
                Note = "Elsewhere",
                State = state,
                Revision = 1
            };
            context.BoardItemAsks.Add(ask);
            await context.SaveChangesAsync();
            return ask;
        }

        [Fact]
        public async Task GetOpenByRecipientOnBoard_IsThatRecipientsPendingAndAcceptedAsks_OnThatBoardOnly()
        {
            using var db = await CreateAsync();
            var pending = await db.AddAskAsync(BoardItemAskState.Pending);
            var accepted = await db.AddAskAsync(BoardItemAskState.Accepted);
            await db.AddAskAsync(BoardItemAskState.Declined);
            await db.AddAskAsync(BoardItemAskState.Completed);
            await db.AddAskAsync(BoardItemAskState.Pending, recipient: Other);
            await AddAskOnAsync(db, await OtherBoardItemAsync(db), BoardItemAskState.Pending);

            using var context = db.NewContext("reader");
            var result = await new BoardItemAskRepository(context).GetOpenByRecipientOnBoardAsync(db.BoardId, Recipient);

            result.Select(a => a.Id).ShouldBe(new[] { pending.Id, accepted.Id });
            result.ShouldAllBe(a => a.BoardItem.Title == "Release notes");
            context.Entry(result[0]).State.ShouldBe(EntityState.Unchanged); // tracked, so the handler can cancel them
        }

        [Fact]
        public async Task GetByItemIdsReadOnly_ReturnsTheGivenItemsAsks_Untracked()
        {
            using var db = await CreateAsync();
            var here = await db.AddAskAsync(BoardItemAskState.Pending);
            var otherItem = await OtherBoardItemAsync(db);
            var there = await AddAskOnAsync(db, otherItem, BoardItemAskState.Accepted);
            await db.AddAskAsync(BoardItemAskState.Declined);

            using var context = db.NewContext("reader");
            var repository = new BoardItemAskRepository(context);

            (await repository.GetByItemIdsReadOnlyAsync(new[] { db.ItemId, otherItem.Id })).Count.ShouldBe(3);
            (await repository.GetByItemIdsReadOnlyAsync(new[] { otherItem.Id })).Single().Id.ShouldBe(there.Id);
            (await repository.GetByItemIdsReadOnlyAsync(Array.Empty<int>())).ShouldBeEmpty();
            context.ChangeTracker.Entries<BoardItemAsk>().ShouldBeEmpty();
            here.Id.ShouldBeGreaterThan(0);
        }

        [Fact]
        public async Task ReceivedFilter_TranslatesToSql_WithItemBoardAndStatusLoaded()
        {
            using var db = await CreateAsync();
            var pending = await db.AddAskAsync(BoardItemAskState.Pending);
            var accepted = await db.AddAskAsync(BoardItemAskState.Accepted);
            await db.AddAskAsync(BoardItemAskState.Declined);
            await db.AddAskAsync(BoardItemAskState.Pending, recipient: Other);
            await AddAskOnAsync(db, await OtherBoardItemAsync(db), BoardItemAskState.Pending);

            using var context = db.NewContext("reader");
            var result = await new BoardItemAskRepository(context)
                .GetWithItemReadOnlyAsync(MyAskFilters.Received(Recipient, new List<int> { db.BoardId }));

            result.Select(a => a.Id).ShouldBe(new[] { pending.Id, accepted.Id });
            result[0].BoardItem.Board.OwnerUserId.ShouldBe(Owner);
            result[0].BoardItem.BoardItemStatus.Status.Name.ShouldBe("In Progress");
        }

        [Fact]
        public async Task SentFilter_KeepsDeclinedAsksFor14Days()
        {
            using var db = await CreateAsync();
            var pending = await db.AddAskAsync(BoardItemAskState.Pending);
            var recent = await db.AddAskAsync(BoardItemAskState.Declined);
            var old = await db.AddAskAsync(BoardItemAskState.Declined);
            await db.AddAskAsync(BoardItemAskState.Cancelled);
            await db.AddAskAsync(BoardItemAskState.Completed);

            using (var context = db.NewContext("seed-user"))
            {
                (await context.BoardItemAsks.SingleAsync(a => a.Id == recent.Id)).RespondedAt = DateTime.UtcNow.AddDays(-13);
                (await context.BoardItemAsks.SingleAsync(a => a.Id == old.Id)).RespondedAt = DateTime.UtcNow.AddDays(-15);
                await context.SaveChangesAsync();
            }

            using var reader = db.NewContext("reader");
            var result = await new BoardItemAskRepository(reader).GetWithItemReadOnlyAsync(
                MyAskFilters.Sent(Requester, new List<int> { db.BoardId }, DateTime.UtcNow - BoardItemAskLimits.SentDeclinedWindow));

            result.Select(a => a.Id).ShouldBe(new[] { pending.Id, recent.Id });
        }

        [Fact]
        public async Task DeletingAnItem_DeletesItsAsks()
        {
            using var db = await CreateAsync();
            await db.AddAskAsync(BoardItemAskState.Pending);
            await db.AddAskAsync(BoardItemAskState.Completed);

            using (var context = db.NewContext(Owner))
            {
                context.BoardItems.Remove(await context.BoardItems.SingleAsync(i => i.Id == db.ItemId));
                await context.SaveChangesAsync();
            }

            using var reader = db.NewContext("reader");
            (await reader.BoardItemAsks.CountAsync()).ShouldBe(0);
        }
    }
}
