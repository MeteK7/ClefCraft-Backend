using FluentValidation;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.AddAskReply
{
    public class AddAskReplyCommandValidator : AbstractValidator<AddAskReplyCommand>
    {
        public AddAskReplyCommandValidator()
        {
            RuleFor(c => c.AskId)
                .GreaterThan(0);

            RuleFor(c => (string?)c.Body)
                .ValidReplyBody();
        }
    }
}
