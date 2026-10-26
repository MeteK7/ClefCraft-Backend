using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.ReassignAsk;
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
    public class ReassignAskCommandHandlerTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Assignee };

        private Task<BoardItemAskDto> Reassign(BoardItemAsk ask, string newRecipient = Other, int? expectedRevision = null, string? note = null) =>
            _bed.ReassignHandler().Handle(new ReassignAskCommand
            {
                AskId = ask.Id,
                ExpectedRevision = expectedRevision ?? ask.Revision,
                NewRecipientUserId = newRecipient,
                Note = note
            }, CancellationToken.None);

        [Fact]
        public async Task Reassign_CancelsTheOldAsk_AndAsksTheNewPerson_InOneSave()
        {
            var old = _bed.AddAsk(BoardItemAskState.Accepted, isRequired: true);
            _bed.Item.AskVersion = 5;

            var dto = await Reassign(old);

            old.State.ShouldBe(BoardItemAskState.Cancelled);
            old.CancelReason.ShouldBe(BoardItemAskCancelReason.Withdrawn);
            old.CancelledAt.ShouldNotBeNull();
            old.CancelledByUserId.ShouldBe(_bed.CallerId);
            old.IsRequired.ShouldBeFalse();

            var replacement = _bed.Asks.Single(a => a.Id == dto.Id);
            replacement.State.ShouldBe(BoardItemAskState.Pending);
            replacement.RecipientUserId.ShouldBe(Other);
            replacement.RequestedByUserId.ShouldBe(Requester);
            replacement.IsRequired.ShouldBeTrue();
            replacement.Note.ShouldBe(old.Note);

            _bed.Item.AskVersion.ShouldBe(6);
            _bed.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Reassign_LogsTheCancellationTheNewAskAndTheMovedRequirement()
        {
            var old = _bed.AddAsk(BoardItemAskState.Pending, isRequired: true);

            await Reassign(old);

            _bed.LoggedOf(AskHistory.Cancelled).Single().Metadata[AskHistory.ToRecipientUserIdKey].ShouldBe(Other);
            _bed.LoggedOf(AskHistory.Created).Single().Metadata[AskHistory.RecipientUserIdKey].ShouldBe(Other);

            var moved = _bed.LoggedOf(AskHistory.RequirementMoved).Single();
            moved.Metadata[AskHistory.AskIdKey].ShouldBe(old.Id);
            moved.Metadata[AskHistory.ToRecipientUserIdKey].ShouldBe(Other);
        }

        [Fact]
        public async Task Reassign_OptionalAsk_MovesNoRequirement()
        {
            var old = _bed.AddAsk(BoardItemAskState.Pending);

            await Reassign(old);

            _bed.Asks.Single(a => a.Id != old.Id).IsRequired.ShouldBeFalse();
            _bed.LoggedOf(AskHistory.RequirementMoved).ShouldBeEmpty();
        }

        [Fact]
        public async Task Reassign_TellsTheOldRecipientItWasCancelled_AndTheNewOneTheyAreAsked_Only()
        {
            var old = _bed.AddAsk(BoardItemAskState.Accepted);

            await Reassign(old);

            _bed.Sent.Count.ShouldBe(2);
            _bed.SentTo(Recipient).Single().Payload.Event.ShouldBe(nameof(AskEvent.Cancelled));
            _bed.SentTo(Other).Single().Payload.Event.ShouldBe(nameof(AskEvent.Requested));
            _bed.SentTo(Requester).ShouldBeEmpty();
        }

        [Theory]
        [InlineData(Outsider)]
        [InlineData(Assignee)]
        [InlineData(Recipient)]
        public async Task Reassign_ToANonMemberTheCallerOrTheSamePerson_IsRejected(string newRecipient)
        {
            var old = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<BadRequestException>(() => Reassign(old, newRecipient));

            old.State.ShouldBe(BoardItemAskState.Pending);
            _bed.Asks.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Reassign_ToTheCurrentAssignee_IsRejected()
        {
            var old = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = Requester;

            await Should.ThrowAsync<BadRequestException>(() => Reassign(old, Assignee));
        }

        [Fact]
        public async Task Reassign_ByANonManager_IsForbidden()
        {
            var old = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = Recipient;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Reassign(old));
        }

        [Theory]
        [InlineData(BoardItemAskState.Declined)]
        [InlineData(BoardItemAskState.Completed)]
        public async Task Reassign_AClosedAsk_IsRejected(BoardItemAskState state)
        {
            var old = _bed.AddAsk(state);

            await Should.ThrowAsync<BadRequestException>(() => Reassign(old));
        }

        [Fact]
        public async Task Reassign_WithAStaleRevision_IsRejected()
        {
            var old = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<BadRequestException>(() => Reassign(old, expectedRevision: old.Revision + 1));
            old.State.ShouldBe(BoardItemAskState.Pending);
        }

        [Fact]
        public async Task Reassign_CanChangeTheNote()
        {
            var old = _bed.AddAsk(BoardItemAskState.Pending);

            var dto = await Reassign(old, note: "Review the API only");

            dto.Note.ShouldBe("Review the API only");
        }
    }
}
