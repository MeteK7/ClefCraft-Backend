using FluentValidation;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.ReassignAsk
{
    public class ReassignAskCommandValidator : AbstractValidator<ReassignAskCommand>
    {
        public ReassignAskCommandValidator()
        {
            RuleFor(c => c.AskId)
                .GreaterThan(0);

            RuleFor(c => c.NewRecipientUserId)
                .NotEmpty().WithMessage("Choose who to ask instead");

            RuleFor(c => c.Note)
                .Must(note => note == null || !string.IsNullOrWhiteSpace(note)).WithMessage("Say what you're asking for")
                .MaximumLength(BoardItemAskLimits.NoteMaxLength);
        }
    }
}
