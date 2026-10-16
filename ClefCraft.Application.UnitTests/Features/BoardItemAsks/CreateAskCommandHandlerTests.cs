using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.CreateAsk;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain.Enums;
using Shouldly;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class CreateAskCommandHandlerTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Requester };

        private Task<BoardItemAskDto> Create(
            string recipient = Recipient, bool isRequired = false, int? replacesAskId = null, string note = "Review the API") =>
            _bed.CreateHandler().Handle(new CreateAskCommand
            {
                BoardItemId = ItemId,
                RecipientUserId = recipient,
                Note = note,
                IsRequired = isRequired,
                ReplacesAskId = replacesAskId
            }, CancellationToken.None);

        [Fact]
        public async Task Create_MakesAPendingAskFromTheCaller_InOneSave()
        {
            var dto = await Create(isRequired: true);

            var ask = _bed.Asks.Single();
            ask.State.ShouldBe(BoardItemAskState.Pending);
            ask.RequestedByUserId.ShouldBe(Requester);
            ask.RecipientUserId.ShouldBe(Recipient);
            ask.IsRequired.ShouldBeTrue();
            _bed.SaveCount.ShouldBe(1);

            dto.Id.ShouldBe(ask.Id);
            dto.State.ShouldBe(BoardItemAskState.Pending);
            dto.RecipientFullName.ShouldBe(FullName(Recipient));
            dto.RecipientIsBoardMember.ShouldBeTrue();
            dto.AllowedActions.ShouldContain(BoardItemAskAction.Cancel);
            dto.AllowedActions.ShouldNotContain(BoardItemAskAction.Accept);
        }

        [Fact]
        public async Task Create_IncrementsTheItemsAskVersion()
        {
            _bed.Item.AskVersion = 7;

            await Create();

            _bed.Item.AskVersion.ShouldBe(8);
        }

        [Fact]
        public async Task Create_LogsAskCreatedOnTheItem_WithoutAnAskIdItCantKnowYet()
        {
            await Create();

            var entry = _bed.LoggedOf(AskHistory.Created).Single();
            entry.EntityType.ShouldBe("BoardItem");
            entry.EntityId.ShouldBe(ItemId);
            entry.Metadata[AskHistory.RecipientUserIdKey].ShouldBe(Recipient);
            entry.Metadata[AskHistory.NoteKey].ShouldBe("Review the API");
            entry.Metadata.ShouldNotContainKey(AskHistory.AskIdKey);
        }

        [Fact]
        public async Task Create_NotifiesTheRecipient_NotTheActor()
        {
            await Create();

            var sent = _bed.Sent.ShouldHaveSingleItem();
            sent.UserId.ShouldBe(Recipient);
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Requested));
            sent.Payload.AskId.ShouldBe(_bed.Asks.Single().Id);
            sent.Payload.BoardId.ShouldBe(BoardId);
            sent.Payload.ActorFullName.ShouldBe(FullName(Requester));
        }

        [Theory]
        [InlineData(Outsider)]
        [InlineData(Requester)]
        [InlineData(Assignee)]
        public async Task Create_ForANonMemberTheCallerOrTheCurrentAssignee_IsRejected(string recipient)
        {
            await Should.ThrowAsync<BadRequestException>(() => Create(recipient));

            _bed.Asks.ShouldBeEmpty();
            _bed.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Create_ByANonMember_IsForbidden()
        {
            _bed.CallerId = Outsider;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Create(Recipient));
        }

        [Fact]
        public async Task Create_OnADoneItem_IsRejected()
        {
            _bed.MarkItemDone();

            await Should.ThrowAsync<BadRequestException>(() => Create());
            _bed.Asks.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_WithoutANote_IsRejected(string note)
        {
            await Should.ThrowAsync<BadRequestException>(() => Create(note: note));
        }

        [Fact]
        public async Task Create_ConcurrencyConflict_IsABadRequest_AndNotifiesNobody()
        {
            _bed.FailNextSave = true;

            var ex = await Should.ThrowAsync<BadRequestException>(() => Create());

            ex.Message.ShouldBe(AskCommandSupport.ConflictMessage);
            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task AskAgain_RequiredOnBoth_MovesTheRequirement_AndKeepsTheOriginalRequester()
        {
            var declined = _bed.AddAsk(BoardItemAskState.Declined, isRequired: true);
            _bed.CallerId = Assignee;

            await Create(recipient: Other, isRequired: true, replacesAskId: declined.Id);

            var replacement = _bed.Asks.Single(a => a.Id != declined.Id);
            replacement.RequestedByUserId.ShouldBe(Requester);
            replacement.IsRequired.ShouldBeTrue();
            declined.IsRequired.ShouldBeFalse();
            declined.State.ShouldBe(BoardItemAskState.Declined);
            declined.Revision.ShouldBe(4);

            var moved = _bed.LoggedOf(AskHistory.RequirementMoved).Single();
            moved.Metadata[AskHistory.AskIdKey].ShouldBe(declined.Id);
            moved.Metadata[AskHistory.ToRecipientUserIdKey].ShouldBe(Other);
        }

        [Fact]
        public async Task AskAgain_NewAskNotRequired_LeavesTheOldRequirementUnmet()
        {
            var declined = _bed.AddAsk(BoardItemAskState.Declined, isRequired: true);

            await Create(recipient: Other, isRequired: false, replacesAskId: declined.Id);

            declined.IsRequired.ShouldBeTrue();
            AskTransitions.IsUnmetRequirement(declined).ShouldBeTrue();
            _bed.LoggedOf(AskHistory.RequirementMoved).ShouldBeEmpty();
        }

        [Fact]
        public async Task AskAgain_ByANonManager_IsForbidden()
        {
            var declined = _bed.AddAsk(BoardItemAskState.Declined);
            _bed.CallerId = Other;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Create(recipient: Recipient, replacesAskId: declined.Id));
        }

        [Fact]
        public async Task AskAgain_OnAnOpenAsk_IsRejected()
        {
            var pending = _bed.AddAsk(BoardItemAskState.Pending);

            await Should.ThrowAsync<BadRequestException>(() => Create(recipient: Other, replacesAskId: pending.Id));
        }

        [Fact]
        public async Task AskAgain_ToTheCurrentAssignee_IsRejected()
        {
            var declined = _bed.AddAsk(BoardItemAskState.Declined);

            await Should.ThrowAsync<BadRequestException>(() => Create(recipient: Assignee, replacesAskId: declined.Id));
        }

        [Fact]
        public async Task AskAgain_ForAnAskOnAnotherItem_IsRejected()
        {
            var otherItem = _bed.AddItemOnOtherBoard();
            var elsewhere = _bed.AddAsk(BoardItemAskState.Declined, item: otherItem);

            await Should.ThrowAsync<BadRequestException>(() => Create(recipient: Other, replacesAskId: elsewhere.Id));
        }
    }
}
