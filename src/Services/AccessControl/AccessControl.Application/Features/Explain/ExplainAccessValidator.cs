using FluentValidation;

namespace AccessHub.AccessControl.Application.Features.Explain;

public sealed class ExplainAccessValidator : AbstractValidator<ExplainAccessQuery>
{
    public ExplainAccessValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.DoorId).NotEmpty();
        RuleFor(x => x.CardNumber).NotEmpty().MaximumLength(32);
    }
}
