using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.NudgeAsk;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class NudgeAskCommandHandlerTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Requester };

        private Task<BoardItemAskDto> Nudge(BoardItemAsk ask) =>
            _bed.NudgeHandler().Handle(new NudgeAskCommand { AskId = ask.Id }, CancellationToken.None);

        [Fact]
        public async Task Nudge_RemindsOnlyTheRecipient_AndChangesNoStateRevisionOrAge()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            var asked = ask.DateCreated;
            _bed.CallerId = Assignee;

            var dto = await Nudge(ask);

            ask.LastNudgedAt.ShouldNotBeNull();
            ask.State.ShouldBe(BoardItemAskState.Pending);
            ask.Revision.ShouldBe(3);
            ask.DateCreated.ShouldBe(asked);
            dto.AllowedActions.ShouldNotContain(BoardItemAskAction.Nudge);
            dto.NextNudgeAt.ShouldBe(ask.LastNudgedAt!.Value.AddHours(24));
            _bed.LoggedOf(AskHistory.Nudged).ShouldHaveSingleItem();

            var sent = _bed.Sent.ShouldHaveSingleItem();
            sent.UserId.ShouldBe(Recipient);
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Nudged));
        }

        // The frontend recognises this rejection by its exact text, so the text is part of the contract.
        [Fact]
        public void AlreadyNudgedMessage_IsTheTextTheFrontendMatches()
        {
            NudgeAskCommandHandler.AlreadyNudgedMessage.ShouldBe("This ask was already nudged in the last 24 hours.");
        }

        [Fact]
        public async Task SecondNudgeWithin24Hours_IsRejected()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            ask.LastNudgedAt = DateTime.UtcNow.AddHours(-23);

            var ex = await Should.ThrowAsync<BadRequestException>(() => Nudge(ask));

            ex.Message.ShouldBe(NudgeAskCommandHandler.AlreadyNudgedMessage);
            _bed.SaveCount.ShouldBe(0);
            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task NudgeAfter24Hours_IsAllowed()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            var previous = DateTime.UtcNow.AddHours(-25);
            ask.LastNudgedAt = previous;

            await Nudge(ask);

            ask.LastNudgedAt!.Value.ShouldBeGreaterThan(previous);
        }

        [Fact]
        public async Task Nudge_ByTheRecipient_IsForbidden()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = Recipient;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Nudge(ask));
        }

        [Fact]
        public async Task Nudge_ByAMemberWhoIsntAManager_IsForbidden()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = Other;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Nudge(ask));
        }

        [Theory]
        [InlineData(BoardItemAskState.Accepted)]
        [InlineData(BoardItemAskState.Declined)]
        public async Task Nudge_OnAnAskThatIsntPending_IsRejected(BoardItemAskState state)
        {
            var ask = _bed.AddAsk(state);

            await Should.ThrowAsync<BadRequestException>(() => Nudge(ask));
        }

        [Fact]
        public async Task ConcurrentNudge_ThatLosesTheSave_IsRejected_AndNotifiesNobody()
        {
            // Two nudges read the same LastNudgedAt; the token lets only one commit.
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.FailNextSave = true;

            var ex = await Should.ThrowAsync<BadRequestException>(() => Nudge(ask));

            ex.Message.ShouldBe(NudgeAskCommandHandler.ConflictMessage);
            _bed.Sent.ShouldBeEmpty();
        }
    }
}
