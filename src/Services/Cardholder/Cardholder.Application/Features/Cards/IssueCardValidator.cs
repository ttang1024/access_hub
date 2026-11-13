using AccessHub.Cardholder.Domain.Cards;
using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.Cards;

public sealed class IssueCardValidator : AbstractValidator<IssueCardCommand>
{
    public IssueCardValidator(TimeProvider clock)
    {
        RuleFor(x => x.CardholderId).NotEmpty();
        RuleFor(x => x.CardNumber).NotEmpty().Must(n => CardNumber.Create(n).IsSuccess).WithMessage("A card number is 4 to 32 letters or digits.");
        RuleFor(x => x.ExpiresAt).GreaterThan(_ => clock.GetUtcNow()).When(x => x.ExpiresAt is not null).WithMessage("Expiry must be in the future.");
    }
}
