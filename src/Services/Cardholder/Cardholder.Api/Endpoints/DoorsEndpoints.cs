using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Web;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Application.Features.Doors;

namespace AccessHub.Cardholder.Api.Endpoints;

internal static class DoorsEndpoints
{
    public static IEndpointRouteBuilder MapDoorsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup($"{ApiRoutes.Sites}/{{siteId:guid}}/doors").WithTags(ApiRoutes.Tags.Doors);

        group.MapPost("/", async (Guid siteId, RegisterDoorRequest request, ICommandHandler<RegisterDoorCommand, Guid> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new RegisterDoorCommand(siteId, request.Name), ct))
                    .Match(id => Results.Created($"{ApiRoutes.Sites}/{siteId}/doors/{id}", new IdResponse(id))))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Register a door at a site")
            .Produces<IdResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (Guid siteId, int? page, int? pageSize, IQueryHandler<ListDoorsQuery, PagedResult<DoorDto>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListDoorsQuery(siteId, page ?? 1, pageSize ?? 20), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("List a site's doors")
            .Produces<PagedResult<DoorDto>>();

        return app;
    }
}
