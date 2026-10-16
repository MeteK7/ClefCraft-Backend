using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ChangeAskState;
using ClefCraft.Application.Features.BoardItemAsks.Commands.NudgeAsk;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Shouldly;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class ChangeAskStateCommandHandlerTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Recipient };

        private Task<BoardItemAskDto> Change(BoardItemAsk ask, BoardItemAskAction action, string? reason = null, int? expectedRevision = null) =>
            _bed.ChangeStateHandler().Handle(new ChangeAskStateCommand
            {
                AskId = ask.Id,
                ExpectedRevision = expectedRevision ?? ask.Revision,
                Action = action,
                Reason = reason
            }, CancellationToken.None);

        [Fact]
        public async Task Accept_ByTheRecipient_AcceptsAndTellsTheRequester()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Change(ask, BoardItemAskAction.Accept);

            ask.State.ShouldBe(BoardItemAskState.Accepted);
            ask.RespondedAt.ShouldNotBeNull();
            ask.Revision.ShouldBe(4);
            _bed.LoggedOf(AskHistory.Accepted).ShouldHaveSingleItem();

            var sent = _bed.Sent.ShouldHaveSingleItem();
            sent.UserId.ShouldBe(Requester);
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Accepted));
            sent.Payload.ActorFullName.ShouldBe(FullName(Recipient));
        }

        [Theory]
        [InlineData(Requester)]
        [InlineData(Assignee)]
        [InlineData(Other)]
        public async Task Accept_BySomeoneOtherThanTheRecipient_IsForbidden(string caller)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = caller;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Change(ask, BoardItemAskAction.Accept));
            ask.State.ShouldBe(BoardItemAskState.Pending);
        }

        [Fact]
        public async Task Decline_RecordsTheReason_AndTellsTheRequesterWhy()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);

            await Change(ask, BoardItemAskAction.Decline, reason: "  Out next week  ");

            ask.State.ShouldBe(BoardItemAskState.Declined);
            ask.ResponseNote.ShouldBe("Out next week");
            _bed.LoggedOf(AskHistory.Declined).Single().Metadata[AskHistory.ReasonKey].ShouldBe("Out next week");

            var sent = _bed.SentTo(Requester).ShouldHaveSingleItem();
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Declined));
            sent.Payload.Reason.ShouldBe("Out next week");
        }

        [Fact]
        public async Task Complete_ByTheRecipient_IsCreditedToThem_AndTellsTheRequester()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);

            await Change(ask, BoardItemAskAction.Complete);

            ask.State.ShouldBe(BoardItemAskState.Completed);
            ask.CompletedByUserId.ShouldBe(Recipient);
            _bed.LoggedOf(AskHistory.Completed).Single().Metadata[AskHistory.CompletedByUserIdKey].ShouldBe(Recipient);

            var sent = _bed.SentTo(Requester).ShouldHaveSingleItem();
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Completed));
            sent.Payload.ActedOnBehalf.ShouldBeFalse();
        }

        [Fact]
        public async Task Complete_FromPending_IsRejected()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<BadRequestException>(() => Change(ask, BoardItemAskAction.Complete));
        }

        [Fact]
        public async Task Reopen_ReopensTheAsk_AndIncrementsTheItemsAskVersion()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Completed);
            _bed.Item.AskVersion = 2;

            await Change(ask, BoardItemAskAction.Reopen);

            ask.State.ShouldBe(BoardItemAskState.Accepted);
            ask.CompletedAt.ShouldBeNull();
            ask.CompletedByUserId.ShouldBeNull();
            _bed.Item.AskVersion.ShouldBe(3);
            _bed.SentTo(Requester).ShouldHaveSingleItem().Payload.Event.ShouldBe(nameof(AskEvent.Reopened));
        }

        [Fact]
        public async Task Cancel_ByTheAssignee_TellsTheRecipientAndTheRequester_NotTheAssignee()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            _bed.CallerId = Assignee;

            await Change(ask, BoardItemAskAction.Cancel);

            ask.State.ShouldBe(BoardItemAskState.Cancelled);
            ask.CancelReason.ShouldBe(BoardItemAskCancelReason.Withdrawn);
            _bed.Sent.Select(s => s.UserId).OrderBy(id => id).ShouldBe(new[] { Recipient, Requester });
            _bed.Sent.ShouldAllBe(s => s.Payload.Event == nameof(AskEvent.Cancelled) && s.Payload.Reason == "Withdrawn");
        }

        [Fact]
        public async Task Cancel_ByTheRecipient_IsForbidden()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<ForbiddenAccessException>(() => Change(ask, BoardItemAskAction.Cancel));
        }

        [Fact]
        public async Task Answer_AfterTheTermsChanged_IsRejectedAsStale()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            var seenRevision = ask.Revision;
            ask.Revision++; // a manager edited the note meanwhile

            var ex = await Should.ThrowAsync<BadRequestException>(() => Change(ask, BoardItemAskAction.Accept, expectedRevision: seenRevision));

            ex.Message.ShouldContain("changed since you loaded it");
            ask.State.ShouldBe(BoardItemAskState.Pending);
        }

        [Fact]
        public async Task Answer_AfterANudge_IsStillTheVersionTheRecipientSaw()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            var seenRevision = ask.Revision;

            _bed.CallerId = Requester;
            await _bed.NudgeHandler().Handle(new NudgeAskCommand { AskId = ask.Id }, CancellationToken.None);

            _bed.CallerId = Recipient;
            await Change(ask, BoardItemAskAction.Accept, expectedRevision: seenRevision);

            ask.State.ShouldBe(BoardItemAskState.Accepted);
        }

        [Fact]
        public async Task SaveConflict_IsABadRequest_AndNobodyIsNotified()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.FailNextSave = true;

            await Should.ThrowAsync<BadRequestException>(() => Change(ask, BoardItemAskAction.Accept));

            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task RequesterWhoLeftTheBoard_IsNotNotified()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.Members.Remove((BoardId, Requester));

            await Change(ask, BoardItemAskAction.Accept);

            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task AnyChange_OnADoneItem_IsRejected()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.MarkItemDone();

            await Should.ThrowAsync<BadRequestException>(() => Change(ask, BoardItemAskAction.Accept));
        }

        [Fact]
        public async Task RecipientWhoIsNoLongerAMember_IsForbidden()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.Members.Remove((BoardId, Recipient));

            await Should.ThrowAsync<ForbiddenAccessException>(() => Change(ask, BoardItemAskAction.Accept));
        }

        [Theory]
        [InlineData(BoardItemAskAction.Nudge)]
        [InlineData(BoardItemAskAction.Edit)]
        [InlineData(BoardItemAskAction.CompleteOnBehalf)]
        public async Task ActionsThatArentStateChanges_AreRejected(BoardItemAskAction action)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);

            await Should.ThrowAsync<BadRequestException>(() => Change(ask, action));
        }
    }
}
