using FluentValidation;

namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetActivityLogForEntity
{
    public class GetActivityLogForEntityValidator : AbstractValidator<GetActivityLogForEntityQuery>
    {
        public GetActivityLogForEntityValidator()
        {
            RuleFor(q => q.EntityType)
                .NotEmpty().WithMessage("{PropertyName} is required")
                .Must(entityType => AllowedEntityTypes.Values.Contains(entityType))
                .WithMessage("{PropertyName} is not a supported entity type");

            RuleFor(q => q.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("{PropertyName} must be at least 1");

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, 100).WithMessage("{PropertyName} must be between 1 and 100");
        }
    }
}
