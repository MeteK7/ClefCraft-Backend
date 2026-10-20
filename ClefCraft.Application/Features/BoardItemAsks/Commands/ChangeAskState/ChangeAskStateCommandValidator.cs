using FluentValidation;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.ChangeAskState
{
    public class ChangeAskStateCommandValidator : AbstractValidator<ChangeAskStateCommand>
    {
        public static readonly IReadOnlySet<BoardItemAskAction> Actions = new HashSet<BoardItemAskAction>
        {
            BoardItemAskAction.Accept,
            BoardItemAskAction.Decline,
            BoardItemAskAction.Complete,
            BoardItemAskAction.Reopen,
            BoardItemAskAction.Cancel
        };

        public ChangeAskStateCommandValidator()
        {
            RuleFor(c => c.AskId)
                .GreaterThan(0);

            RuleFor(c => c.Action)
                .Must(Actions.Contains).WithMessage("{PropertyName} must be Accept, Decline, Complete, Reopen or Cancel");

            RuleFor(c => c.Reason)
                .MaximumLength(BoardItemAskLimits.ResponseNoteMaxLength);

            RuleFor(c => c.ReplyBody)
                .Null().WithMessage("Only marking an ask done can include a reply")
                .When(c => c.Action != BoardItemAskAction.Complete);

            RuleFor(c => c.ReplyBody)
                .ValidReplyBody()
                .When(c => c.Action == BoardItemAskAction.Complete && c.ReplyBody != null);
        }
    }
}
