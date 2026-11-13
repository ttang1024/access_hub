using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.Cardholder;
using AccessHub.EventIngestion.Application.Abstractions;
using AccessHub.EventIngestion.Domain.Sites;

namespace AccessHub.EventIngestion.Application.Projection;

/// <summary>Keeps the API key hash of each site, so sites authenticate without a call to Cardholder.</summary>
public sealed class ApplySiteKeyHandler(IKnownSiteStore sites, IUnitOfWork unitOfWork) : ICommandHandler<SiteRegistered>, ICommandHandler<SiteApiKeyRotated>
{
    public Task<Result> HandleAsync(SiteRegistered e, CancellationToken cancellationToken) => ApplyAsync(e.SiteId, e.ApiKeyHash, e.Version, cancellationToken);

    public Task<Result> HandleAsync(SiteApiKeyRotated e, CancellationToken cancellationToken) => ApplyAsync(e.SiteId, e.ApiKeyHash, e.Version, cancellationToken);

    private async Task<Result> ApplyAsync(Guid siteId, string hash, int version, CancellationToken cancellationToken)
    {
        var site = await sites.FindAsync(siteId, cancellationToken);
        if (site is null)
        {
            site = KnownSite.Create(siteId);
            sites.Add(site);
        }

        if (site.Apply(Convert.FromBase64String(hash), version))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
