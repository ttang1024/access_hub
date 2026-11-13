using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Auth;
using AccessHub.BuildingBlocks.Infrastructure.Web;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Application.Features.AccessGroups;

namespace AccessHub.Cardholder.Api.Endpoints;

internal static class AccessGroupsEndpoints
{
    public static IEndpointRouteBuilder MapAccessGroupsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.AccessGroups).WithTags(ApiRoutes.Tags.AccessGroups);

        group.MapPost("/", async (AccessGroupRequest request, ICommandHandler<CreateAccessGroupCommand, Guid> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new CreateAccessGroupCommand(request.Name, request.Doors), ct))
                    .Match(id => Results.Created($"{ApiRoutes.AccessGroups}/{id}", new IdResponse(id))))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Create an access group with doors and weekly schedules")
            .Produces<IdResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{accessGroupId:guid}", async (Guid accessGroupId, AccessGroupRequest request, ICommandHandler<UpdateAccessGroupCommand> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new UpdateAccessGroupCommand(accessGroupId, request.Name, request.Doors, request.ExpectedVersion), ct)).Match(Results.NoContent))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Replace an access group's name, doors and schedules")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{accessGroupId:guid}", async (Guid accessGroupId, int? expectedVersion, ICommandHandler<DeleteAccessGroupCommand> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new DeleteAccessGroupCommand(accessGroupId, expectedVersion), ct)).Match(Results.NoContent))
            .RequireAuthorization(AuthConstants.Policies.Admin)
            .WithSummary("Delete an access group")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async (int? page, int? pageSize, IQueryHandler<ListAccessGroupsQuery, PagedResult<AccessGroupDto>> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new ListAccessGroupsQuery(page ?? 1, pageSize ?? 20), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("List access groups")
            .Produces<PagedResult<AccessGroupDto>>();

        group.MapGet("/{accessGroupId:guid}", async (Guid accessGroupId, IQueryHandler<GetAccessGroupQuery, AccessGroupDto> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetAccessGroupQuery(accessGroupId), ct)).Match(Results.Ok))
            .RequireAuthorization(AuthConstants.Policies.Viewer)
            .WithSummary("Get an access group")
            .Produces<AccessGroupDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
