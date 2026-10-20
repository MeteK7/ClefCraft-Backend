using FluentValidation;

namespace ClefCraft.Application.Features.BoardItemAsks.Commands.EditAskReply
{
    public class EditAskReplyCommandValidator : AbstractValidator<EditAskReplyCommand>
    {
        public EditAskReplyCommandValidator()
        {
            RuleFor(c => c.ReplyId)
                .GreaterThan(0);

            RuleFor(c => (string?)c.Body)
                .ValidReplyBody();
        }
    }
}
