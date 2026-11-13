using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Web;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Application.Features.Sites;

namespace AccessHub.Cardholder.Api.Endpoints;

internal static class SitesEndpoints
{
    public static IEndpointRouteBuilder MapSitesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Sites).WithTags(ApiRoutes.Tags.Sites);

        group.MapPost("/", async (RegisterSiteRequest request, ICommandHandler<RegisterSiteCommand, RegisterSiteResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new RegisterSiteCommand(request.Name, request.TimeZoneId), ct))
                    .Match(r => Results.Created($"{ApiRoutes.Sites}/{r.SiteId}", r)))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Register a site")
            .WithDescription("Returns the site's API key. It is shown only once; only its hash is stored.")
            .Produces<RegisterSiteResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPost("/{siteId:guid}/api-key/rotate", async (Guid siteId, ICommandHandler<RotateSiteApiKeyCommand, RotateSiteApiKeyResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new RotateSiteApiKeyCommand(siteId), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Rotate a site's API key")
            .Produces<RotateSiteApiKeyResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (int? page, int? pageSize, IQueryHandler<ListSitesQuery, PagedResult<SiteDto>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListSitesQuery(page ?? 1, pageSize ?? 20), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("List sites")
            .Produces<PagedResult<SiteDto>>()
            .ProducesValidationProblem();

        group.MapGet("/{siteId:guid}", async (Guid siteId, IQueryHandler<GetSiteQuery, SiteDto> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetSiteQuery(siteId), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("Get a site")
            .Produces<SiteDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
