using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardMembers.Commands.RemoveMember;
using ClefCraft.Domain.Enums;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    // Which asks are "open asks of this member on this board" is the repository's query
    // (GetOpenByRecipientOnBoardAsync); here it is answered from the test bed's asks.
    public class RemoveBoardMemberAskTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Owner };

        private Task Remove(string userId) =>
            _bed.RemoveMemberHandler().Handle(
                new RemoveBoardMemberCommand { BoardId = BoardId, UserId = userId, RequestingUserId = Owner },
                CancellationToken.None);

        [Fact]
        public async Task RemovingARecipient_CancelsTheirOpenAsks_InTheSameSaveAsTheRemoval()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending);
            var accepted = _bed.AddAsk(BoardItemAskState.Accepted);
            var completed = _bed.AddAsk(BoardItemAskState.Completed);
            var declined = _bed.AddAsk(BoardItemAskState.Declined);
            var someoneElses = _bed.AddAsk(BoardItemAskState.Pending, recipient: Other);

            await Remove(Recipient);

            pending.State.ShouldBe(BoardItemAskState.Cancelled);
            pending.CancelReason.ShouldBe(BoardItemAskCancelReason.RecipientRemoved);
            pending.CancelledAt.ShouldNotBeNull();
            pending.CancelledByUserId.ShouldBe(Owner);
            accepted.State.ShouldBe(BoardItemAskState.Cancelled);
            accepted.CancelledByUserId.ShouldBe(Owner);
            completed.State.ShouldBe(BoardItemAskState.Completed);
            completed.CancelledAt.ShouldBeNull();
            declined.State.ShouldBe(BoardItemAskState.Declined);
            someoneElses.State.ShouldBe(BoardItemAskState.Pending);

            _bed.SaveCount.ShouldBe(1);
            _bed.Members.ShouldNotContain((BoardId, Recipient));
            _bed.LoggedOf(AskHistory.Cancelled).Count().ShouldBe(2);
            _bed.LoggedOf(AskHistory.Cancelled).ShouldAllBe(l => (string)l.Metadata[AskHistory.ReasonKey] == "RecipientRemoved");
        }

        [Fact]
        public async Task RemovingARecipient_LeavesTheirRequiredAskAnUnmetRequirement()
        {
            var required = _bed.AddAsk(BoardItemAskState.Accepted, isRequired: true);

            await Remove(Recipient);

            required.IsRequired.ShouldBeTrue();
            AskTransitions.IsUnmetRequirement(required).ShouldBeTrue();
        }

        [Fact]
        public async Task OnlyTheRequesterIsTold_NotTheRemovedMember()
        {
            _bed.AddAsk(BoardItemAskState.Pending);

            await Remove(Recipient);

            var sent = _bed.Sent.ShouldHaveSingleItem();
            sent.UserId.ShouldBe(Requester);
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Cancelled));
            sent.Payload.Reason.ShouldBe("RecipientRemoved");
        }

        [Fact]
        public async Task TheNoticesOfOneChange_CarryOneTimeOfOccurrence()
        {
            _bed.AddAsk(BoardItemAskState.Pending);
            _bed.AddAsk(BoardItemAskState.Accepted);
            var before = DateTime.UtcNow;

            await Remove(Recipient);

            _bed.Sent.Count.ShouldBe(2);
            _bed.Sent.Select(s => s.Payload.OccurredAt).Distinct().ShouldHaveSingleItem()
                .ShouldBeInRange(before, DateTime.UtcNow);
        }

        [Fact]
        public async Task RemovingTheRequester_LeavesTheirAsksOpen()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending);

            await Remove(Requester);

            pending.State.ShouldBe(BoardItemAskState.Pending);
            _bed.AskRepository.Verify(r => r.GetOpenByRecipientOnBoardAsync(BoardId, Requester), Times.Once);
        }

        [Fact]
        public async Task SaveConflict_IsABadRequest_AndNobodyIsNotified()
        {
            _bed.AddAsk(BoardItemAskState.Pending);
            _bed.FailNextSave = true;

            await Should.ThrowAsync<BadRequestException>(() => Remove(Recipient));

            _bed.Sent.ShouldBeEmpty();
        }
    }
}
