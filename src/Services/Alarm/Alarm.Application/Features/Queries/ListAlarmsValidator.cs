using AccessHub.BuildingBlocks.Application;
using FluentValidation;

namespace AccessHub.Alarm.Application.Features.Queries;

public sealed class ListAlarmsValidator : AbstractValidator<ListAlarmsQuery>
{
    private static readonly string[] States = ["open", "Raised", "Acknowledged", "Closed"];

    public ListAlarmsValidator()
    {
        RuleFor(x => x.State).Must(s => s is null || States.Contains(s, StringComparer.OrdinalIgnoreCase)).WithMessage("state must be open, Raised, Acknowledged or Closed.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}
