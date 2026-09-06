using FluentValidation;

namespace ClefCraft.Application.Features.Comments.Commands.UpdateComment
{
    public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
    {
        public UpdateCommentCommandValidator()
        {
            RuleFor(c => c.BodyHtml)
                .NotEmpty().WithMessage("Comment body cannot be empty");
        }
    }
}
