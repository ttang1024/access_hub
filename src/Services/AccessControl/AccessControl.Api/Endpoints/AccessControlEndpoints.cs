using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Application.Features.Explain;
using AccessHub.AccessControl.Application.Features.SyncStatus;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Web;

namespace AccessHub.AccessControl.Api.Endpoints;

internal static class AccessControlEndpoints
{
    public static IEndpointRouteBuilder MapAccessControlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.SyncStatus, async (IQueryHandler<GetSyncStatusQuery, IReadOnlyList<SiteSyncStatusDto>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetSyncStatusQuery(), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithTags(ApiRoutes.Tag)
            .WithSummary("Per-site sync status: connected, last seen, latest and delivered sequence, lag")
            .Produces<IReadOnlyList<SiteSyncStatusDto>>();

        app.MapPost(ApiRoutes.Explain, async (ExplainAccessRequest request, IQueryHandler<ExplainAccessQuery, ExplainAccessResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ExplainAccessQuery(request.SiteId, request.DoorId, request.CardNumber, request.Timestamp), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithTags(ApiRoutes.Tag)
            .WithSummary("Explain an access decision")
            .WithDescription("Runs the same decision engine the sites run, against the cloud's copy of the site's list. No side effects.")
            .Produces<ExplainAccessResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
