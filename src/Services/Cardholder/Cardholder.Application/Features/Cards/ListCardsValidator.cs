using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.Cards;

public sealed class ListCardsValidator : AbstractValidator<ListCardsQuery>
{
    public ListCardsValidator() => this.ApplyPageRules(x => x.Page, x => x.PageSize);
}
