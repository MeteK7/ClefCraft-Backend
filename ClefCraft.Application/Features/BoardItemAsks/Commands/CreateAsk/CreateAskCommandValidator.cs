using FluentValidation;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.CreateAsk
{
    public class CreateAskCommandValidator : AbstractValidator<CreateAskCommand>
    {
        public CreateAskCommandValidator()
        {
            RuleFor(c => c.BoardItemId)
                .GreaterThan(0);

            RuleFor(c => c.RecipientUserId)
                .NotEmpty().WithMessage("Choose who to ask");

            RuleFor(c => c.Note)
                .Must(note => !string.IsNullOrWhiteSpace(note)).WithMessage("Say what you're asking for")
                .MaximumLength(BoardItemAskLimits.NoteMaxLength);
        }
    }
}
