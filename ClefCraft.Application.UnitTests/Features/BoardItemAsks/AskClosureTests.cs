using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class AskClosureTests
    {
        private const string Requester = "requester";
        private const string Recipient = "recipient";
        private const string Assignee = "assignee";
        private const string Owner = "owner";
        private const string Bystander = "bystander";

        private static readonly System.DateTime Now = new(2026, 10, 16, 12, 0, 0, System.DateTimeKind.Utc);

        private int _nextId = 1;

        private BoardItemAsk Ask(BoardItemAskState state, bool isRequired = false, string recipient = Recipient) => new()
        {
            Id = _nextId++,
            BoardItemId = 1,
            RequestedByUserId = Requester,
            RecipientUserId = recipient,
            Note = "Review",
            State = state,
            IsRequired = isRequired,
            Revision = 1
        };

        private static AskClosureResult Apply(IReadOnlyCollection<BoardItemAsk> asks, AskClosureRequest? request, string caller = Assignee) =>
            AskClosure.Apply(asks, request, ask => AskActorRoles.For(ask, caller, Assignee, Owner), caller, Now);

        private static AskClosureRequest Request(IEnumerable<int> acknowledged, IEnumerable<int>? complete = null, IEnumerable<int>? overrides = null) => new()
        {
            AcknowledgedAskIds = acknowledged.ToList(),
            CompleteAskIds = (complete ?? new int[0]).ToList(),
            OverrideAskIds = (overrides ?? new int[0]).ToList()
        };

        private static void ShouldBeUnchanged(IEnumerable<BoardItemAsk> asks, IReadOnlyList<BoardItemAskState> states)
        {
            asks.Select(a => a.State).ShouldBe(states);
            asks.ShouldAllBe(a => a.Revision == 1);
        }

        [Fact]
        public void NoOpenAsksAndNoUnmetRequirements_NeedsNoClosure()
        {
            var asks = new[] { Ask(BoardItemAskState.Completed, isRequired: true), Ask(BoardItemAskState.Declined) };

            var result = Apply(asks, request: null);

            result.Completed.ShouldBeEmpty();
            result.Cancelled.ShouldBeEmpty();
            result.OverriddenAskIds.ShouldBeEmpty();
        }

        [Fact]
        public void OpenAsks_WithoutAClosure_AreRejected()
        {
            var asks = new[] { Ask(BoardItemAskState.Pending) };

            Should.Throw<BadRequestException>(() => Apply(asks, request: null)).Message.ShouldBe(AskClosure.MissingMessage);
        }

        [Fact]
        public void PendingAndUnselectedAcceptedAsks_AreCancelledAsItemCompleted()
        {
            var pending = Ask(BoardItemAskState.Pending);
            var accepted = Ask(BoardItemAskState.Accepted);

            var result = Apply(new[] { pending, accepted }, Request(new[] { pending.Id, accepted.Id }));

            result.Cancelled.ShouldBe(new[] { pending, accepted });
            result.Completed.ShouldBeEmpty();
            pending.State.ShouldBe(BoardItemAskState.Cancelled);
            pending.CancelReason.ShouldBe(BoardItemAskCancelReason.ItemCompleted);
            accepted.State.ShouldBe(BoardItemAskState.Cancelled);
            accepted.Revision.ShouldBe(2);
        }

        [Fact]
        public void SelectedAcceptedAsk_IsCompletedByTheCaller_CreditedToItsRecipient_AndNotAlsoCancelled()
        {
            var accepted = Ask(BoardItemAskState.Accepted);
            var other = Ask(BoardItemAskState.Accepted);

            var result = Apply(new[] { accepted, other }, Request(new[] { accepted.Id, other.Id }, complete: new[] { accepted.Id }));

            result.Completed.ShouldBe(new[] { accepted });
            result.Cancelled.ShouldBe(new[] { other });
            result.Completed.Intersect(result.Cancelled).ShouldBeEmpty();

            accepted.State.ShouldBe(BoardItemAskState.Completed);
            accepted.CompletedByUserId.ShouldBe(Assignee);
            accepted.RecipientUserId.ShouldBe(Recipient);
            accepted.CompletedAt.ShouldBe(Now);
            accepted.CancelReason.ShouldBeNull();
        }

        [Fact]
        public void RecipientClosingTheItem_CanMarkTheirOwnAskDone()
        {
            var accepted = Ask(BoardItemAskState.Accepted);

            var result = Apply(new[] { accepted }, Request(new[] { accepted.Id }, complete: new[] { accepted.Id }), caller: Recipient);

            result.Completed.Single().CompletedByUserId.ShouldBe(Recipient);
        }

        [Fact]
        public void MarkingSomeoneElsesAskDone_WithoutBeingItsManager_IsForbidden_AndChangesNothing()
        {
            var accepted = Ask(BoardItemAskState.Accepted);
            var pending = Ask(BoardItemAskState.Pending);

            Should.Throw<ForbiddenAccessException>(() =>
                Apply(new[] { accepted, pending }, Request(new[] { accepted.Id, pending.Id }, complete: new[] { accepted.Id }), caller: Bystander));

            ShouldBeUnchanged(new[] { accepted, pending }, new[] { BoardItemAskState.Accepted, BoardItemAskState.Pending });
        }

        [Fact]
        public void BoardOwner_MayMarkAnAcceptedAskDone()
        {
            var accepted = Ask(BoardItemAskState.Accepted);

            var result = Apply(new[] { accepted }, Request(new[] { accepted.Id }, complete: new[] { accepted.Id }), caller: Owner);

            result.Completed.Single().CompletedByUserId.ShouldBe(Owner);
        }

        [Theory]
        [InlineData("acknowledged")]
        [InlineData("complete")]
        [InlineData("override")]
        public void DuplicateIds_AreRejected(string list)
        {
            var accepted = Ask(BoardItemAskState.Accepted, isRequired: true);
            var request = list switch
            {
                "acknowledged" => Request(new[] { accepted.Id, accepted.Id }, complete: new[] { accepted.Id }),
                "complete" => Request(new[] { accepted.Id }, complete: new[] { accepted.Id, accepted.Id }),
                _ => Request(new[] { accepted.Id }, overrides: new[] { accepted.Id, accepted.Id })
            };

            Should.Throw<BadRequestException>(() => Apply(new[] { accepted }, request)).Message.ShouldBe(AskClosure.DuplicateMessage);
            accepted.State.ShouldBe(BoardItemAskState.Accepted);
        }

        [Fact]
        public void AcknowledgedSet_MissingAnAsk_IsRejectedAsChanged()
        {
            var seen = Ask(BoardItemAskState.Pending);
            var createdAfterThePromptOpened = Ask(BoardItemAskState.Pending);

            Should.Throw<BadRequestException>(() => Apply(new[] { seen, createdAfterThePromptOpened }, Request(new[] { seen.Id })))
                .Message.ShouldBe(AskClosure.ChangedMessage);

            ShouldBeUnchanged(new[] { seen, createdAfterThePromptOpened }, new[] { BoardItemAskState.Pending, BoardItemAskState.Pending });
        }

        [Fact]
        public void AcknowledgedSet_WithAnUnknownOrForeignId_IsRejectedAsChanged()
        {
            var pending = Ask(BoardItemAskState.Pending);

            Should.Throw<BadRequestException>(() => Apply(new[] { pending }, Request(new[] { pending.Id, 9999 })))
                .Message.ShouldBe(AskClosure.ChangedMessage);
        }

        [Fact]
        public void AcknowledgedSet_WithAnAskNoLongerOpen_IsRejectedAsChanged()
        {
            var pending = Ask(BoardItemAskState.Pending);
            var nowDeclinedOptional = Ask(BoardItemAskState.Declined);

            Should.Throw<BadRequestException>(() => Apply(new[] { pending, nowDeclinedOptional }, Request(new[] { pending.Id, nowDeclinedOptional.Id })))
                .Message.ShouldBe(AskClosure.ChangedMessage);
        }

        [Theory]
        [InlineData(BoardItemAskState.Pending)]
        [InlineData(BoardItemAskState.Declined)]
        public void CompletingAnAskThatIsntAccepted_IsRejected(BoardItemAskState state)
        {
            var ask = Ask(state, isRequired: true);

            Should.Throw<BadRequestException>(() =>
                Apply(new[] { ask }, Request(new[] { ask.Id }, complete: new[] { ask.Id }, overrides: new int[0])));

            ask.State.ShouldBe(state);
        }

        [Fact]
        public void UnmetRequirement_WithoutOverride_IsRejectedAsChanged()
        {
            var required = Ask(BoardItemAskState.Pending, isRequired: true);

            Should.Throw<BadRequestException>(() => Apply(new[] { required }, Request(new[] { required.Id })))
                .Message.ShouldBe(AskClosure.ChangedMessage);

            required.State.ShouldBe(BoardItemAskState.Pending);
        }

        [Fact]
        public void OverrideList_ShortOrWithExtraIds_IsRejected()
        {
            var first = Ask(BoardItemAskState.Pending, isRequired: true);
            var second = Ask(BoardItemAskState.Accepted, isRequired: true);
            var optional = Ask(BoardItemAskState.Pending);
            var all = new[] { first, second, optional };
            var acknowledged = all.Select(a => a.Id).ToArray();

            Should.Throw<BadRequestException>(() => Apply(all, Request(acknowledged, overrides: new[] { first.Id })))
                .Message.ShouldBe(AskClosure.ChangedMessage);
            Should.Throw<BadRequestException>(() => Apply(all, Request(acknowledged, overrides: new[] { first.Id, second.Id, optional.Id })))
                .Message.ShouldBe(AskClosure.ChangedMessage);
        }

        [Fact]
        public void OverrideIds_WhenNothingIsUnmet_AreRejected()
        {
            var accepted = Ask(BoardItemAskState.Accepted, isRequired: true);

            // Completing the required ask satisfies it, so there's nothing left to override.
            Should.Throw<BadRequestException>(() =>
                Apply(new[] { accepted }, Request(new[] { accepted.Id }, complete: new[] { accepted.Id }, overrides: new[] { accepted.Id })));
        }

        [Fact]
        public void AskMadeRequiredAfterThePromptOpened_CannotBeOverriddenUnseen()
        {
            // The prompt showed an optional pending ask; meanwhile someone made it required. The
            // acknowledged set still matches, but the override list the user sent doesn't name it.
            var pending = Ask(BoardItemAskState.Pending);
            var acknowledged = new[] { pending.Id };
            pending.IsRequired = true;

            Should.Throw<BadRequestException>(() => Apply(new[] { pending }, Request(acknowledged, overrides: new int[0])))
                .Message.ShouldBe(AskClosure.ChangedMessage);

            pending.State.ShouldBe(BoardItemAskState.Pending);
        }

        [Fact]
        public void ExactOverride_CompletesPastUnmetRequirements_AndRecordsExactlyThoseIds()
        {
            var requiredPending = Ask(BoardItemAskState.Pending, isRequired: true);
            var requiredDeclined = Ask(BoardItemAskState.Declined, isRequired: true);
            var requiredAccepted = Ask(BoardItemAskState.Accepted, isRequired: true);
            var all = new[] { requiredPending, requiredDeclined, requiredAccepted };

            var result = Apply(all, Request(
                all.Select(a => a.Id),
                complete: new[] { requiredAccepted.Id },
                overrides: new[] { requiredDeclined.Id, requiredPending.Id }));

            result.OverriddenAskIds.ShouldBe(new[] { requiredDeclined.Id, requiredPending.Id });
            result.Completed.ShouldBe(new[] { requiredAccepted });
            result.Cancelled.ShouldBe(new[] { requiredPending });

            // A terminal unmet requirement is left as it is, still required.
            requiredDeclined.State.ShouldBe(BoardItemAskState.Declined);
            requiredDeclined.IsRequired.ShouldBeTrue();
            requiredDeclined.Revision.ShouldBe(1);
        }

        [Theory]
        [InlineData(BoardItemAskState.Declined, null)]
        [InlineData(BoardItemAskState.Cancelled, BoardItemAskCancelReason.Withdrawn)]
        [InlineData(BoardItemAskState.Cancelled, BoardItemAskCancelReason.RecipientRemoved)]
        [InlineData(BoardItemAskState.Cancelled, BoardItemAskCancelReason.ItemCompleted)]
        public void RequiredAskThatWasDeclinedOrCancelled_StillBlocksUntilOverridden(BoardItemAskState state, BoardItemAskCancelReason? reason)
        {
            // ItemCompleted covers an item reopened after an earlier override: the requirement is
            // still unmet, so completing it again needs another explicit override.
            var required = Ask(state, isRequired: true);
            required.CancelReason = reason;

            Should.Throw<BadRequestException>(() => Apply(new[] { required }, request: null)).Message.ShouldBe(AskClosure.MissingMessage);
            Should.Throw<BadRequestException>(() => Apply(new[] { required }, Request(new[] { required.Id })));

            Apply(new[] { required }, Request(new[] { required.Id }, overrides: new[] { required.Id }))
                .OverriddenAskIds.ShouldBe(new[] { required.Id });
        }

        [Fact]
        public void WaivedRequirementOnADeclinedAsk_NoLongerNeedsReview()
        {
            var waived = Ask(BoardItemAskState.Declined, isRequired: false);

            var result = Apply(new[] { waived }, request: null);

            result.OverriddenAskIds.ShouldBeEmpty();
        }
    }
}
