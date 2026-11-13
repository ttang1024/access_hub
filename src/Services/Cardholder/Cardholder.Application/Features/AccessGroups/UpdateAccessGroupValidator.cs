using AccessHub.Cardholder.Domain.AccessGroups;
using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class UpdateAccessGroupValidator : AbstractValidator<UpdateAccessGroupCommand>
{
    public UpdateAccessGroupValidator()
    {
        RuleFor(x => x.AccessGroupId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(AccessGroup.MaxNameLength);
        RuleFor(x => x.ExpectedVersion).GreaterThan(0).When(x => x.ExpectedVersion is not null);
        RuleFor(x => x.Doors).NotNull().Must(d => d is null || d.Count <= AccessGroup.MaxDoors).WithMessage($"At most {AccessGroup.MaxDoors} doors.");
        RuleForEach(x => x.Doors).SetValidator(new AccessGroupDoorInputValidator());
    }
}
