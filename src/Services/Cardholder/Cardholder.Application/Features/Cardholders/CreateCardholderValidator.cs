using FluentValidation;
using CardholderAggregate = AccessHub.Cardholder.Domain.Cardholders.Cardholder;

namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed class CreateCardholderValidator : AbstractValidator<CreateCardholderCommand>
{
    public CreateCardholderValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(CardholderAggregate.MaxNameLength);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(CardholderAggregate.MaxNameLength);
        RuleFor(x => x.AccessGroupIds!.Count).LessThanOrEqualTo(CardholderAggregate.MaxGroups).When(x => x.AccessGroupIds is not null).OverridePropertyName("accessGroupIds");
        RuleForEach(x => x.AccessGroupIds).NotEmpty();
    }
}
