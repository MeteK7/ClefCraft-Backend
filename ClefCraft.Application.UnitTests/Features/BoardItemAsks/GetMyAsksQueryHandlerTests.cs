using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks;
using ClefCraft.Domain.Enums;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class GetMyAsksQueryHandlerTests
    {
        private readonly AskTestBed _bed = new();

        private Task<List<BoardItemAskDto>> Get(AskDirection direction) =>
            _bed.GetMyAsksHandler().Handle(new GetMyAsksQuery { Direction = direction }, CancellationToken.None);

        [Fact]
        public async Task Received_IsTheOpenAsksAddressedToTheCaller_PendingFirst()
        {
            var accepted = _bed.AddAsk(BoardItemAskState.Accepted);
            var pending = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.AddAsk(BoardItemAskState.Declined);
            _bed.AddAsk(BoardItemAskState.Completed);
            _bed.AddAsk(BoardItemAskState.Pending, recipient: Other);
            _bed.CallerId = Recipient;

            var result = await Get(AskDirection.Received);

            result.Select(d => d.Id).ShouldBe(new[] { pending.Id, accepted.Id });
            result.First().AllowedActions.ShouldContain(BoardItemAskAction.Accept);
        }

        [Fact]
        public async Task Sent_IsTheCallersOpenAsks_PlusThoseDeclinedInTheLast14Days()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending);
            var accepted = _bed.AddAsk(BoardItemAskState.Accepted, recipient: Other);
            var declinedRecently = _bed.AddAsk(BoardItemAskState.Declined);
            declinedRecently.RespondedAt = DateTime.UtcNow.AddDays(-13);
            var declinedLongAgo = _bed.AddAsk(BoardItemAskState.Declined);
            declinedLongAgo.RespondedAt = DateTime.UtcNow.AddDays(-15);
            _bed.AddAsk(BoardItemAskState.Completed);
            _bed.AddAsk(BoardItemAskState.Cancelled);
            _bed.AddAsk(BoardItemAskState.Pending, requester: Assignee, recipient: Other);
            _bed.CallerId = Requester;

            var result = await Get(AskDirection.Sent);

            result.Select(d => d.Id).ShouldBe(new[] { pending.Id, accepted.Id, declinedRecently.Id });
        }

        [Fact]
        public async Task BothDirections_LeaveOutBoardsTheCallerIsNoLongerOn()
        {
            var otherItem = _bed.AddItemOnOtherBoard();
            _bed.AddAsk(BoardItemAskState.Pending, item: otherItem);
            var here = _bed.AddAsk(BoardItemAskState.Pending);

            _bed.CallerId = Recipient;
            (await Get(AskDirection.Received)).Select(d => d.Id).ShouldBe(new[] { here.Id });

            _bed.CallerId = Requester;
            (await Get(AskDirection.Sent)).Select(d => d.Id).ShouldBe(new[] { here.Id });
        }

        [Fact]
        public async Task TheCallerIsAlwaysTheAuthenticatedUser()
        {
            _bed.AddAsk(BoardItemAskState.Pending, recipient: Recipient);
            _bed.CallerId = Other;

            (await Get(AskDirection.Received)).ShouldBeEmpty();
            (await Get(AskDirection.Sent)).ShouldBeEmpty();
        }

        [Fact]
        public async Task Sent_ShowsWhetherEachRecipientIsStillAMember()
        {
            _bed.AddAsk(BoardItemAskState.Pending, recipient: Other);
            _bed.Members.Remove((BoardId, Other));

            var dto = (await Get(AskDirection.Sent)).Single();

            dto.RecipientIsBoardMember.ShouldBeFalse();
            dto.BoardItemTitle.ShouldBe("Release notes");
            dto.BoardId.ShouldBe(BoardId);
        }

        [Fact]
        public async Task NextNudgeAt_IsForTheRequester_NeverForTheRecipient()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            ask.LastNudgedAt = DateTime.UtcNow.AddHours(-2);

            _bed.CallerId = Recipient;
            var received = (await Get(AskDirection.Received)).Single();
            received.NextNudgeAt.ShouldBeNull();
            received.AllowedActions.ShouldNotContain(BoardItemAskAction.Nudge);

            _bed.CallerId = Requester;
            var sent = (await Get(AskDirection.Sent)).Single();
            sent.NextNudgeAt.ShouldBe(ask.LastNudgedAt.Value.AddHours(24));
            sent.AllowedActions.ShouldNotContain(BoardItemAskAction.Nudge);
        }

        [Fact]
        public async Task InvalidDirection_IsABadRequest()
        {
            await Should.ThrowAsync<BadRequestException>(() => Get((AskDirection)7));
        }

        // ---- Waiting for you (AskTurn) ------------------------------------------------------------

        private Task<int> Count(AskDirection direction) =>
            _bed.GetMyAskCountHandler().Handle(new GetMyAskCountQuery { Direction = direction }, CancellationToken.None);

        [Fact]
        public async Task Waiting_IsTheCallersMoves_AsRecipientAndAsRequester_GroupedByBoard()
        {
            // The caller, Other, is on both boards.
            var otherItem = _bed.AddItemOnOtherBoard();
            _bed.Members.Add((OtherBoardId, Other));

            var deliver = _bed.AddAsk(BoardItemAskState.Accepted, recipient: Other);
            var respond = _bed.AddAsk(BoardItemAskState.Pending, recipient: Other);
            var resolve = _bed.AddAsk(BoardItemAskState.Declined, requester: Other, recipient: Recipient, isRequired: true);
            var elsewhere = _bed.AddAsk(BoardItemAskState.Pending, recipient: Other, item: otherItem);

            // Not the caller's move: news, someone else's turn, or closed.
            _bed.AddAsk(BoardItemAskState.Declined, requester: Other, recipient: Recipient);
            _bed.AddAsk(BoardItemAskState.Pending, requester: Other, recipient: Recipient);
            _bed.AddAsk(BoardItemAskState.Completed, recipient: Other);
            _bed.AddAsk(BoardItemAskState.Pending, recipient: Recipient);
            _bed.CallerId = Other;

            var result = await Get(AskDirection.Waiting);

            // "Other" before "Release"; within a board, decisions, then work, then requirements.
            result.Select(d => d.Id).ShouldBe(new[] { elsewhere.Id, respond.Id, deliver.Id, resolve.Id });
            result.Select(d => d.YourMove).ShouldBe(new AskTurnReason?[]
            {
                AskTurnReason.Respond, AskTurnReason.Respond, AskTurnReason.Deliver, AskTurnReason.ResolveRequirement
            });
            result.Select(d => d.BoardTitle).ShouldBe(new[] { "Other", "Release", "Release", "Release" });

            (await Count(AskDirection.Waiting)).ShouldBe(result.Count);
        }

        [Fact]
        public async Task Waiting_LeavesOutDoneItemsAndBoardsTheCallerLeft()
        {
            _bed.AddAsk(BoardItemAskState.Pending, recipient: Other, item: _bed.AddItemOnOtherBoard());
            _bed.AddAsk(BoardItemAskState.Pending, recipient: Other);
            _bed.CallerId = Other;

            (await Get(AskDirection.Waiting)).Count.ShouldBe(1);

            _bed.MarkItemDone();

            (await Get(AskDirection.Waiting)).ShouldBeEmpty();
            (await Count(AskDirection.Waiting)).ShouldBe(0);
        }

        [Fact]
        public async Task Count_UsesTheSameRulesAsTheLists()
        {
            _bed.AddAsk(BoardItemAskState.Pending);
            _bed.AddAsk(BoardItemAskState.Accepted);
            _bed.AddAsk(BoardItemAskState.Declined);

            _bed.CallerId = Recipient;
            (await Count(AskDirection.Received)).ShouldBe((await Get(AskDirection.Received)).Count);
            _bed.CallerId = Requester;
            (await Count(AskDirection.Sent)).ShouldBe((await Get(AskDirection.Sent)).Count);
        }

        [Fact]
        public async Task Count_InvalidDirection_IsABadRequest()
        {
            await Should.ThrowAsync<BadRequestException>(() => Count((AskDirection)7));
        }
    }
}
