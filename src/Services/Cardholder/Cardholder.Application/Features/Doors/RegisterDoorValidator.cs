using AccessHub.Cardholder.Domain.Doors;
using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.Doors;

public sealed class RegisterDoorValidator : AbstractValidator<RegisterDoorCommand>
{
    public RegisterDoorValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Door.MaxNameLength);
    }
}
