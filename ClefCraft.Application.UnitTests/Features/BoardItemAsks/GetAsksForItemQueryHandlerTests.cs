using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Application.Features.BoardItemAsks.Queries.GetAsksForItem;
using ClefCraft.Domain.Enums;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class GetAsksForItemQueryHandlerTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Assignee };

        private Task<List<BoardItemAskDto>> Get() =>
            _bed.GetAsksForItemHandler().Handle(new GetAsksForItemQuery(ItemId), CancellationToken.None);

        [Fact]
        public async Task ReturnsEverythingTheCompletionPromptNeeds()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending);
            var accepted = _bed.AddAsk(BoardItemAskState.Accepted, recipient: Other, isRequired: true);
            var declinedRequired = _bed.AddAsk(BoardItemAskState.Declined, isRequired: true);
            var removedRequired = _bed.AddAsk(BoardItemAskState.Cancelled, recipient: "user-gone", isRequired: true);
            removedRequired.CancelReason = BoardItemAskCancelReason.RecipientRemoved;
            var completed = _bed.AddAsk(BoardItemAskState.Completed, isRequired: true);

            var result = await Get();

            result.Select(d => d.Id).ShouldBe(new[] { pending.Id, accepted.Id, declinedRequired.Id, removedRequired.Id, completed.Id });

            var toReview = result.Where(d => d.IsOpen || d.IsUnmetRequirement).Select(d => d.Id).ToList();
            toReview.ShouldBe(AskClosure.AsksToReview(_bed.Asks).Select(a => a.Id).ToList());
            toReview.ShouldBe(new[] { pending.Id, accepted.Id, declinedRequired.Id, removedRequired.Id });

            var removed = result.Single(d => d.Id == removedRequired.Id);
            removed.IsUnmetRequirement.ShouldBeTrue();
            removed.RecipientIsBoardMember.ShouldBeFalse();
            removed.CancelReason.ShouldBe(BoardItemAskCancelReason.RecipientRemoved);
            removed.Revision.ShouldBe(removedRequired.Revision);

            var acceptedDto = result.Single(d => d.Id == accepted.Id);
            acceptedDto.AllowedActions.ShouldContain(BoardItemAskAction.CompleteOnBehalf);
            acceptedDto.RecipientFullName.ShouldBe(FullName(Other));
            acceptedDto.IsRequired.ShouldBeTrue();
        }

        [Fact]
        public async Task AllowedActions_AreTheCallers()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending);

            _bed.CallerId = Recipient;
            (await Get()).Single().AllowedActions.ShouldBe(new[] { BoardItemAskAction.Accept, BoardItemAskAction.Decline });

            _bed.CallerId = Other;
            (await Get()).Single().AllowedActions.ShouldBeEmpty();
        }

        [Fact]
        public async Task RecipientWhoBecameTheAssignee_KeepsTheirAsk_AndManagesIt()
        {
            var accepted = _bed.AddAsk(BoardItemAskState.Accepted);
            _bed.Item.AssigneeId = Recipient;
            _bed.CallerId = Recipient;

            var dto = (await Get()).Single();

            dto.Id.ShouldBe(accepted.Id);
            dto.State.ShouldBe(BoardItemAskState.Accepted);
            dto.AllowedActions.ShouldContain(BoardItemAskAction.Complete);
            dto.AllowedActions.ShouldContain(BoardItemAskAction.Cancel);
        }

        [Theory]
        [InlineData(Assignee, true)]
        [InlineData(Owner, true)]
        [InlineData(Requester, true)]
        [InlineData(Recipient, false)]
        [InlineData(Other, false)]
        public async Task NextNudgeAt_GoesOnlyToAskManagers_DuringTheCooldown(string viewer, bool getsIt)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            ask.LastNudgedAt = System.DateTime.UtcNow.AddHours(-2);
            _bed.CallerId = viewer;

            var dto = (await Get()).Single();

            dto.AllowedActions.ShouldNotContain(BoardItemAskAction.Nudge);
            if (getsIt)
                dto.NextNudgeAt.ShouldBe(ask.LastNudgedAt.Value.AddHours(24));
            else
                dto.NextNudgeAt.ShouldBeNull();
        }

        [Fact]
        public async Task OnADoneItem_NobodyMayDoAnything()
        {
            _bed.AddAsk(BoardItemAskState.Declined, isRequired: true);
            _bed.MarkItemDone();

            (await Get()).Single().AllowedActions.ShouldBeEmpty();
        }

        [Fact]
        public async Task NonMember_IsForbidden()
        {
            _bed.CallerId = Outsider;

            await Should.ThrowAsync<ForbiddenAccessException>(Get);
        }
    }
}
