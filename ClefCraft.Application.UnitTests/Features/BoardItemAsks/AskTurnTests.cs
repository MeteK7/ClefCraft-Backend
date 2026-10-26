using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class AskTurnTests
    {
        private const string Requester = "requester";
        private const string Recipient = "recipient";
        private const string Assignee = "assignee";
        private const string Owner = "owner";
        private const string Other = "other";
        private static readonly string[] Everyone = { Requester, Recipient, Assignee, Owner, Other };

        private const int BoardId = 10;
        private const int InProgressStatusId = 3;
        private const int DoneStatusId = 5;

        private static BoardItemAsk Ask(BoardItemAskState state, bool isRequired, bool itemIsDone = false) => new()
        {
            Id = 1,
            BoardItemId = 1,
            BoardItem = new Domain.BoardItem
            {
                Id = 1,
                BoardId = BoardId,
                AssigneeId = Assignee,
                BoardItemStatus = new BoardItemStatus { StatusId = itemIsDone ? DoneStatusId : InProgressStatusId }
            },
            RequestedByUserId = Requester,
            RecipientUserId = Recipient,
            State = state,
            IsRequired = isRequired
        };

        // The table the plan states: whose move each state is, and why. Nobody's while the item is Done.
        private static (string UserId, AskTurnReason Reason)? Expected(BoardItemAskState state, bool isRequired, bool itemIsDone) =>
            itemIsDone ? null : state switch
            {
                BoardItemAskState.Pending => (Recipient, AskTurnReason.Respond),
                BoardItemAskState.Accepted => (Recipient, AskTurnReason.Deliver),
                BoardItemAskState.Declined or BoardItemAskState.Cancelled when isRequired => (Requester, AskTurnReason.ResolveRequirement),
                _ => null
            };

        public static IEnumerable<object[]> AllStates()
        {
            foreach (var state in Enum.GetValues<BoardItemAskState>())
                foreach (var isRequired in new[] { false, true })
                    foreach (var itemIsDone in new[] { false, true })
                        yield return new object[] { state, isRequired, itemIsDone };
        }

        [Theory]
        [MemberData(nameof(AllStates))]
        public void EveryState_IsTheMoveOfExactlyWhoTheTableSays(BoardItemAskState state, bool isRequired, bool itemIsDone)
        {
            var ask = Ask(state, isRequired, itemIsDone);
            var expected = Expected(state, isRequired, itemIsDone);

            var move = AskTurn.Of(ask, itemIsDone);
            (move is null ? ((string, AskTurnReason)?)null : (move.Value.UserId, move.Value.Reason)).ShouldBe(expected);

            // Only the move's owner sees a reason: never the assignee, the board owner or anyone else.
            foreach (var user in Everyone)
                AskTurn.ReasonFor(ask, user, itemIsDone).ShouldBe(expected?.UserId == user ? expected?.Reason : null);
        }

        [Theory]
        [MemberData(nameof(AllStates))]
        public void TheQuery_AgreesWithTheRule(BoardItemAskState state, bool isRequired, bool itemIsDone)
        {
            var ask = Ask(state, isRequired, itemIsDone);

            foreach (var user in Everyone)
            {
                var isYourMove = AskTurn.IsYourMove(user, new[] { BoardId }, new[] { DoneStatusId }).Compile();
                isYourMove(ask).ShouldBe(AskTurn.ReasonFor(ask, user, itemIsDone) != null, $"{user}");
            }
        }

        [Fact]
        public void TheQuery_LeavesOutBoardsTheUserIsNotOn()
        {
            var ask = Ask(BoardItemAskState.Pending, isRequired: false);

            AskTurn.IsYourMove(Recipient, new[] { 99 }, new[] { DoneStatusId }).Compile()(ask).ShouldBeFalse();
        }

        // What each clearing action does to the ask, as its handler does it.
        private static readonly Dictionary<BoardItemAskAction, Action<BoardItemAsk>> Clearing = new()
        {
            [BoardItemAskAction.Accept] = a => a.State = BoardItemAskState.Accepted,
            [BoardItemAskAction.Decline] = a => a.State = BoardItemAskState.Declined,
            [BoardItemAskAction.Complete] = a => a.State = BoardItemAskState.Completed,
            // Asking again with the requirement moves it to the new ask (CreateAskCommandHandler).
            [BoardItemAskAction.AskAgain] = a => a.IsRequired = false,
            [BoardItemAskAction.WaiveRequirement] = a => a.IsRequired = false
        };

        private static readonly Dictionary<AskTurnReason, BoardItemAskAction[]> ClearedBy = new()
        {
            [AskTurnReason.Respond] = new[] { BoardItemAskAction.Accept, BoardItemAskAction.Decline },
            [AskTurnReason.Deliver] = new[] { BoardItemAskAction.Complete, BoardItemAskAction.Decline },
            [AskTurnReason.ResolveRequirement] = new[] { BoardItemAskAction.AskAgain, BoardItemAskAction.WaiveRequirement }
        };

        [Theory]
        [MemberData(nameof(AllStates))]
        public void EveryMove_CanBeClearedByItsOwner(BoardItemAskState state, bool isRequired, bool itemIsDone)
        {
            var ask = Ask(state, isRequired, itemIsDone);
            if (AskTurn.Of(ask, itemIsDone) is not { } move)
                return;

            var roles = AskActorRoles.For(ask, move.UserId, Assignee, Owner);
            var allowed = AskTransitions.AllowedActions(ask, roles, itemIsDone, DateTime.UtcNow);
            var clearing = ClearedBy[move.Reason].Where(allowed.Contains).ToList();

            clearing.ShouldNotBeEmpty($"{state}, required: {isRequired}");

            // Each of them really clears it: afterwards the ask isn't this owner's move for this reason.
            foreach (var action in clearing)
            {
                var after = Ask(state, isRequired, itemIsDone);
                Clearing[action](after);
                AskTurn.Of(after, itemIsDone).ShouldNotBe(move, $"{action} from {state}");
            }
        }

        [Fact]
        public void NudgesRepliesDueDatesAndExpectedAnswers_NeverChangeWhoseMoveItIs()
        {
            foreach (var state in Enum.GetValues<BoardItemAskState>())
                foreach (var isRequired in new[] { false, true })
                {
                    var plain = AskTurn.Of(Ask(state, isRequired), false);

                    var busy = Ask(state, isRequired);
                    busy.LastNudgedAt = DateTime.UtcNow;
                    busy.DueDate = DateTime.UtcNow.AddDays(-3);
                    busy.ExpectsAnswer = true;
                    busy.Replies = new List<BoardItemAskReply>
                    {
                        new() { AuthorUserId = Requester, Body = "Any news?" },
                        new() { AuthorUserId = Recipient, Body = "Tomorrow" }
                    };

                    AskTurn.Of(busy, false).ShouldBe(plain, $"{state}, required: {isRequired}");
                }
        }

        [Fact]
        public void ADeclinedAskThatIsNotRequired_IsNobodysMove_SinceNothingCouldClearIt()
        {
            var ask = Ask(BoardItemAskState.Declined, isRequired: false);

            AskTurn.Of(ask, false).ShouldBeNull();
            AskTransitions.AllowedActions(ask, AskActorRoles.For(ask, Requester, Assignee, Owner), false, DateTime.UtcNow)
                .ShouldNotContain(BoardItemAskAction.Cancel);
        }

        [Fact]
        public void ARequirementOverriddenWhenTheItemWasDone_IsTheRequestersMoveAgainOnceTheItemLeavesDone()
        {
            var ask = Ask(BoardItemAskState.Declined, isRequired: true, itemIsDone: true);
            AskTurn.Of(ask, itemIsDone: true).ShouldBeNull();

            AskTurn.Of(ask, itemIsDone: false).ShouldBe(new AskTurn.Move(Requester, AskTurnReason.ResolveRequirement));
        }
    }
}
