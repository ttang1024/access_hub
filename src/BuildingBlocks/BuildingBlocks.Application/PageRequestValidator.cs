using FluentValidation;

namespace AccessHub.BuildingBlocks.Application;

/// <summary>Validates paging parameters.</summary>
public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    /// <summary>Creates the validator.</summary>
    public PageRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}
