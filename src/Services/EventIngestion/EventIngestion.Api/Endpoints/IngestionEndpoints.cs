using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Web;
using AccessHub.EventIngestion.Application.Features.IngestEvents;

namespace AccessHub.EventIngestion.Api.Endpoints;

internal static class IngestionEndpoints
{
    public const string Route = "/api/v1/sites/{siteId:guid}/events";

    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(Route, async (Guid siteId, IngestEventsRequest request, HttpContext http, ICommandHandler<IngestEventsCommand, IngestEventsResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new IngestEventsCommand(siteId, http.GetSiteId() ?? Guid.Empty, request.Events ?? []), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Site)
            .WithTags("Events")
            .WithSummary("Upload a batch of site events")
            .WithDescription("Accepts 1 to 500 events. Idempotent by site-generated event ID: replays count as duplicates, not errors. An invalid event rejects the whole batch.")
            .Produces<IngestEventsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
