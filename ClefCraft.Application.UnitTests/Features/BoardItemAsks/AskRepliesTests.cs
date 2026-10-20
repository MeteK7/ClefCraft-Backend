using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.AddAskReply;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ChangeAskState;
using ClefCraft.Application.Features.BoardItemAsks.Commands.CreateAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.EditAskReply;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ReassignAsk;
using ClefCraft.Application.Features.BoardItemAsks.Commands.SetAskExpectsAnswer;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetAsksForItem;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class AskRepliesTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Recipient };

        private Task<BoardItemAskDto> Reply(BoardItemAsk ask, string body = "SELECT * FROM items;") =>
            _bed.AddReplyHandler().Handle(new AddAskReplyCommand { AskId = ask.Id, Body = body }, CancellationToken.None);

        private Task<BoardItemAskDto> Edit(BoardItemAskReply reply, string body) =>
            _bed.EditReplyHandler().Handle(new EditAskReplyCommand { ReplyId = reply.Id, Body = body }, CancellationToken.None);

        private Task<BoardItemAskDto> Complete(BoardItemAsk ask, string? replyBody = null, int? expectedRevision = null) =>
            _bed.ChangeStateHandler().Handle(new ChangeAskStateCommand
            {
                AskId = ask.Id,
                ExpectedRevision = expectedRevision ?? ask.Revision,
                Action = BoardItemAskAction.Complete,
                ReplyBody = replyBody
            }, CancellationToken.None);

        private Task<BoardItemAskDto> SetExpectsAnswer(BoardItemAsk ask, bool expectsAnswer) =>
            _bed.SetExpectsAnswerHandler().Handle(new SetAskExpectsAnswerCommand
            {
                AskId = ask.Id,
                ExpectedRevision = ask.Revision,
                ExpectsAnswer = expectsAnswer
            }, CancellationToken.None);

        // ---- Posting ------------------------------------------------------------------------------

        [Theory]
        [InlineData(Recipient, Requester)]
        [InlineData(Requester, Recipient)]
        public async Task Reply_ByEitherParticipant_IsSavedTrimmed_AndTellsOnlyTheOtherOne(string author, string otherParty)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = author;

            var dto = await Reply(ask, "  SELECT 1;\n  ");

            var saved = _bed.Replies.ShouldHaveSingleItem();
            saved.AuthorUserId.ShouldBe(author);
            saved.Body.ShouldBe("SELECT 1;");
            dto.Replies.ShouldHaveSingleItem().IsMine.ShouldBeTrue();

            var sent = _bed.Sent.ShouldHaveSingleItem();
            sent.UserId.ShouldBe(otherParty);
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Replied));
            sent.Payload.ReplyId.ShouldBe(saved.Id);
            sent.Payload.ReplyExcerpt.ShouldBe("SELECT 1;");
        }

        [Fact]
        public async Task Reply_ChangesNothingAboutTheAsk()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            var askVersion = _bed.Item.AskVersion;

            var dto = await Reply(ask);

            ask.State.ShouldBe(BoardItemAskState.Accepted);
            ask.Revision.ShouldBe(3);
            _bed.Item.AskVersion.ShouldBe(askVersion);
            _bed.Logged.ShouldBeEmpty();
            dto.AllowedActions.ShouldBe(new[] { BoardItemAskAction.Decline, BoardItemAskAction.Complete, BoardItemAskAction.CompleteOnBehalf });
        }

        [Theory]
        [InlineData(Assignee)]
        [InlineData(Owner)]
        [InlineData(Other)]
        public async Task Reply_BySomeoneElseOnTheBoard_IsForbidden(string caller)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = caller;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Reply(ask));
            _bed.Replies.ShouldBeEmpty();
        }

        [Fact]
        public async Task Reply_BySomeoneNotOnTheBoard_IsForbidden()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = Outsider;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Reply(ask));
        }

        [Fact]
        public async Task Reply_OnACancelledAsk_IsRejected()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Cancelled);

            var ex = await Should.ThrowAsync<BadRequestException>(() => Reply(ask));
            ex.Message.ShouldBe(AskReplies.CancelledMessage);
            _bed.Replies.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(BoardItemAskState.Pending)]
        [InlineData(BoardItemAskState.Accepted)]
        [InlineData(BoardItemAskState.Completed)]
        [InlineData(BoardItemAskState.Declined)]
        public async Task Reply_IsAllowedInEveryStateButCancelled(BoardItemAskState state)
        {
            var ask = _bed.AddAsk(state);

            await Reply(ask);

            _bed.Replies.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task Reply_IsAllowedWhileTheItemIsDone()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Completed);
            _bed.MarkItemDone();

            var dto = await Reply(ask);

            dto.Replies.ShouldHaveSingleItem();
            dto.CanReply.ShouldBeTrue();
            dto.AllowedActions.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   \n\t ")]
        public async Task Reply_Empty_IsRejected(string body)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            var ex = await Should.ThrowAsync<BadRequestException>(() => Reply(ask, body));
            ex.ValidationErrors!.Values.SelectMany(v => v).ShouldContain("Write a reply");
        }

        [Fact]
        public async Task Reply_LongerThanTheLimit_IsRejected_ButSurroundingWhitespaceDoesNotCount()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<BadRequestException>(() => Reply(ask, new string('x', BoardItemAskLimits.ReplyMaxLength + 1)));

            await Reply(ask, "  " + new string('x', BoardItemAskLimits.ReplyMaxLength) + "  ");
            _bed.Replies.ShouldHaveSingleItem().Body.Length.ShouldBe(BoardItemAskLimits.ReplyMaxLength);
        }

        [Fact]
        public async Task ReplyNotification_CarriesAShortExcerpt()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Reply(ask, "INSERT INTO items\n  (title)\nVALUES ('" + new string('a', 300) + "');");

            var excerpt = _bed.Sent.Single().Payload.ReplyExcerpt!;
            excerpt.ShouldStartWith("INSERT INTO items (title) VALUES ('");
            excerpt.Length.ShouldBe(BoardItemAskLimits.ReplyExcerptLength + 1);
            excerpt.ShouldEndWith("…");
        }

        // ---- Editing ------------------------------------------------------------------------------

        [Fact]
        public async Task Edit_ByTheAuthor_ChangesTheBodyAndMarksItEdited()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            var reply = _bed.AddReply(ask, Recipient);

            var dto = await Edit(reply, "  SELECT 2;  ");

            reply.Body.ShouldBe("SELECT 2;");
            reply.DateEdited.ShouldNotBeNull();
            dto.Replies.Single().DateEdited.ShouldBe(reply.DateEdited);
            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task Edit_WithTheSameText_IsNotAnEdit()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            var reply = _bed.AddReply(ask, Recipient, "SELECT 1;");

            await Edit(reply, "SELECT 1; ");

            reply.DateEdited.ShouldBeNull();
            _bed.SaveCount.ShouldBe(0);
        }

        [Theory]
        [InlineData(Requester)]
        [InlineData(Owner)]
        public async Task Edit_BySomeoneOtherThanTheAuthor_IsForbidden(string caller)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            var reply = _bed.AddReply(ask, Recipient, "SELECT 1;");
            _bed.CallerId = caller;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Edit(reply, "DROP TABLE items;"));
            reply.Body.ShouldBe("SELECT 1;");
        }

        [Fact]
        public async Task Edit_IsValidatedLikeANewReply()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            var reply = _bed.AddReply(ask, Recipient);

            await Should.ThrowAsync<BadRequestException>(() => Edit(reply, "  "));
            await Should.ThrowAsync<BadRequestException>(() => Edit(reply, new string('x', BoardItemAskLimits.ReplyMaxLength + 1)));
        }

        [Fact]
        public async Task Edit_OnACancelledAsk_IsRejected()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Cancelled);
            var reply = _bed.AddReply(ask, Recipient);

            await Should.ThrowAsync<BadRequestException>(() => Edit(reply, "changed"));
        }

        [Fact]
        public async Task Edit_UnknownReply_IsNotFound()
        {
            await Should.ThrowAsync<NotFoundException>(() =>
                _bed.EditReplyHandler().Handle(new EditAskReplyCommand { ReplyId = 999, Body = "x" }, CancellationToken.None));
        }

        // ---- Expects an answer --------------------------------------------------------------------

        [Fact]
        public async Task Complete_WhenAnAnswerIsExpectedAndNoneWasGiven_IsRejectedWithItsCode()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            ask.ExpectsAnswer = true;

            var ex = await Should.ThrowAsync<BadRequestException>(() => Complete(ask));

            ex.Code.ShouldBe(AskReplies.AnswerRequiredCode);
            ex.Message.ShouldBe(AskReplies.AnswerRequiredMessage);
            ask.State.ShouldBe(BoardItemAskState.Accepted);
            _bed.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Complete_WhenOnlyTheRequesterReplied_IsStillRejected()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            ask.ExpectsAnswer = true;
            _bed.AddReply(ask, Requester, "Which table?");

            (await Should.ThrowAsync<BadRequestException>(() => Complete(ask))).Code.ShouldBe(AskReplies.AnswerRequiredCode);
        }

        [Fact]
        public async Task Complete_OnceTheRecipientReplied_Succeeds()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            ask.ExpectsAnswer = true;
            _bed.AddReply(ask, Recipient);

            var dto = await Complete(ask);

            ask.State.ShouldBe(BoardItemAskState.Completed);
            dto.IsAnswered.ShouldBeTrue();
        }

        [Fact]
        public async Task Complete_WithOnlyAnAnswerFromBeforeTheLastReopen_IsRejected_UntilTheRecipientRepliesAgain()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            ask.ExpectsAnswer = true;
            ask.LastReopenedAt = DateTime.UtcNow.AddMinutes(-10);
            _bed.AddReply(ask, Recipient, at: DateTime.UtcNow.AddMinutes(-20));

            (await Should.ThrowAsync<BadRequestException>(() => Complete(ask))).Code.ShouldBe(AskReplies.AnswerRequiredCode);

            _bed.AddReply(ask, Recipient, at: DateTime.UtcNow.AddMinutes(-1));
            await Complete(ask);
            ask.State.ShouldBe(BoardItemAskState.Completed);
        }

        [Fact]
        public async Task Reopen_RecordsWhen_SoEarlierRepliesNoLongerAnswer()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Completed);
            ask.ExpectsAnswer = true;
            _bed.AddReply(ask, Recipient, at: DateTime.UtcNow.AddHours(-4));

            var dto = await _bed.ChangeStateHandler().Handle(new ChangeAskStateCommand
            {
                AskId = ask.Id,
                ExpectedRevision = ask.Revision,
                Action = BoardItemAskAction.Reopen
            }, CancellationToken.None);

            ask.LastReopenedAt.ShouldNotBeNull();
            dto.IsAnswered.ShouldBeFalse();
        }

        [Fact]
        public async Task Complete_WithoutExpectsAnswer_NeedsNoReply()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);

            var dto = await Complete(ask);

            ask.State.ShouldBe(BoardItemAskState.Completed);
            dto.IsAnswered.ShouldBeFalse();
        }

        [Fact]
        public void IsAnswered_CountsARecipientReplyPostedWhilePending()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.AddReply(ask, Recipient);

            AskReplies.IsAnswered(ask, _bed.Replies).ShouldBeTrue();
        }

        [Fact]
        public async Task SetExpectsAnswer_LikeMakeRequired_BumpsTheRevisionAndRecordsIt()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = Requester;

            var dto = await SetExpectsAnswer(ask, true);

            ask.ExpectsAnswer.ShouldBeTrue();
            ask.Revision.ShouldBe(4);
            _bed.LoggedOf(AskHistory.AnswerExpected).ShouldHaveSingleItem().EntityId.ShouldBe(ItemId);
            dto.AllowedActions.ShouldContain(BoardItemAskAction.ClearExpectsAnswer);
            dto.AllowedActions.ShouldNotContain(BoardItemAskAction.SetExpectsAnswer);
            _bed.Sent.ShouldBeEmpty();

            await SetExpectsAnswer(ask, false);

            ask.ExpectsAnswer.ShouldBeFalse();
            ask.Revision.ShouldBe(5);
            _bed.LoggedOf(AskHistory.AnswerNotExpected).ShouldHaveSingleItem();
        }

        [Theory]
        [InlineData(Recipient)]
        [InlineData(Other)]
        public async Task SetExpectsAnswer_ByANonManager_IsForbidden(string caller)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = caller;

            await Should.ThrowAsync<ForbiddenAccessException>(() => SetExpectsAnswer(ask, true));
        }

        [Fact]
        public async Task SetExpectsAnswer_OnACompletedAsk_OrWithAStaleRevision_IsRejected()
        {
            _bed.CallerId = Requester;
            var completed = _bed.AddAsk(BoardItemAskState.Completed);
            await Should.ThrowAsync<BadRequestException>(() => SetExpectsAnswer(completed, true));

            var pending = _bed.AddAsk(BoardItemAskState.Pending);
            await Should.ThrowAsync<BadRequestException>(() => _bed.SetExpectsAnswerHandler().Handle(new SetAskExpectsAnswerCommand
            {
                AskId = pending.Id,
                ExpectedRevision = pending.Revision - 1,
                ExpectsAnswer = true
            }, CancellationToken.None));
            pending.ExpectsAnswer.ShouldBeFalse();
        }

        // ---- Send & mark done ---------------------------------------------------------------------

        [Fact]
        public async Task SendAndComplete_FromAccepted_SavesTheReplyAndCompletesInOneSave()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            ask.ExpectsAnswer = true;

            var dto = await Complete(ask, "  SELECT 1;  ");

            _bed.SaveCount.ShouldBe(1);
            _bed.Replies.ShouldHaveSingleItem().Body.ShouldBe("SELECT 1;");
            ask.State.ShouldBe(BoardItemAskState.Completed);
            ask.Revision.ShouldBe(4);
            dto.IsAnswered.ShouldBeTrue();

            // One notification: Completed, carrying the answer; no separate Replied.
            var sent = _bed.Sent.ShouldHaveSingleItem();
            sent.UserId.ShouldBe(Requester);
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Completed));
            sent.Payload.ReplyId.ShouldBe(_bed.Replies.Single().Id);
            sent.Payload.ReplyExcerpt.ShouldBe("SELECT 1;");
        }

        [Fact]
        public async Task SendAndComplete_FromPending_AcceptsAndCompletesInTheSameSave()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Complete(ask, "Here it is.");

            _bed.SaveCount.ShouldBe(1);
            ask.State.ShouldBe(BoardItemAskState.Completed);
            ask.RespondedAt.ShouldNotBeNull();
            ask.CompletedAt.ShouldNotBeNull();
            ask.Revision.ShouldBe(4);
            _bed.Logged.Select(l => l.ActionType).ShouldBe(new[] { AskHistory.Accepted, AskHistory.Completed });
            _bed.Sent.ShouldHaveSingleItem().Payload.Event.ShouldBe(nameof(AskEvent.Completed));
        }

        [Fact]
        public async Task PlainComplete_FromPending_IsStillNotAllowed()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<BadRequestException>(() => Complete(ask));
            ask.State.ShouldBe(BoardItemAskState.Pending);
        }

        [Fact]
        public async Task SendAndComplete_WithAStaleRevision_SavesNothing()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<BadRequestException>(() => Complete(ask, "Here it is.", expectedRevision: ask.Revision - 1));

            ask.State.ShouldBe(BoardItemAskState.Pending);
            _bed.Replies.ShouldBeEmpty();
            _bed.PendingReplies.ShouldBeEmpty();
            _bed.SaveCount.ShouldBe(0);
            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task SendAndComplete_WithAnInvalidReply_SavesNothing()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);

            await Should.ThrowAsync<BadRequestException>(() => Complete(ask, "   "));
            await Should.ThrowAsync<BadRequestException>(() => Complete(ask, new string('x', BoardItemAskLimits.ReplyMaxLength + 1)));

            ask.State.ShouldBe(BoardItemAskState.Accepted);
            _bed.Replies.ShouldBeEmpty();
            _bed.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task SendAndComplete_WhenTheSaveConflicts_KeepsNoReply()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            _bed.FailNextSave = true;

            await Should.ThrowAsync<BadRequestException>(() => Complete(ask, "Here it is."));

            _bed.Replies.ShouldBeEmpty();
            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task SendAndComplete_ByTheRequester_IsForbidden()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            _bed.CallerId = Requester;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Complete(ask, "Done it myself."));
            _bed.Replies.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(BoardItemAskAction.Accept)]
        [InlineData(BoardItemAskAction.Decline)]
        [InlineData(BoardItemAskAction.Reopen)]
        public async Task OnlyComplete_TakesAReply(BoardItemAskAction action)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<BadRequestException>(() => _bed.ChangeStateHandler().Handle(new ChangeAskStateCommand
            {
                AskId = ask.Id,
                ExpectedRevision = ask.Revision,
                Action = action,
                ReplyBody = "text"
            }, CancellationToken.None));
        }

        // ---- Loading ------------------------------------------------------------------------------

        [Fact]
        public async Task GetAsksForItem_LoadsEveryAsksRepliesInOneQuery_OldestFirst()
        {
            var first = _bed.AddAsk(BoardItemAskState.Accepted);
            var second = _bed.AddAsk(BoardItemAskState.Pending, recipient: Other);
            _bed.AddReply(first, Recipient, "one");
            _bed.AddReply(second, Requester, "question");
            _bed.AddReply(first, Requester, "two");
            _bed.CallerId = Requester;

            var dtos = await _bed.GetAsksForItemHandler().Handle(new GetAsksForItemQuery(ItemId), CancellationToken.None);

            _bed.ReplyQueryCount.ShouldBe(1);
            var firstDto = dtos.Single(d => d.Id == first.Id);
            firstDto.Replies.Select(r => r.Body).ShouldBe(new[] { "one", "two" });
            firstDto.Replies.Select(r => r.IsMine).ShouldBe(new[] { false, true });
            firstDto.Replies[0].AuthorFullName.ShouldBe(FullName(Recipient));
            firstDto.IsAnswered.ShouldBeTrue();
            dtos.Single(d => d.Id == second.Id).Replies.Select(r => r.Body).ShouldBe(new[] { "question" });
        }

        [Fact]
        public async Task GetMyAsks_LoadsRepliesInOneQuery()
        {
            var first = _bed.AddAsk(BoardItemAskState.Pending);
            var second = _bed.AddAsk(BoardItemAskState.Accepted);
            _bed.AddReply(first, Recipient);
            _bed.AddReply(second, Recipient);

            var dtos = await _bed.GetMyAsksHandler().Handle(new GetMyAsksQuery { Direction = AskDirection.Received }, CancellationToken.None);

            _bed.ReplyQueryCount.ShouldBe(1);
            dtos.ShouldAllBe(d => d.Replies.Count == 1);
        }

        [Theory]
        [InlineData(Requester, true)]
        [InlineData(Recipient, true)]
        [InlineData(Assignee, false)]
        [InlineData(Owner, false)]
        [InlineData(Other, false)]
        public async Task CanReply_IsForTheTwoParticipantsOnly(string viewer, bool canReply)
        {
            _bed.AddAsk(BoardItemAskState.Pending);
            var cancelled = _bed.AddAsk(BoardItemAskState.Cancelled);
            _bed.CallerId = viewer;

            var dtos = await _bed.GetAsksForItemHandler().Handle(new GetAsksForItemQuery(ItemId), CancellationToken.None);

            dtos.Single(d => d.Id != cancelled.Id).CanReply.ShouldBe(canReply);
            dtos.Single(d => d.Id == cancelled.Id).CanReply.ShouldBeFalse();
        }

        [Fact]
        public async Task CreateAsk_CanExpectAnAnswer_AndReassignKeepsIt()
        {
            _bed.CallerId = Requester;

            var created = await _bed.CreateHandler().Handle(new CreateAskCommand
            {
                BoardItemId = ItemId,
                RecipientUserId = Recipient,
                Note = "Share the seed SQL",
                ExpectsAnswer = true
            }, CancellationToken.None);

            created.ExpectsAnswer.ShouldBeTrue();
            created.AllowedActions.ShouldContain(BoardItemAskAction.ClearExpectsAnswer);
            _bed.LoggedOf(AskHistory.Created).Single().Metadata[AskHistory.ExpectsAnswerKey].ShouldBe(true);

            var reassigned = await _bed.ReassignHandler().Handle(new ReassignAskCommand
            {
                AskId = created.Id,
                ExpectedRevision = created.Revision,
                NewRecipientUserId = Other
            }, CancellationToken.None);

            reassigned.ExpectsAnswer.ShouldBeTrue();
        }
    }
}
