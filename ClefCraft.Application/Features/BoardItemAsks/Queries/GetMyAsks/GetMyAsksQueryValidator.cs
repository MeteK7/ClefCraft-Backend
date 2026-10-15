using FluentValidation;

namespace ClefCraft.Application.Features.BoardItemAsks.Queries.GetMyAsks
{
    public class GetMyAsksQueryValidator : AbstractValidator<GetMyAsksQuery>
    {
        public GetMyAsksQueryValidator()
        {
            RuleFor(q => q.Direction)
                .IsInEnum().WithMessage("{PropertyName} must be Received or Sent");
        }
    }
}
