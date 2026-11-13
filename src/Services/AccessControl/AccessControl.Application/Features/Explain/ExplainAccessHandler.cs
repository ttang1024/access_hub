using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Application.Sync;
using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.AccessControl.Application.Features.Explain;

/// <summary>Runs the same engine the sites run, against the cloud's copy of the list. No side effects.</summary>
public sealed class ExplainAccessHandler(IAccessControlReadModel readModel, TimeProvider clock) : IQueryHandler<ExplainAccessQuery, ExplainAccessResponse>
{
    public async Task<Result<ExplainAccessResponse>> HandleAsync(ExplainAccessQuery query, CancellationToken cancellationToken)
    {
        var loaded = await readModel.LoadAccessListAsync(query.SiteId, cancellationToken);
        if (loaded is not { } site)
        {
            return SyncErrors.UnknownSite(query.SiteId);
        }

        var at = query.Timestamp ?? clock.GetUtcNow();
        var decision = AccessDecisionEngine.Decide(site.List, query.DoorId, query.CardNumber.Trim().ToUpperInvariant(), at);
        return new ExplainAccessResponse(decision.Granted, decision.Reason?.ToString(), at, site.Sequence);
    }
}
