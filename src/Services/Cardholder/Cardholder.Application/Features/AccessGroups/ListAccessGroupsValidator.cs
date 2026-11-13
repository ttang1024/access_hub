using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class ListAccessGroupsValidator : AbstractValidator<ListAccessGroupsQuery>
{
    public ListAccessGroupsValidator() => this.ApplyPageRules(x => x.Page, x => x.PageSize);
}
