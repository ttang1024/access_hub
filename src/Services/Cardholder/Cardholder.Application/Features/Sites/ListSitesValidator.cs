using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.Sites;

public sealed class ListSitesValidator : AbstractValidator<ListSitesQuery>
{
    public ListSitesValidator() => this.ApplyPageRules(x => x.Page, x => x.PageSize);
}
