using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using ClefCraft.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static ClefCraft.Persistence.IntegrationTests.TestHelpers.AskSqliteDatabase;

namespace ClefCraft.Persistence.IntegrationTests
{
    // Ask replies as SQL (SQLite): loading for many asks at once, stamping and the cascade.
    public class BoardItemAskReplyRepositoryTests
    {
        private static async Task<BoardItemAskReply> AddReplyAsync(AskSqliteDatabase db, BoardItemAsk ask, string author, string body)
        {
            using var context = db.NewContext(author);
            var reply = new BoardItemAskReply { BoardItemAskId = ask.Id, AuthorUserId = author, Body = body };
            await new BoardItemAskRepository(context).AddReplyAsync(reply);
            await context.SaveChangesAsync();
            return reply;
        }

        [Fact]
        public async Task GetRepliesByAskIdsReadOnly_ReturnsTheGivenAsksReplies_OldestFirst_Untracked()
        {
            using var db = await CreateAsync();
            var first = await db.AddAskAsync(BoardItemAskState.Accepted);
            var second = await db.AddAskAsync(BoardItemAskState.Pending);
            var unrelated = await db.AddAskAsync(BoardItemAskState.Pending);
            var one = await AddReplyAsync(db, first, Recipient, "one");
            var question = await AddReplyAsync(db, second, Requester, "question");
            var two = await AddReplyAsync(db, first, Requester, "two");
            await AddReplyAsync(db, unrelated, Recipient, "elsewhere");

            using var context = db.NewContext("reader");
            var repository = new BoardItemAskRepository(context);

            var replies = await repository.GetRepliesByAskIdsReadOnlyAsync(new[] { first.Id, second.Id });

            replies.Select(r => r.Id).ShouldBe(new[] { one.Id, question.Id, two.Id });
            replies.Select(r => r.Body).ShouldBe(new[] { "one", "question", "two" });
            replies.ShouldAllBe(r => r.DateCreated != null);
            context.ChangeTracker.Entries<BoardItemAskReply>().ShouldBeEmpty();
            (await repository.GetRepliesByAskIdsReadOnlyAsync(Array.Empty<int>())).ShouldBeEmpty();
        }

        [Fact]
        public async Task SavingAReply_StampsWhenAndWho_AndEditsAreSaved()
        {
            using var db = await CreateAsync();
            var ask = await db.AddAskAsync(BoardItemAskState.Accepted);
            var reply = await AddReplyAsync(db, ask, Recipient, "SELECT 1;");

            reply.DateCreated.ShouldNotBeNull();
            reply.CreatedBy.ShouldBe(Recipient);

            using (var context = db.NewContext(Recipient))
            {
                var tracked = (await new BoardItemAskRepository(context).GetReplyByIdAsync(reply.Id)).ShouldNotBeNull();
                tracked.Body = "SELECT 2;";
                tracked.DateEdited = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }

            using var reader = db.NewContext("reader");
            var saved = await reader.BoardItemAskReplies.SingleAsync();
            saved.Body.ShouldBe("SELECT 2;");
            saved.DateEdited.ShouldNotBeNull();
        }

        [Fact]
        public async Task ANewReply_CanBeLinkedAsTheAnswer_InTheSameSave()
        {
            using var db = await CreateAsync();
            var ask = await db.AddAskAsync(BoardItemAskState.Accepted);

            using (var context = db.NewContext(Recipient))
            {
                var repository = new BoardItemAskRepository(context);
                var tracked = (await repository.GetByIdAsync(ask.Id)).ShouldNotBeNull();
                var reply = new BoardItemAskReply { BoardItemAskId = ask.Id, AuthorUserId = Recipient, Body = "Here it is." };
                await repository.AddReplyAsync(reply);
                tracked.State = BoardItemAskState.Completed;
                tracked.AnswerReply = reply;
                await context.SaveChangesAsync();
            }

            using var reader = db.NewContext("reader");
            var saved = await reader.BoardItemAsks.SingleAsync(a => a.Id == ask.Id);
            var answer = await reader.BoardItemAskReplies.SingleAsync();
            saved.AnswerReplyId.ShouldBe(answer.Id);
            answer.Body.ShouldBe("Here it is.");
        }

        [Fact]
        public async Task DeletingAnAsk_DeletesItsReplies()
        {
            using var db = await CreateAsync();
            var ask = await db.AddAskAsync(BoardItemAskState.Accepted);
            var kept = await db.AddAskAsync(BoardItemAskState.Pending);
            await AddReplyAsync(db, ask, Recipient, "gone with the ask");
            await AddReplyAsync(db, kept, Recipient, "stays");

            using (var context = db.NewContext(Requester))
            {
                context.BoardItemAsks.Remove(await context.BoardItemAsks.SingleAsync(a => a.Id == ask.Id));
                await context.SaveChangesAsync();
            }

            using var reader = db.NewContext("reader");
            (await reader.BoardItemAskReplies.Select(r => r.Body).ToListAsync()).ShouldBe(new[] { "stays" });
        }
    }
}
