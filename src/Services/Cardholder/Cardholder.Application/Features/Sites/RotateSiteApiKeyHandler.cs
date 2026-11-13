using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.Application.Features.Sites;

public sealed class RotateSiteApiKeyHandler(ISiteRepository sites, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<RotateSiteApiKeyCommand, RotateSiteApiKeyResponse>
{
    public async Task<Result<RotateSiteApiKeyResponse>> HandleAsync(RotateSiteApiKeyCommand command, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(command.SiteId);
        var site = await sites.GetAsync(siteId, cancellationToken);
        if (site is null)
        {
            return SiteErrors.NotFound(siteId);
        }

        var (apiKey, hash) = SiteApiKey.Generate();
        var rotated = site.RotateApiKey(hash, clock.GetUtcNow());
        if (rotated.IsFailure)
        {
            return rotated.Error!;
        }

        // Never audit key material, not even the hash.
        audit.Record("Site.ApiKeyRotated", nameof(Site), site.Id.ToString(), null, new { site.Version });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RotateSiteApiKeyResponse(site.Id.Value, apiKey);
    }
}
