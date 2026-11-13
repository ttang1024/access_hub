using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;
using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class TimeWindowDtoValidator : AbstractValidator<TimeWindowDto>
{
    public TimeWindowDtoValidator()
    {
        RuleFor(w => w.Day).IsInEnum();
        RuleFor(w => w.StartMinute).InclusiveBetween(0, TimeWindow.MinutesPerDay - 1);
        RuleFor(w => w.EndMinute).InclusiveBetween(1, TimeWindow.MinutesPerDay).GreaterThan(w => w.StartMinute);
    }
}
