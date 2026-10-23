using ClefCraft.Application.Contracts.Analytics;
using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItem.Commands.UpdateBoardItem;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    // Completing an item settles its asks (plan §3.4, with the explicit override list). PUT
    // /BoardItems/{id} and SwitchColumn both reach this handler, so this is the one place it happens.
    public class UpdateBoardItemAskClosureTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Assignee };

        private Task Complete(AskClosureRequest? closure, Mock<ITaskLifecycleService>? lifecycle = null) =>
            _bed.UpdateItemHandler(lifecycle).Handle(new UpdateBoardItemCommand
            {
                Id = ItemId,
                BoardColumnId = _bed.Item.BoardColumnId,
                StatusId = DoneStatusId,
                AskClosure = closure
            }, CancellationToken.None);

        private static AskClosureRequest Closure(IEnumerable<BoardItemAsk> acknowledged, IEnumerable<BoardItemAsk>? complete = null, IEnumerable<BoardItemAsk>? overrides = null) => new()
        {
            AcknowledgedAsks = acknowledged.Select(a => new AcknowledgedAsk { AskId = a.Id, Revision = a.Revision }).ToList(),
            CompleteAskIds = (complete ?? Enumerable.Empty<BoardItemAsk>()).Select(a => a.Id).ToList(),
            OverrideAskIds = (overrides ?? Enumerable.Empty<BoardItemAsk>()).Select(a => a.Id).ToList()
        };

        [Fact]
        public async Task OpenAsksWithoutAClosure_AreRejected_BeforeTheItemIsTouched()
        {
            _bed.AddAsk(BoardItemAskState.Pending);

            var ex = await Should.ThrowAsync<BadRequestException>(() => Complete(null));

            ex.Message.ShouldBe(AskClosure.MissingMessage);
            _bed.Item.BoardItemStatus.StatusId.ShouldBe(InProgressStatusId);
            _bed.SaveCount.ShouldBe(0);
            _bed.BoardItemRepository.Verify(r => r.UpdateBoardItem(It.IsAny<Domain.BoardItem>()), Times.Never);
        }

        [Fact]
        public async Task MarkingAnAskDoneForItsRecipient_TakesTheirLatestReplyAsTheAnswer()
        {
            var answered = _bed.AddAsk(BoardItemAskState.Accepted);
            var silent = _bed.AddAsk(BoardItemAskState.Accepted);
            _bed.AddReply(answered, Recipient, "older", at: System.DateTime.UtcNow.AddHours(-2));
            var latest = _bed.AddReply(answered, Recipient, "latest", at: System.DateTime.UtcNow.AddHours(-1));
            _bed.AddReply(silent, Requester, "a question");

            await Complete(Closure(new[] { answered, silent }, complete: new[] { answered, silent }));

            answered.AnswerReplyId.ShouldBe(latest.Id);
            silent.AnswerReplyId.ShouldBeNull();
            _bed.ReplyQueryCount.ShouldBe(1);
        }

        [Fact]
        public async Task MarkingAnAskDoneForItsRecipient_CreditsThemButRecordsWhoDidIt()
        {
            var accepted = _bed.AddAsk(BoardItemAskState.Accepted);

            await Complete(Closure(new[] { accepted }, complete: new[] { accepted }));

            accepted.State.ShouldBe(BoardItemAskState.Completed);
            accepted.RecipientUserId.ShouldBe(Recipient);
            accepted.CompletedByUserId.ShouldBe(Assignee);

            var entry = _bed.LoggedOf(AskHistory.Completed).Single();
            entry.EntityType.ShouldBe("BoardItem");
            entry.EntityId.ShouldBe(ItemId);
            entry.ActorUserId.ShouldBe(Assignee);
            entry.Metadata[AskHistory.RecipientUserIdKey].ShouldBe(Recipient);
            entry.Metadata[AskHistory.CompletedByUserIdKey].ShouldBe(Assignee);

            var toRecipient = _bed.SentTo(Recipient).Single().Payload;
            toRecipient.Event.ShouldBe(nameof(AskEvent.Completed));
            toRecipient.ActedOnBehalf.ShouldBeTrue();
            toRecipient.ActorFullName.ShouldBe(FullName(Assignee));
            _bed.SentTo(Requester).Single().Payload.ActedOnBehalf.ShouldBeTrue();
            _bed.SentTo(Assignee).ShouldBeEmpty();
        }

        [Fact]
        public async Task TheOtherOpenAsks_AreCancelledAsItemCompleted_AndTheirPeopleTold()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending, recipient: Other);
            var accepted = _bed.AddAsk(BoardItemAskState.Accepted);

            await Complete(Closure(new[] { pending, accepted }));

            pending.State.ShouldBe(BoardItemAskState.Cancelled);
            pending.CancelReason.ShouldBe(BoardItemAskCancelReason.ItemCompleted);
            accepted.State.ShouldBe(BoardItemAskState.Cancelled);

            _bed.LoggedOf(AskHistory.Cancelled).Count().ShouldBe(2);
            _bed.LoggedOf(AskHistory.Cancelled).ShouldAllBe(l => (string)l.Metadata[AskHistory.ReasonKey] == "ItemCompleted");

            _bed.SentTo(Other).Single().Payload.Reason.ShouldBe("ItemCompleted");
            _bed.SentTo(Recipient).Single().Payload.Event.ShouldBe(nameof(AskEvent.Cancelled));
            _bed.SentTo(Requester).Count().ShouldBe(2);
        }

        [Fact]
        public async Task Override_RecordsExactlyTheOverriddenAsks_AndWhoOverrodeThem()
        {
            var declinedRequired = _bed.AddAsk(BoardItemAskState.Declined, isRequired: true);
            var pendingRequired = _bed.AddAsk(BoardItemAskState.Pending, recipient: Other, isRequired: true);

            await Complete(Closure(new[] { declinedRequired, pendingRequired }, overrides: new[] { declinedRequired, pendingRequired }));

            var entry = _bed.LoggedOf(AskHistory.RequirementsOverridden).Single();
            entry.EntityId.ShouldBe(ItemId);
            entry.ActorUserId.ShouldBe(Assignee);
            ((int[])entry.Metadata[AskHistory.AskIdsKey]).ShouldBe(new[] { declinedRequired.Id, pendingRequired.Id });

            declinedRequired.State.ShouldBe(BoardItemAskState.Declined);
            pendingRequired.State.ShouldBe(BoardItemAskState.Cancelled);
            _bed.Item.BoardItemStatus.StatusId.ShouldBe(DoneStatusId);
        }

        [Fact]
        public async Task UnmetRequirementWithoutOverride_IsRejected_AndNothingIsSaved()
        {
            var required = _bed.AddAsk(BoardItemAskState.Cancelled, isRequired: true);
            required.CancelReason = BoardItemAskCancelReason.RecipientRemoved;

            await Should.ThrowAsync<BadRequestException>(() => Complete(Closure(new[] { required })));

            _bed.SaveCount.ShouldBe(0);
            _bed.Logged.ShouldBeEmpty();
        }

        [Fact]
        public async Task ReopenedItem_WithAnEarlierOverriddenRequirement_NeedsOverridingAgain()
        {
            var required = _bed.AddAsk(BoardItemAskState.Pending, isRequired: true);
            await Complete(Closure(new[] { required }, overrides: new[] { required }));
            required.State.ShouldBe(BoardItemAskState.Cancelled);

            // Reopen the item, then try to complete it again without an override.
            _bed.Item.BoardItemStatus.StatusId = InProgressStatusId;

            await Should.ThrowAsync<BadRequestException>(() => Complete(Closure(new[] { required })));
            await Complete(Closure(new[] { required }, overrides: new[] { required }));

            _bed.LoggedOf(AskHistory.RequirementsOverridden).Count().ShouldBe(2);
        }

        [Fact]
        public async Task MarkingDoneForSomeoneWithoutManagingTheirAsk_IsForbidden()
        {
            var accepted = _bed.AddAsk(BoardItemAskState.Accepted);
            _bed.CallerId = Other;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Complete(Closure(new[] { accepted }, complete: new[] { accepted })));

            accepted.State.ShouldBe(BoardItemAskState.Accepted);
            _bed.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task EnteringDone_AlwaysIncrementsAskVersion_EvenWithoutAsks()
        {
            _bed.Item.AskVersion = 4;

            await Complete(null);

            _bed.Item.AskVersion.ShouldBe(5);
            _bed.Logged.ShouldBeEmpty();
            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task SavingAnItemAlreadyDone_DoesNotTouchItsAsks()
        {
            _bed.MarkItemDone();
            _bed.AddAsk(BoardItemAskState.Declined, isRequired: true);
            _bed.Item.AskVersion = 4;

            await Complete(null);

            _bed.Item.AskVersion.ShouldBe(4);
            _bed.AskRepository.Verify(r => r.GetByItemIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AnEditThatDoesntCompleteTheItem_DoesNotTouchItsAsks()
        {
            _bed.AddAsk(BoardItemAskState.Pending);
            _bed.Item.AskVersion = 4;

            await _bed.UpdateItemHandler().Handle(new UpdateBoardItemCommand
            {
                Id = ItemId,
                BoardColumnId = _bed.Item.BoardColumnId,
                Title = "Release notes v2"
            }, CancellationToken.None);

            _bed.Item.AskVersion.ShouldBe(4);
            _bed.AskRepository.Verify(r => r.GetByItemIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SaveConflict_IsABadRequest_AndNobodyIsNotified()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.FailNextSave = true;

            var ex = await Should.ThrowAsync<BadRequestException>(() => Complete(Closure(new[] { pending })));

            ex.Message.ShouldBe(UpdateBoardItemCommandHandler.ConflictMessage);
            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task CompletionIsStillRecordedInTheLifecycle()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending);
            var lifecycle = new Mock<ITaskLifecycleService>();

            await Complete(Closure(new[] { pending }), lifecycle);

            lifecycle.Verify(l => l.RecordCompletionAsync(ItemId), Times.Once);
        }
    }
}
