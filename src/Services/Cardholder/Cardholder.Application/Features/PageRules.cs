using AccessHub.BuildingBlocks.Application;
using FluentValidation;

namespace AccessHub.Cardholder.Application.Features;

internal static class PageRules
{
    public static void ApplyPageRules<T>(this AbstractValidator<T> validator, Func<T, int> page, Func<T, int> pageSize)
    {
        validator.RuleFor(x => page(x)).GreaterThanOrEqualTo(1).OverridePropertyName("page");
        validator.RuleFor(x => pageSize(x)).InclusiveBetween(1, PageRequest.MaxPageSize).OverridePropertyName("pageSize");
    }
}
