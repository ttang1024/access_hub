using AccessHub.Cardholder.Domain.AccessGroups;
using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class AccessGroupDoorInputValidator : AbstractValidator<AccessGroupDoorInput>
{
    public AccessGroupDoorInputValidator()
    {
        RuleFor(d => d.DoorId).NotEmpty();
        RuleFor(d => d.Windows).NotEmpty().Must(w => w is null || w.Count <= Schedule.MaxWindows);
        RuleForEach(d => d.Windows).SetValidator(new TimeWindowDtoValidator());
    }
}
