using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.Doors;

public sealed class ListDoorsValidator : AbstractValidator<ListDoorsQuery>
{
    public ListDoorsValidator() => this.ApplyPageRules(x => x.Page, x => x.PageSize);
}
