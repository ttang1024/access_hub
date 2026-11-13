using AccessHub.Cardholder.Domain.Sites;
using FluentValidation;

namespace AccessHub.Cardholder.Application.Features.Sites;

public sealed class RegisterSiteValidator : AbstractValidator<RegisterSiteCommand>
{
    public RegisterSiteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Site.MaxNameLength);
        RuleFor(x => x.TimeZoneId).NotEmpty().Must(tz => SiteTimeZone.Create(tz).IsSuccess).WithMessage("'{PropertyValue}' is not a known IANA time zone.");
    }
}
