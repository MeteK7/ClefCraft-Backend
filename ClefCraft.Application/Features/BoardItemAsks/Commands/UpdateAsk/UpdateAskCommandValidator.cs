using FluentValidation;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.UpdateAsk
{
    public class UpdateAskCommandValidator : AbstractValidator<UpdateAskCommand>
    {
        public UpdateAskCommandValidator()
        {
            RuleFor(c => c.AskId)
                .GreaterThan(0);

            RuleFor(c => c.Note)
                .Must(note => !string.IsNullOrWhiteSpace(note)).WithMessage("Say what you're asking for")
                .MaximumLength(BoardItemAskLimits.NoteMaxLength);

            RuleFor(c => c.WaiveReason)
                .MaximumLength(BoardItemAskLimits.ResponseNoteMaxLength);
        }
    }
}
