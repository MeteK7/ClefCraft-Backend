using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class AskTransitionsTests
    {
        private enum Who { Recipient, Manager, Either }

        // The lifecycle as the plan states it (§3.3 / §3.5): who may take each action, from which
        // states, and the extra condition on the requirement where there is one.
        private static readonly Dictionary<BoardItemAskAction, (Who Who, BoardItemAskState[] From, bool? NeedsRequired)> Spec = new()
        {
            [BoardItemAskAction.Accept] = (Who.Recipient, new[] { BoardItemAskState.Pending }, null),
            [BoardItemAskAction.Decline] = (Who.Recipient, new[] { BoardItemAskState.Pending, BoardItemAskState.Accepted }, null),
            [BoardItemAskAction.Complete] = (Who.Recipient, new[] { BoardItemAskState.Accepted }, null),
            [BoardItemAskAction.Reopen] = (Who.Recipient, new[] { BoardItemAskState.Completed }, null),
            [BoardItemAskAction.Cancel] = (Who.Manager, new[] { BoardItemAskState.Pending, BoardItemAskState.Accepted }, null),
            [BoardItemAskAction.Edit] = (Who.Manager, new[] { BoardItemAskState.Pending, BoardItemAskState.Accepted }, null),
            [BoardItemAskAction.SetRequired] = (Who.Manager, new[] { BoardItemAskState.Pending, BoardItemAskState.Accepted }, false),
            [BoardItemAskAction.WaiveRequirement] = (Who.Manager,
                new[] { BoardItemAskState.Pending, BoardItemAskState.Accepted, BoardItemAskState.Declined, BoardItemAskState.Cancelled }, true),
            [BoardItemAskAction.Nudge] = (Who.Manager, new[] { BoardItemAskState.Pending }, null),
            [BoardItemAskAction.Reassign] = (Who.Manager, new[] { BoardItemAskState.Pending, BoardItemAskState.Accepted }, null),
            [BoardItemAskAction.AskAgain] = (Who.Manager, new[] { BoardItemAskState.Declined, BoardItemAskState.Cancelled }, null),
            [BoardItemAskAction.CompleteOnBehalf] = (Who.Either, new[] { BoardItemAskState.Accepted }, null)
        };

        public static IEnumerable<object[]> AllCombinations()
        {
            foreach (var action in System.Enum.GetValues<BoardItemAskAction>())
                foreach (var state in System.Enum.GetValues<BoardItemAskState>())
                    foreach (var isRecipient in new[] { false, true })
                        foreach (var isManager in new[] { false, true })
                            foreach (var isRequired in new[] { false, true })
                                yield return new object[] { action, state, isRecipient, isManager, isRequired };
        }

        [Theory]
        [MemberData(nameof(AllCombinations))]
        public void EveryActionStateAndRole_IsAllowedExactlyAsSpecified(
            BoardItemAskAction action, BoardItemAskState state, bool isRecipient, bool isManager, bool isRequired)
        {
            var (who, from, needsRequired) = Spec[action];
            var roleOk = who switch
            {
                Who.Recipient => isRecipient,
                Who.Manager => isManager,
                _ => isRecipient || isManager
            };
            var expected = roleOk && from.Contains(state) && (needsRequired == null || needsRequired == isRequired);

            var ask = new BoardItemAsk { State = state, IsRequired = isRequired };
            var roles = new AskActorRoles(isRecipient, isManager);

            (AskTransitions.HasRole(action, roles) && AskTransitions.IsAllowedFrom(action, ask)).ShouldBe(expected);
            AskTransitions.AllowedActions(ask, roles, itemIsDone: false, System.DateTime.UtcNow).Contains(action).ShouldBe(expected);
        }

        [Fact]
        public void AllowedActions_ItemIsDone_IsEmptyForEveryone()
        {
            var ask = new BoardItemAsk { State = BoardItemAskState.Accepted, IsRequired = true };

            AskTransitions.AllowedActions(ask, new AskActorRoles(true, true), itemIsDone: true, System.DateTime.UtcNow)
                .ShouldBeEmpty();
        }

        [Fact]
        public void AllowedActions_NudgedLessThan24HoursAgo_LeavesNudgeOut_ThenOffersItAgain()
        {
            var now = System.DateTime.UtcNow;
            var ask = new BoardItemAsk { State = BoardItemAskState.Pending, LastNudgedAt = now.AddHours(-23) };
            var manager = new AskActorRoles(IsRecipient: false, IsManager: true);

            AskTransitions.AllowedActions(ask, manager, false, now).ShouldNotContain(BoardItemAskAction.Nudge);

            ask.LastNudgedAt = now.AddHours(-25);
            AskTransitions.AllowedActions(ask, manager, false, now).ShouldContain(BoardItemAskAction.Nudge);
        }

        [Fact]
        public void EnsureAllowed_WrongRole_IsForbidden_EvenOnADoneItem()
        {
            var ask = new BoardItemAsk { State = BoardItemAskState.Pending };

            Should.Throw<ForbiddenAccessException>(() =>
                AskTransitions.EnsureAllowed(BoardItemAskAction.Accept, ask, new AskActorRoles(false, true), itemIsDone: true));
        }

        [Fact]
        public void EnsureAllowed_RightRoleOnADoneItem_IsABadRequest()
        {
            var ask = new BoardItemAsk { State = BoardItemAskState.Pending };

            Should.Throw<BadRequestException>(() =>
                AskTransitions.EnsureAllowed(BoardItemAskAction.Accept, ask, new AskActorRoles(true, false), itemIsDone: true));
        }

        [Fact]
        public void EnsureAllowed_RightRoleWrongState_IsABadRequest()
        {
            var ask = new BoardItemAsk { State = BoardItemAskState.Declined };

            Should.Throw<BadRequestException>(() =>
                AskTransitions.EnsureAllowed(BoardItemAskAction.Accept, ask, new AskActorRoles(true, false), itemIsDone: false));
        }

        [Theory]
        [InlineData(BoardItemAskState.Pending, true)]
        [InlineData(BoardItemAskState.Accepted, true)]
        [InlineData(BoardItemAskState.Declined, true)]
        [InlineData(BoardItemAskState.Cancelled, true)]
        [InlineData(BoardItemAskState.Completed, false)]
        public void IsUnmetRequirement_OnlyCompletedSatisfiesARequirement(BoardItemAskState state, bool unmet)
        {
            AskTransitions.IsUnmetRequirement(new BoardItemAsk { State = state, IsRequired = true }).ShouldBe(unmet);
            AskTransitions.IsUnmetRequirement(new BoardItemAsk { State = state, IsRequired = false }).ShouldBeFalse();
        }

        [Theory]
        [InlineData("requester", false, true)]
        [InlineData("assignee", false, true)]
        [InlineData("owner", false, true)]
        [InlineData("recipient", true, false)]
        [InlineData("someone-else", false, false)]
        public void Roles_ManagersAreTheRequesterTheAssigneeAndTheBoardOwner(string caller, bool isRecipient, bool isManager)
        {
            var ask = new BoardItemAsk { RequestedByUserId = "requester", RecipientUserId = "recipient" };

            AskActorRoles.For(ask, caller, assigneeId: "assignee", boardOwnerId: "owner")
                .ShouldBe(new AskActorRoles(isRecipient, isManager));
        }

        [Fact]
        public void Roles_RecipientWhoBecomesTheAssignee_IsRecipientAndManager()
        {
            var ask = new BoardItemAsk { RequestedByUserId = "requester", RecipientUserId = "recipient" };

            var roles = AskActorRoles.For(ask, "recipient", assigneeId: "recipient", boardOwnerId: "owner");

            roles.ShouldBe(new AskActorRoles(IsRecipient: true, IsManager: true));
        }
    }
}
