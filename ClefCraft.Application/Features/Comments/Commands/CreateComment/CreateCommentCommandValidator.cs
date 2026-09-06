using FluentValidation;

namespace ClefCraft.Application.Features.Comments.Commands.CreateComment
{
    public class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
    {
        public CreateCommentCommandValidator()
        {
            RuleFor(c => c.EntityType)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .Must(entityType => AllowedEntityTypes.Values.Contains(entityType))
                .WithMessage("{PropertyName} is not a supported entity type");

            RuleFor(c => c.EntityId)
                .GreaterThan(0);

            RuleFor(c => c.BodyHtml)
                .NotEmpty().WithMessage("Comment body cannot be empty");
        }
    }
}
