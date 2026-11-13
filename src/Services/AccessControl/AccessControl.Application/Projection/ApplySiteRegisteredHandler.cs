using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.ReadModel;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.AccessControl.Application.Projection;

/// <summary>Stores the site's time zone and API key hash. Changes no list entries.</summary>
public sealed class ApplySiteRegisteredHandler(IProjectionStore store) : ICommandHandler<SiteRegistered>, ICommandHandler<SiteApiKeyRotated>
{
    public Task<Result> HandleAsync(SiteRegistered e, CancellationToken cancellationToken) =>
        ApplyAsync(e.SiteId, e.Name, e.TimeZoneId, e.ApiKeyHash, e.Version, cancellationToken);

    public Task<Result> HandleAsync(SiteApiKeyRotated e, CancellationToken cancellationToken) =>
        ApplyAsync(e.SiteId, e.Name, e.TimeZoneId, e.ApiKeyHash, e.Version, cancellationToken);

    private async Task<Result> ApplyAsync(Guid siteId, string name, string timeZoneId, string apiKeyHash, int version, CancellationToken cancellationToken)
    {
        await store.AcquireProjectionLockAsync(cancellationToken);
        var site = await store.FindSiteAsync(siteId, cancellationToken);
        if (site is null)
        {
            site = SiteState.Placeholder(siteId);
            store.Add(site);
        }

        site.Apply(name, timeZoneId, Convert.FromBase64String(apiKeyHash), version);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
