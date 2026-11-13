using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.Application.Features.Sites;

public sealed class RegisterSiteHandler(ISiteRepository sites, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<RegisterSiteCommand, RegisterSiteResponse>
{
    public async Task<Result<RegisterSiteResponse>> HandleAsync(RegisterSiteCommand command, CancellationToken cancellationToken)
    {
        var (apiKey, hash) = SiteApiKey.Generate();
        var site = Site.Register(command.Name, command.TimeZoneId, hash, clock.GetUtcNow());
        if (site.IsFailure)
        {
            return site.Error!;
        }

        sites.Add(site.Value);
        audit.Record("Site.Registered", nameof(Site), site.Value.Id.ToString(), null, new { site.Value.Name, site.Value.TimeZoneId });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RegisterSiteResponse(site.Value.Id.Value, apiKey);
    }
}
