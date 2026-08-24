using FluentValidation;

namespace ClefCraft.Application.Features.ActivityLogs.Queries.GetCalendarEventActivity
{
    public class GetCalendarEventActivityValidator : AbstractValidator<GetCalendarEventActivityQuery>
    {
        public GetCalendarEventActivityValidator()
        {
            RuleFor(q => q.EventId)
                .GreaterThan(0).WithMessage("{PropertyName} must be a valid event id");

            RuleFor(q => q.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("{PropertyName} must be at least 1");

            RuleFor(q => q.PageSize)
                .InclusiveBetween(1, 100).WithMessage("{PropertyName} must be between 1 and 100");
        }
    }
}
