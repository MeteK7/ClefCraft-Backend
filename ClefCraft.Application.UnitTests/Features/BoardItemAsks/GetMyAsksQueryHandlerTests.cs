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
        public async Task InvalidDirection_IsABadRequest()
        {
            await Should.ThrowAsync<BadRequestException>(() => Get((AskDirection)7));
        }
    }
}
