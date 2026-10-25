using ClefCraft.Application.Exceptions;
using ClefCraft.Application.Features.BoardItemAsks;
using ClefCraft.Application.Features.BoardItemAsks.Commands.UpdateAsk;
using ClefCraft.Application.Features.BoardItemAsks.DTOs;
using ClefCraft.Domain;
using ClefCraft.Domain.Enums;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static ClefCraft.Application.UnitTests.Features.BoardItemAsks.AskTestBed;

namespace ClefCraft.Application.UnitTests.Features.BoardItemAsks
{
    public class UpdateAskCommandHandlerTests
    {
        private readonly AskTestBed _bed = new() { CallerId = Requester };

        private Task<BoardItemAskDto> Update(BoardItemAsk ask, string? note = null, DateTime? dueDate = null,
            bool? isRequired = null, int? expectedRevision = null, string? waiveReason = null, bool clearDueDate = false) =>
            _bed.UpdateHandler().Handle(new UpdateAskCommand
            {
                AskId = ask.Id,
                ExpectedRevision = expectedRevision ?? ask.Revision,
                Note = note ?? ask.Note,
                DueDate = clearDueDate ? null : dueDate ?? ask.DueDate,
                IsRequired = isRequired ?? ask.IsRequired,
                WaiveReason = waiveReason
            }, CancellationToken.None);

        [Fact]
        public async Task EditingAnAcceptedAsksNote_AsksAgain_WithoutResettingItsAge()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);
            var asked = ask.DateCreated;

            var dto = await Update(ask, note: "Review the migration and the rollback");

            ask.State.ShouldBe(BoardItemAskState.Pending);
            ask.RespondedAt.ShouldBeNull();
            ask.DateCreated.ShouldBe(asked);
            ask.Revision.ShouldBe(4);
            dto.State.ShouldBe(BoardItemAskState.Pending);
            _bed.LoggedOf(AskHistory.Changed).ShouldHaveSingleItem().Metadata[AskHistory.AskIdKey].ShouldBe(ask.Id);
        }

        [Fact]
        public async Task EditingTheDueDate_NotifiesTheRecipientOfTheChange()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Update(ask, dueDate: new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));

            var sent = _bed.Sent.ShouldHaveSingleItem();
            sent.UserId.ShouldBe(Recipient);
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Changed));
        }

        [Fact]
        public async Task SettingRequired_DoesNotChangeTheState()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Accepted);

            await Update(ask, isRequired: true);

            ask.IsRequired.ShouldBeTrue();
            ask.State.ShouldBe(BoardItemAskState.Accepted);
            _bed.LoggedOf(AskHistory.RequirementSet).ShouldHaveSingleItem();
            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task WaivingTheRequirementOfADeclinedAsk_IsAllowed_AndRecordsTheReason()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Declined, isRequired: true);

            await Update(ask, isRequired: false, waiveReason: "Security signed off in person");

            ask.IsRequired.ShouldBeFalse();
            ask.State.ShouldBe(BoardItemAskState.Declined);
            _bed.LoggedOf(AskHistory.RequirementWaived).ShouldHaveSingleItem()
                .Metadata[AskHistory.ReasonKey].ShouldBe("Security signed off in person");
        }

        [Theory]
        [InlineData(Assignee)]
        [InlineData(Owner)]
        public async Task SomeoneElseWaivingTheRequirement_TellsOnlyTheRequester_WhoseMoveItWas(string manager)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Declined, isRequired: true);
            AskTurn.Of(ask, false)!.Value.UserId.ShouldBe(Requester);
            _bed.CallerId = manager;

            await Update(ask, isRequired: false);

            var sent = _bed.Sent.ShouldHaveSingleItem();
            sent.UserId.ShouldBe(Requester);
            sent.Payload.Event.ShouldBe(nameof(AskEvent.Changed));
            sent.Payload.Reason.ShouldBe(AskNotifier.RequirementWaivedReason);
            AskTurn.Of(ask, false).ShouldBeNull();
        }

        [Fact]
        public async Task TheRequesterWaivingTheirOwnRequirement_NotifiesNobody()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Cancelled, isRequired: true);

            await Update(ask, isRequired: false);

            _bed.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task SettingTheRequirement_NotifiesNobody()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = Assignee;

            await Update(ask, isRequired: true);

            _bed.Sent.ShouldBeEmpty();
        }

        [Theory]
        [InlineData(BoardItemAskState.Declined)]
        [InlineData(BoardItemAskState.Cancelled)]
        [InlineData(BoardItemAskState.Completed)]
        public async Task EditingTheTermsOfAClosedAsk_IsRejected(BoardItemAskState state)
        {
            var ask = _bed.AddAsk(state);

            await Should.ThrowAsync<BadRequestException>(() => Update(ask, note: "Something else"));
            ask.Note.ShouldBe("Review the migration");
        }

        [Fact]
        public async Task ChangingRequiredOnACompletedAsk_IsRejected()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Completed, isRequired: true);

            await Should.ThrowAsync<BadRequestException>(() => Update(ask, isRequired: false));
            ask.IsRequired.ShouldBeTrue();
        }

        [Fact]
        public async Task StaleRevision_IsRejected_AndSavesNothing()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            var ex = await Should.ThrowAsync<BadRequestException>(() => Update(ask, note: "New", expectedRevision: ask.Revision - 1));

            ex.Message.ShouldContain("changed since you loaded it");
            _bed.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task TheRecipient_CannotEditTheTerms()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = Recipient;

            await Should.ThrowAsync<ForbiddenAccessException>(() => Update(ask, note: "Less work"));
        }

        [Theory]
        [InlineData(Assignee)]
        [InlineData(Owner)]
        public async Task TheAssigneeAndTheBoardOwner_ManageAsksTheyDidntMake(string manager)
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.CallerId = manager;

            await Update(ask, note: "Narrower scope");

            ask.Note.ShouldBe("Narrower scope");
        }

        [Fact]
        public async Task AnyChange_OnADoneItem_IsRejected()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);
            _bed.MarkItemDone();

            await Should.ThrowAsync<BadRequestException>(() => Update(ask, note: "Too late"));
        }

        [Fact]
        public async Task NothingChanged_SavesNothing()
        {
            var ask = _bed.AddAsk(BoardItemAskState.Pending);

            await Update(ask);

            _bed.SaveCount.ShouldBe(0);
            _bed.Logged.ShouldBeEmpty();
        }

        [Fact]
        public async Task UnknownAsk_IsNotFound()
        {
            var phantom = new BoardItemAsk { Id = 4242, Note = "x" };

            await Should.ThrowAsync<NotFoundException>(() => Update(phantom));
        }
    }
}
