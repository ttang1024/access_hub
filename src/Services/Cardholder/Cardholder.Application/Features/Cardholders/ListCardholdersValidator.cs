using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed class ListCardholdersValidator : AbstractValidator<ListCardholdersQuery>
{
    public ListCardholdersValidator()
    {
        this.ApplyPageRules(x => x.Page, x => x.PageSize);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}
